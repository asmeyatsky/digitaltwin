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
using DigitalTwin.Core.Events;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.Telemetry;

namespace DigitalTwin.Core.Services
{
    public class ProactiveCheckInService : IProactiveCheckInService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly IEmotionalStateService _emotionalStateService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProactiveCheckInService> _logger;
        private readonly IEventBus? _eventBus;
        private readonly IPushNotificationService? _notificationService;

        public ProactiveCheckInService(
            DigitalTwinDbContext context,
            IEmotionalStateService emotionalStateService,
            IHttpClientFactory httpClientFactory,
            ILogger<ProactiveCheckInService> logger,
            IEventBus? eventBus = null,
            IPushNotificationService? notificationService = null)
        {
            _context = context;
            _emotionalStateService = emotionalStateService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _eventBus = eventBus;
            _notificationService = notificationService;
        }

        public async Task<CheckInSuggestion?> EvaluateCheckInAsync(string userId)
        {
            try
            {
                using var activity = DiagnosticConfig.Source.StartActivity("EvaluateCheckIn");
                activity?.SetTag("userId", userId);

                var userGuid = Guid.Parse(userId);

                // Check if a check-in was already created recently (within 12 hours)
                var recentCheckIn = await _context.CheckInRecords
                    .Where(c => c.UserId == userId && c.CreatedAt > DateTime.UtcNow.AddHours(-12))
                    .AnyAsync();

                if (recentCheckIn) return null;

                // Analyze emotional trend
                var trend = await _emotionalStateService.AnalyzeEmotionalTrendsAsync(userGuid, TimeSpan.FromDays(3));

                // Trigger 1: Declining emotion trend (3+ negative sessions)
                var negativeEmotions = new[] { "Sad", "Angry", "Fear", "Fearful", "Anxious", "Frustrated", "Disgust", "Disgusted" };
                if (negativeEmotions.Contains(trend.DominantEmotion.ToString()))
                {
                    var message = await GenerateCheckInMessageAsync("mood_triggered",
                        $"User has been experiencing {trend.DominantEmotion} frequently");

                    if (_eventBus != null)
                        await _eventBus.PublishAsync("checkin.triggered", new CheckInTriggered(userId, "mood_triggered", trend.DominantEmotion.ToString(), DateTime.UtcNow));

                    MetricsRegistry.CheckInEvaluationsTotal.WithLabels("mood_triggered").Inc();
                    activity?.SetTag("triggerType", "mood_triggered");

                    // Deliver check-in as push notification
                    await SendCheckInPushAsync(userGuid, "mood_triggered", message);

                    return new CheckInSuggestion
                    {
                        Type = "mood_triggered",
                        Message = message,
                        EmotionContext = trend.DominantEmotion.ToString()
                    };
                }

                // Trigger 2: No interaction for 48+ hours
                var lastMemory = await _context.EmotionalMemories
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                if (lastMemory != null && lastMemory.CreatedAt < DateTime.UtcNow.AddHours(-48))
                {
                    var message = await GenerateCheckInMessageAsync("daily",
                        "User hasn't been active for over 48 hours");

                    if (_eventBus != null)
                        await _eventBus.PublishAsync("checkin.triggered", new CheckInTriggered(userId, "daily", "inactive", DateTime.UtcNow));

                    MetricsRegistry.CheckInEvaluationsTotal.WithLabels("daily").Inc();
                    activity?.SetTag("triggerType", "daily");

                    // Deliver check-in as push notification
                    await SendCheckInPushAsync(userGuid, "daily", message);

                    return new CheckInSuggestion
                    {
                        Type = "daily",
                        Message = message,
                        EmotionContext = "inactive"
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating check-in for user {UserId}", userId);
                return null;
            }
        }

        public async Task RecordCheckInAsync(string userId, string type, string? emotionContext)
        {
            var record = new CheckInRecord
            {
                UserId = userId,
                ScheduledAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow,
                Type = type,
                EmotionContext = emotionContext
            };

            _context.CheckInRecords.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CheckInRecord>> GetPendingCheckInsAsync(string userId)
        {
            return await _context.CheckInRecords
                .Where(c => c.UserId == userId && c.Response == null)
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task RespondToCheckInAsync(Guid checkInId, string response)
        {
            var record = await _context.CheckInRecords.FindAsync(checkInId);
            if (record != null)
            {
                record.Response = response;
                await _context.SaveChangesAsync();
            }
        }

        private async Task<string> GenerateCheckInMessageAsync(string type, string context)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("LLM");
                var serviceKey = Environment.GetEnvironmentVariable("Services__ServiceKey") ?? "dev-service-key";
                client.DefaultRequestHeaders.Add("X-Service-Key", serviceKey);

                var response = await client.PostAsJsonAsync("/generate-response", new
                {
                    message = $"Generate a gentle, caring check-in message. Context: {context}",
                    emotion = "neutral",
                    context = "proactive_checkin"
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LlmCheckInResponse>();
                    return result?.Response ?? GetFallbackMessage(type);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate check-in message via LLM, using fallback");
            }

            return GetFallbackMessage(type);
        }

        private async Task SendCheckInPushAsync(Guid userId, string type, string message)
        {
            if (_notificationService == null) return;

            try
            {
                var title = type switch
                {
                    "mood_triggered" => "Your companion is thinking of you",
                    "daily" => "Daily check-in",
                    "weekly" => "Weekly check-in",
                    _ => "Check-in"
                };

                await _notificationService.SendPushAsync(userId, title, message, new Dictionary<string, string>
                {
                    { "type", "checkin" },
                    { "checkInType", type }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send check-in push notification for user {UserId}", userId);
            }
        }

        private static string GetFallbackMessage(string type) => type switch
        {
            "mood_triggered" => "I noticed you've been going through a tough time lately. I'm here if you'd like to talk about how you're feeling.",
            "daily" => "Hey there! I haven't heard from you in a while. Just checking in — how are you doing today?",
            "weekly" => "It's been a week since we last connected. I'd love to hear how things are going for you.",
            _ => "Hi! Just wanted to check in and see how you're doing. I'm always here for you."
        };

        private class LlmCheckInResponse
        {
            public string Response { get; set; } = string.Empty;
        }
    }
}
