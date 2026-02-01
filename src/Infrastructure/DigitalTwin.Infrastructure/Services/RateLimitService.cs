using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace DigitalTwin.Infrastructure.Services
{
    /// <summary>
    /// Distributed Rate Limiting Service using Redis
    /// </summary>
    public interface IRateLimitService
    {
        Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window);
        Task<int> GetCurrentCountAsync(string key);
        Task<TimeSpan?> GetResetTimeAsync(string key);
    }

    public class RedisRateLimitService : IRateLimitService
    {
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _defaultWindow = TimeSpan.FromMinutes(1);

        public RedisRateLimitService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window)
        {
            var currentCount = await GetCurrentCountAsync(key);
            
            if (currentCount >= limit)
            {
                return false;
            }

            // Increment counter
            var newCount = currentCount + 1;
            var resetTime = DateTime.UtcNow.Add(window);
            
            var rateLimitData = new RateLimitData
            {
                Count = newCount,
                ResetTime = resetTime
            };

            var jsonData = JsonSerializer.Serialize(rateLimitData);
            
            // Set with expiration
            await _cache.SetStringAsync(key, jsonData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = window
            });

            return true;
        }

        public async Task<int> GetCurrentCountAsync(string key)
        {
            var jsonData = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(jsonData))
            {
                return 0;
            }

            try
            {
                var rateLimitData = JsonSerializer.Deserialize<RateLimitData>(jsonData);
                return rateLimitData?.Count ?? 0;
            }
            catch
            {
                // If deserialization fails, assume 0
                return 0;
            }
        }

        public async Task<TimeSpan?> GetResetTimeAsync(string key)
        {
            var jsonData = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(jsonData))
            {
                return null;
            }

            try
            {
                var rateLimitData = JsonSerializer.Deserialize<RateLimitData>(jsonData);
                return rateLimitData?.ResetTime - DateTime.UtcNow;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// In-memory rate limiting service for fallback
    /// </summary>
    public class InMemoryRateLimitService : IRateLimitService
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, RateLimitData> _cache 
            = new System.Collections.Concurrent.ConcurrentDictionary<string, RateLimitData>();
        private readonly object _lock = new object();

        public Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                
                if (_cache.TryGetValue(key, out var data))
                {
                    // Check if window has expired
                    if (now >= data.ResetTime)
                    {
                        // Reset counter
                        data = new RateLimitData
                        {
                            Count = 1,
                            ResetTime = now.Add(window)
                        };
                        _cache[key] = data;
                        return Task.FromResult(true);
                    }

                    if (data.Count >= limit)
                    {
                        return Task.FromResult(false);
                    }

                    data.Count++;
                    return Task.FromResult(true);
                }
                else
                {
                    // New entry
                    data = new RateLimitData
                    {
                        Count = 1,
                        ResetTime = now.Add(window)
                    };
                    _cache[key] = data;
                    return Task.FromResult(true);
                }
            }
        }

        public Task<int> GetCurrentCountAsync(string key)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var data))
                {
                    return Task.FromResult(data.Count);
                }
                return Task.FromResult(0);
            }
        }

        public Task<TimeSpan?> GetResetTimeAsync(string key)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var data))
                {
                    var remaining = data.ResetTime - DateTime.UtcNow;
                    return Task.FromResult<TimeSpan?>(remaining > TimeSpan.Zero ? remaining : null);
                }
                return Task.FromResult<TimeSpan?>(null);
            }
        }
    }

    /// <summary>
    /// Rate limiting data structure
    /// </summary>
    internal class RateLimitData
    {
        public int Count { get; set; }
        public DateTime ResetTime { get; set; }
    }

    /// <summary>
    /// Rate limiting middleware
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        public RateLimitingMiddleware(
            RequestDelegate next, 
            IRateLimitService rateLimitService,
            ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _rateLimitService = rateLimitService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Get client identifier
            var clientId = GetClientId(context);
            var endpoint = context.GetEndpoint();
            
            // Get rate limit from endpoint metadata or use defaults
            var limit = GetRateLimit(endpoint);
            var key = $"rate_limit_{limit.Category}_{clientId}";
            
            var isAllowed = await _rateLimitService.IsAllowedAsync(key, limit.Requests, limit.Window);
            
            if (!isAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId} on {Path}", clientId, context.Request.Path);
                
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.Headers.Add("Retry-After", 
                    ((int)limit.Window.TotalSeconds).ToString());
                context.Response.Headers.Add("X-RateLimit-Limit", limit.Requests.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", "0");
                
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            // Add rate limit headers
            var currentCount = await _rateLimitService.GetCurrentCountAsync(key);
            var resetTime = await _rateLimitService.GetResetTimeAsync(key);
            
            context.Response.Headers.Add("X-RateLimit-Limit", limit.Requests.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", (limit.Requests - currentCount).ToString());
            
            if (resetTime.HasValue)
            {
                context.Response.Headers.Add("X-RateLimit-Reset", 
                    ((long)resetTime.Value.TotalSeconds).ToString());
            }

            await _next(context);
        }

        private string GetClientId(HttpContext context)
        {
            // Try to get user ID from claims
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }

            // Fall back to IP address
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            return $"ip:{ipAddress ?? "unknown"}";
        }

        private RateLimitConfig GetRateLimit(Endpoint endpoint)
        {
            // Check for custom rate limit on endpoint
            var rateLimitAttribute = endpoint?.Metadata.GetMetadata<RateLimitAttribute>();
            if (rateLimitAttribute != null)
            {
                return new RateLimitConfig
                {
                    Requests = rateLimitAttribute.RequestsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    Category = rateLimitAttribute.Identifier
                };
            }

            // Default rate limits based on path
            var path = endpoint?.RequestDelegate?.Target?.ToString() ?? "";
            if (path.Contains("login") || path.Contains("register"))
            {
                return new RateLimitConfig
                {
                    Requests = 5, // Stricter for auth endpoints
                    Window = TimeSpan.FromMinutes(15),
                    Category = "auth"
                };
            }

            return new RateLimitConfig
            {
                Requests = 100,
                Window = TimeSpan.FromMinutes(1),
                Category = "default"
            };
        }
    }

    internal class RateLimitConfig
    {
        public int Requests { get; set; }
        public TimeSpan Window { get; set; }
        public string Category { get; set; }
    }
}