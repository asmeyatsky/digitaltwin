using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Events;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.Telemetry;

namespace DigitalTwin.Core.Services
{
    public class UsageLimitService : IUsageLimitService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<UsageLimitService> _logger;
        private readonly IEventBus? _eventBus;

        // Tier limits: resource -> daily limit per tier
        private static readonly Dictionary<string, Dictionary<string, int>> TierLimits = new()
        {
            ["free"] = new()
            {
                ["conversations"] = 5,
                ["voice"] = 0,
                ["voice_emotion"] = 0,
                ["camera_emotion"] = 0,
                ["avatar_3d"] = 0,
            },
            ["plus"] = new()
            {
                ["conversations"] = int.MaxValue,
                ["voice"] = int.MaxValue,
                ["voice_emotion"] = 50,
                ["camera_emotion"] = 0,
                ["avatar_3d"] = int.MaxValue,
            },
            ["premium"] = new()
            {
                ["conversations"] = int.MaxValue,
                ["voice"] = int.MaxValue,
                ["voice_emotion"] = int.MaxValue,
                ["camera_emotion"] = int.MaxValue,
                ["avatar_3d"] = int.MaxValue,
            }
        };

        public UsageLimitService(
            DigitalTwinDbContext context,
            IDistributedCache cache,
            ILogger<UsageLimitService> logger,
            IEventBus? eventBus = null)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _eventBus = eventBus;
        }

        public async Task<(bool allowed, int remaining, int limit)> CheckLimitAsync(string userId, string resource)
        {
            using var activity = DiagnosticConfig.Source.StartActivity("CheckUsageLimit");
            activity?.SetTag("resource", resource);

            var tier = await GetUserTierAsync(userId);
            var limit = GetLimit(tier, resource);

            if (limit == int.MaxValue)
                return (true, int.MaxValue, int.MaxValue);

            if (limit == 0)
                return (false, 0, 0);

            var used = await GetCurrentUsageAsync(userId, resource);
            var remaining = Math.Max(0, limit - used);

            if (remaining <= 0)
                MetricsRegistry.UsageLimitHitsTotal.WithLabels(resource).Inc();

            if (remaining <= 0 && _eventBus != null)
                await _eventBus.PublishAsync("usage.limit.exceeded", new UsageLimitExceeded(userId, resource, limit, DateTime.UtcNow));

            activity?.SetTag("allowed", remaining > 0);

            return (remaining > 0, remaining, limit);
        }

        public async Task IncrementUsageAsync(string userId, string resource)
        {
            var key = UsageKey(userId, resource);
            var currentStr = await _cache.GetStringAsync(key);
            var current = currentStr != null ? int.Parse(currentStr) : 0;
            current++;

            // Set with expiry at end of day UTC
            var now = DateTime.UtcNow;
            var endOfDay = now.Date.AddDays(1);
            var ttl = endOfDay - now;

            await _cache.SetStringAsync(key, current.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = endOfDay
            });
        }

        public async Task<UsageSummary> GetUsageAsync(string userId)
        {
            var tier = await GetUserTierAsync(userId);
            var resources = new Dictionary<string, UsageDetail>();
            var endOfDay = DateTime.UtcNow.Date.AddDays(1);

            var tierLimits = TierLimits.GetValueOrDefault(tier, TierLimits["free"]);

            foreach (var (resource, limit) in tierLimits)
            {
                var used = await GetCurrentUsageAsync(userId, resource);
                resources[resource] = new UsageDetail
                {
                    Used = used,
                    Limit = limit,
                    ResetsAt = endOfDay
                };
            }

            return new UsageSummary
            {
                UserId = userId,
                Tier = tier,
                Resources = resources
            };
        }

        private async Task<string> GetUserTierAsync(string userId)
        {
            var cacheKey = $"tier:{userId}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null) return cached;

            var subscription = await _context.Subscriptions
                .Where(s => s.UserId == userId && s.Status == "active")
                .FirstOrDefaultAsync();

            var tier = subscription?.Tier ?? "free";

            await _cache.SetStringAsync(cacheKey, tier, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(10)
            });

            return tier;
        }

        private static int GetLimit(string tier, string resource)
        {
            var tierLimits = TierLimits.GetValueOrDefault(tier, TierLimits["free"]);
            return tierLimits.GetValueOrDefault(resource, 0);
        }

        private async Task<int> GetCurrentUsageAsync(string userId, string resource)
        {
            var key = UsageKey(userId, resource);
            var value = await _cache.GetStringAsync(key);
            return value != null ? int.Parse(value) : 0;
        }

        private static string UsageKey(string userId, string resource)
        {
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return $"usage:{userId}:{resource}:{date}";
        }
    }
}
