using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    /// <summary>
    /// Webhook service for event notifications
    /// </summary>
    public class WebhookService : IWebhookService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebhookRepository _webhookRepository;
        private readonly IWebhookDeliveryRepository _deliveryRepository;
        private readonly Dictionary<string, IWebhookEventProcessor> _eventProcessors;

        public WebhookService(
            IHttpClientFactory httpClientFactory,
            IWebhookRepository webhookRepository,
            IWebhookDeliveryRepository deliveryRepository,
            IEnumerable<IWebhookEventProcessor> eventProcessors)
        {
            _httpClientFactory = httpClientFactory;
            _webhookRepository = webhookRepository;
            _deliveryRepository = deliveryRepository;
            _eventProcessors = eventProcessors.ToDictionary(p => p.EventType, p);
        }

        /// <summary>
        /// Creates a new webhook subscription
        /// </summary>
        public async Task<WebhookSubscription> CreateWebhookAsync(WebhookSubscription webhook)
        {
            webhook.Id = Guid.NewGuid();
            webhook.Secret = GenerateWebhookSecret();
            webhook.IsActive = true;
            webhook.CreatedAt = DateTime.UtcNow;
            webhook.LastTriggeredAt = null;

            ValidateWebhook(webhook);

            await _webhookRepository.AddAsync(webhook);

            return webhook;
        }

        /// <summary>
        /// Gets all webhook subscriptions
        /// </summary>
        public async Task<List<WebhookSubscription>> GetWebhooksAsync()
        {
            return await _webhookRepository.GetAllAsync();
        }

        /// <summary>
        /// Gets webhooks for a specific building
        /// </summary>
        public async Task<List<WebhookSubscription>> GetWebhooksAsync(Guid buildingId)
        {
            return await _webhookRepository.GetByBuildingIdAsync(buildingId);
        }

        /// <summary>
        /// Gets webhook by ID
        /// </summary>
        public async Task<WebhookSubscription> GetWebhookAsync(Guid webhookId)
        {
            return await _webhookRepository.GetByIdAsync(webhookId);
        }

        /// <summary>
        /// Updates a webhook subscription
        /// </summary>
        public async Task<WebhookSubscription> UpdateWebhookAsync(WebhookSubscription webhook)
        {
            webhook.UpdatedAt = DateTime.UtcNow;
            ValidateWebhook(webhook);

            await _webhookRepository.UpdateAsync(webhook);

            return webhook;
        }

        /// <summary>
        /// Deletes a webhook subscription
        /// </summary>
        public async Task<bool> DeleteWebhookAsync(Guid webhookId)
        {
            return await _webhookRepository.DeleteAsync(webhookId);
        }

        /// <summary>
        /// Triggers webhook events
        /// </summary>
        public async Task<List<WebhookDeliveryResult>> TriggerWebhooksAsync(WebhookEvent webhookEvent)
        {
            var deliveryResults = new List<WebhookDeliveryResult>();

            // Get matching webhooks
            var webhooks = await GetMatchingWebhooksAsync(webhookEvent);

            foreach (var webhook in webhooks.Where(w => w.IsActive))
            {
                try
                {
                    var deliveryResult = await DeliverWebhookAsync(webhook, webhookEvent);
                    deliveryResults.Add(deliveryResult);
                }
                catch (Exception ex)
                {
                    deliveryResults.Add(new WebhookDeliveryResult
                    {
                        WebhookId = webhook.Id,
                        Success = false,
                        ErrorMessage = ex.Message,
                        AttemptedAt = DateTime.UtcNow
                    });
                }
            }

            return deliveryResults;
        }

        /// <summary>
        /// Retries failed webhook deliveries
        /// </summary>
        public async Task<List<WebhookDeliveryResult>> RetryFailedDeliveriesAsync()
        {
            var failedDeliveries = await _deliveryRepository.GetFailedDeliveriesAsync();
            var retryResults = new List<WebhookDeliveryResult>();

            foreach (var delivery in failedDeliveries)
            {
                if (delivery.RetryCount < 3 && DateTime.UtcNow > delivery.NextRetryAt)
                {
                    var webhook = await _webhookRepository.GetByIdAsync(delivery.WebhookId);
                    if (webhook != null)
                    {
                        var retryResult = await RetryWebhookDeliveryAsync(webhook, delivery);
                        retryResults.Add(retryResult);
                    }
                }
            }

            return retryResults;
        }

        /// <summary>
        /// Gets webhook delivery history
        /// </summary>
        public async Task<WebhookDeliveryHistory> GetDeliveryHistoryAsync(WebhookDeliveryHistoryRequest request)
        {
            var deliveries = await _deliveryRepository.GetWithPaginationAsync(
                request.Page,
                request.PageSize,
                request.WebhookId,
                request.Status,
                request.StartDate,
                request.EndDate
            );

            return new WebhookDeliveryHistory
            {
                Deliveries = deliveries.Items,
                TotalCount = deliveries.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)deliveries.TotalCount / request.PageSize)
            };
        }

        /// <summary>
        /// Gets webhook statistics
        /// </summary>
        public async Task<WebhookStatistics> GetWebhookStatisticsAsync()
        {
            var webhooks = await _webhookRepository.GetAllAsync();
            var recentDeliveries = await _deliveryRepository.GetRecentDeliveriesAsync(DateTime.UtcNow.AddDays(30));

            return new WebhookStatistics
            {
                TotalWebhooks = webhooks.Count,
                ActiveWebhooks = webhooks.Count(w => w.IsActive),
                TotalDeliveries = recentDeliveries.Count,
                SuccessfulDeliveries = recentDeliveries.Count(d => d.Status == WebhookDeliveryStatus.Success),
                FailedDeliveries = recentDeliveries.Count(d => d.Status == WebhookDeliveryStatus.Failed),
                AverageResponseTime = CalculateAverageResponseTime(recentDeliveries),
                TopEventTypes = GetTopEventTypes(recentDeliveries),
                WebhookByStatus = webhooks.GroupBy(w => w.IsActive).ToDictionary(g => g.Key, g => g.Count()),
                DeliveryTrends = CalculateDeliveryTrends(recentDeliveries),
                ErrorRate = CalculateErrorRate(recentDeliveries)
            };
        }

        /// <summary>
        /// Tests webhook endpoint
        /// </summary>
        public async Task<WebhookTestResult> TestWebhookAsync(WebhookTestRequest request)
        {
            var testResult = new WebhookTestResult
            {
                WebhookId = request.WebhookId,
                TestTimestamp = DateTime.UtcNow
            };

            try
            {
                var testEvent = new WebhookEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = "test",
                    Timestamp = DateTime.UtcNow,
                    BuildingId = request.BuildingId,
                    Data = new { Test = true, Message = "Webhook test event" },
                    Source = "WebhookService"
                };

                var httpClient = _httpClientFactory.CreateClient();
                var webhookRequest = CreateHttpRequestMessage(request.Url, testEvent, request.Secret);

                var response = await httpClient.SendAsync(webhookRequest, TimeSpan.FromSeconds(30));

                testResult.Success = response.IsSuccessStatusCode;
                testResult.StatusCode = (int)response.StatusCode;
                testResult.ResponseBody = await response.Content.ReadAsStringAsync();
                testResult.ResponseTime = DateTime.UtcNow - testResult.TestTimestamp;

                if (testResult.Success)
                {
                    testResult.Message = "Webhook endpoint is accessible and returned a success response";
                }
                else
                {
                    testResult.Message = $"Webhook endpoint returned HTTP {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                testResult.Success = false;
                testResult.ErrorMessage = ex.Message;
                testResult.Message = "Webhook endpoint is not accessible";
            }

            return testResult;
        }

        /// <summary>
        /// Gets webhook event types
        /// </summary>
        public async Task<List<WebhookEventType>> GetWebhookEventTypesAsync()
        {
            return await Task.FromResult(new List<WebhookEventType>
            {
                new WebhookEventType
                {
                    Name = "sensor_data_received",
                    Description = "Triggered when new sensor data is received",
                    Category = "Data",
                    SamplePayload = new { SensorId = "sensor-123", Value = 25.5, Timestamp = "2023-01-01T12:00:00Z" }
                },
                new WebhookEventType
                {
                    Name = "alert_triggered",
                    Description = "Triggered when an alert is generated",
                    Category = "Alerts",
                    SamplePayload = new { AlertId = "alert-456", Type = "High Temperature", Severity = "High" }
                },
                new WebhookEventType
                {
                    Name = "equipment_status_changed",
                    Description = "Triggered when equipment status changes",
                    Category = "Equipment",
                    SamplePayload = new { EquipmentId = "eq-789", OldStatus = "Offline", NewStatus = "Online" }
                },
                new WebhookEventType
                {
                    Name = "report_generated",
                    Description = "Triggered when a report is generated",
                    Category = "Reports",
                    SamplePayload = new { ReportId = "report-101", Type = "Energy", Format = "PDF" }
                },
                new WebhookEventType
                {
                    Name = "user_action",
                    Description = "Triggered when a user performs a significant action",
                    Category = "Users",
                    SamplePayload = new { UserId = "user-202", Action = "Login", Timestamp = "2023-01-01T12:00:00Z" }
                },
                new WebhookEventType
                {
                    Name = "system_event",
                    Description = "Triggered for system-level events",
                    Category = "System",
                    SamplePayload = new { Event = "Maintenance", Description = "Scheduled maintenance completed" }
                }
            });
        }

        /// <summary>
        /// Creates webhook signature
        /// </summary>
        public string CreateWebhookSignature(string payload, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Verifies webhook signature
        /// </summary>
        public bool VerifyWebhookSignature(string payload, string signature, string secret)
        {
            var expectedSignature = CreateWebhookSignature(payload, secret);
            return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }

        private void ValidateWebhook(WebhookSubscription webhook)
        {
            if (string.IsNullOrEmpty(webhook.Name))
                throw new ArgumentException("Webhook name is required");

            if (string.IsNullOrEmpty(webhook.Url))
                throw new ArgumentException("Webhook URL is required");

            if (!Uri.TryCreate(webhook.Url, UriKind.Absolute, out _))
                throw new ArgumentException("Invalid webhook URL");

            if (webhook.EventTypes == null || !webhook.EventTypes.Any())
                throw new ArgumentException("At least one event type must be specified");

            foreach (var eventType in webhook.EventTypes)
            {
                if (string.IsNullOrEmpty(eventType))
                    throw new ArgumentException("Event type cannot be null or empty");
            }
        }

        private async Task<List<WebhookSubscription>> GetMatchingWebhooksAsync(WebhookEvent webhookEvent)
        {
            var webhooks = await _webhookRepository.GetAllAsync();

            return webhooks.Where(w =>
                w.IsActive &&
                w.BuildingId == webhookEvent.BuildingId &&
                w.EventTypes.Contains(webhookEvent.EventType)).ToList();
        }

        private async Task<WebhookDeliveryResult> DeliverWebhookAsync(WebhookSubscription webhook, WebhookEvent webhookEvent)
        {
            var delivery = new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                WebhookId = webhook.Id,
                EventId = webhookEvent.Id,
                EventType = webhookEvent.EventType,
                AttemptedAt = DateTime.UtcNow,
                Status = WebhookDeliveryStatus.Pending,
                RetryCount = 0,
                NextRetryAt = DateTime.UtcNow.AddMinutes(1)
            };

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var request = CreateHttpRequestMessage(webhook.Url, webhookEvent, webhook.Secret);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.SendAsync(request, TimeSpan.FromSeconds(30));
                stopwatch.Stop();

                delivery.StatusCode = (int)response.StatusCode;
                delivery.ResponseBody = await response.Content.ReadAsStringAsync();
                delivery.ResponseTime = stopwatch.Elapsed;
                delivery.Status = response.IsSuccessStatusCode
                    ? WebhookDeliveryStatus.Success
                    : WebhookDeliveryStatus.Failed;

                if (delivery.Status == WebhookDeliveryStatus.Success)
                {
                    webhook.LastTriggeredAt = DateTime.UtcNow;
                    webhook.TotalTriggers++;
                    await _webhookRepository.UpdateAsync(webhook);
                }
            }
            catch (Exception ex)
            {
                delivery.Status = WebhookDeliveryStatus.Failed;
                delivery.ErrorMessage = ex.Message;
            }

            await _deliveryRepository.AddAsync(delivery);

            return new WebhookDeliveryResult
            {
                DeliveryId = delivery.Id,
                WebhookId = webhook.Id,
                EventId = webhookEvent.Id,
                Success = delivery.Status == WebhookDeliveryStatus.Success,
                StatusCode = delivery.StatusCode,
                ErrorMessage = delivery.ErrorMessage,
                AttemptedAt = delivery.AttemptedAt,
                ResponseTime = delivery.ResponseTime,
                RetryCount = delivery.RetryCount
            };
        }

        private HttpRequestMessage CreateHttpRequestMessage(string url, WebhookEvent webhookEvent, string secret)
        {
            var payload = JsonSerializer.Serialize(webhookEvent);
            var signature = CreateWebhookSignature(payload, secret);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            request.Headers.Add("X-DigitalTwin-Signature", signature);
            request.Headers.Add("X-DigitalTwin-Event", webhookEvent.EventType);
            request.Headers.Add("X-DigitalTwin-Timestamp", webhookEvent.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            request.Headers.Add("User-Agent", "DigitalTwin-Webhook/1.0");

            return request;
        }

        private async Task<WebhookDeliveryResult> RetryWebhookDeliveryAsync(WebhookSubscription webhook, WebhookDelivery delivery)
        {
            delivery.RetryCount++;
            delivery.AttemptedAt = DateTime.UtcNow;
            delivery.NextRetryAt = CalculateNextRetryTime(delivery.RetryCount);

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var webhookEvent = new WebhookEvent
                {
                    Id = delivery.EventId,
                    EventType = delivery.EventType,
                    Timestamp = delivery.AttemptedAt,
                    BuildingId = webhook.BuildingId,
                    Data = delivery.OriginalPayload,
                    Source = "WebhookRetry"
                };

                var request = CreateHttpRequestMessage(webhook.Url, webhookEvent, webhook.Secret);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.SendAsync(request, TimeSpan.FromSeconds(30));
                stopwatch.Stop();

                delivery.StatusCode = (int)response.StatusCode;
                delivery.ResponseBody = await response.Content.ReadAsStringAsync();
                delivery.ResponseTime = stopwatch.Elapsed;
                delivery.Status = response.IsSuccessStatusCode
                    ? WebhookDeliveryStatus.Success
                    : WebhookDeliveryStatus.Failed;

                if (delivery.Status == WebhookDeliveryStatus.Success)
                {
                    webhook.TotalTriggers++;
                    await _webhookRepository.UpdateAsync(webhook);
                }
            }
            catch (Exception ex)
            {
                delivery.Status = WebhookDeliveryStatus.Failed;
                delivery.ErrorMessage = ex.Message;
            }

            await _deliveryRepository.UpdateAsync(delivery);

            return new WebhookDeliveryResult
            {
                DeliveryId = delivery.Id,
                WebhookId = webhook.Id,
                EventId = delivery.EventId,
                Success = delivery.Status == WebhookDeliveryStatus.Success,
                StatusCode = delivery.StatusCode,
                ErrorMessage = delivery.ErrorMessage,
                AttemptedAt = delivery.AttemptedAt,
                ResponseTime = delivery.ResponseTime,
                RetryCount = delivery.RetryCount
            };
        }

        private string GenerateWebhookSecret()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private DateTime CalculateNextRetryTime(int retryCount)
        {
            return DateTime.UtcNow.AddMinutes(Math.Pow(2, retryCount)); // Exponential backoff
        }

        private double CalculateAverageResponseTime(List<WebhookDelivery> deliveries)
        {
            var successfulDeliveries = deliveries.Where(d => d.Status == WebhookDeliveryStatus.Success).ToList();
            if (!successfulDeliveries.Any()) return 0;

            return successfulDeliveries.Average(d => d.ResponseTime?.TotalMilliseconds ?? 0);
        }

        private List<string> GetTopEventTypes(List<WebhookDelivery> deliveries)
        {
            return deliveries
                .GroupBy(d => d.EventType)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();
        }

        private List<WebhookDeliveryTrend> CalculateDeliveryTrends(List<WebhookDelivery> deliveries)
        {
            return deliveries
                .GroupBy(d => d.AttemptedAt.Date)
                .Select(g => new WebhookDeliveryTrend
                {
                    Date = g.Key,
                    TotalDeliveries = g.Count(),
                    SuccessfulDeliveries = g.Count(d => d.Status == WebhookDeliveryStatus.Success),
                    FailedDeliveries = g.Count(d => d.Status == WebhookDeliveryStatus.Failed)
                })
                .OrderByDescending(t => t.Date)
                .Take(30)
                .ToList();
        }

        private double CalculateErrorRate(List<WebhookDelivery> deliveries)
        {
            if (!deliveries.Any()) return 0;
            return (double)deliveries.Count(d => d.Status == WebhookDeliveryStatus.Failed) / deliveries.Count * 100;
        }
    }
}