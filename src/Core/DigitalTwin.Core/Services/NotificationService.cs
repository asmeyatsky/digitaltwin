using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(
            DigitalTwinDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<PushNotificationService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task RegisterDeviceAsync(Guid userId, string token, string platform)
        {
            try
            {
                var existing = await _context.DeviceTokens
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.Token == token);

                if (existing != null)
                {
                    existing.Platform = platform;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var deviceToken = new DeviceToken
                    {
                        UserId = userId,
                        Token = token,
                        Platform = platform,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.DeviceTokens.Add(deviceToken);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Registered device token for user {UserId} on {Platform}", userId, platform);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register device token for user {UserId}", userId);
            }
        }

        public async Task UnregisterDeviceAsync(Guid userId, string token)
        {
            try
            {
                var existing = await _context.DeviceTokens
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.Token == token);

                if (existing != null)
                {
                    _context.DeviceTokens.Remove(existing);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Unregistered device token for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister device token for user {UserId}", userId);
            }
        }

        public async Task SendPushAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null)
        {
            try
            {
                var tokens = await _context.DeviceTokens
                    .Where(d => d.UserId == userId)
                    .Select(d => d.Token)
                    .ToListAsync();

                if (!tokens.Any())
                {
                    _logger.LogDebug("No device tokens found for user {UserId}, skipping push notification", userId);
                    return;
                }

                var client = _httpClientFactory.CreateClient("ExpoPush");

                foreach (var token in tokens)
                {
                    await SendExpoPushAsync(client, token, title, body, data);
                }

                _logger.LogInformation("Sent push notification to {Count} device(s) for user {UserId}", tokens.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification to user {UserId}", userId);
            }
        }

        public async Task SendPushToMultipleAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null)
        {
            try
            {
                var userIdList = userIds.ToList();
                var tokens = await _context.DeviceTokens
                    .Where(d => userIdList.Contains(d.UserId))
                    .Select(d => d.Token)
                    .ToListAsync();

                if (!tokens.Any())
                {
                    _logger.LogDebug("No device tokens found for {Count} user(s), skipping push notification", userIdList.Count);
                    return;
                }

                var client = _httpClientFactory.CreateClient("ExpoPush");

                foreach (var token in tokens)
                {
                    await SendExpoPushAsync(client, token, title, body, data);
                }

                _logger.LogInformation("Sent push notification to {Count} device(s) for {UserCount} user(s)", tokens.Count, userIdList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification to multiple users");
            }
        }

        private async Task SendExpoPushAsync(HttpClient client, string token, string title, string body, Dictionary<string, string>? data)
        {
            try
            {
                var payload = new
                {
                    to = token,
                    title = title,
                    body = body,
                    data = data
                };

                var response = await client.PostAsJsonAsync("send", payload);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Expo Push API returned {StatusCode} for token {Token}: {Response}",
                        response.StatusCode, token[..Math.Min(token.Length, 20)], responseBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send Expo push notification to token {Token}",
                    token[..Math.Min(token.Length, 20)]);
            }
        }
    }
}
