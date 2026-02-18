using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
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
            _eventProcessors = eventProcessors.ToDictionary(p => p.EventType, p => p);
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

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await httpClient.SendAsync(webhookRequest, cts.Token);

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
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await httpClient.SendAsync(request, cts.Token);
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
                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await httpClient.SendAsync(request, cts2.Token);
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

        /// <summary>
        /// Gets webhook delivery logs for a specific webhook
        /// </summary>
        public async Task<List<WebhookDeliveryLog>> GetDeliveryLogsAsync(Guid webhookId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var deliveries = await _deliveryRepository.GetByWebhookIdAsync(webhookId);

            // Filter by date range
            if (startDate.HasValue)
                deliveries = deliveries.Where(d => d.AttemptedAt >= startDate.Value).ToList();
            if (endDate.HasValue)
                deliveries = deliveries.Where(d => d.AttemptedAt <= endDate.Value).ToList();

            // Convert deliveries to delivery logs
            var logs = new List<WebhookDeliveryLog>();
            foreach (var delivery in deliveries.OrderByDescending(d => d.AttemptedAt))
            {
                logs.Add(new WebhookDeliveryLog
                {
                    Id = Guid.NewGuid(),
                    WebhookId = webhookId,
                    EventId = delivery.EventId,
                    Timestamp = delivery.AttemptedAt,
                    LogLevel = delivery.Status == WebhookDeliveryStatus.Success ? "Info" : delivery.Status == WebhookDeliveryStatus.Failed ? "Error" : "Warning",
                    Message = delivery.Status == WebhookDeliveryStatus.Success
                        ? $"Successfully delivered event {delivery.EventType} (HTTP {delivery.StatusCode})"
                        : $"Failed to deliver event {delivery.EventType}: {delivery.ErrorMessage ?? "Unknown error"}",
                    Details = $"Attempt {delivery.RetryCount + 1}, Response time: {delivery.ResponseTime?.TotalMilliseconds ?? 0:F0}ms, Status: {delivery.StatusCode}",
                    Exception = delivery.Status == WebhookDeliveryStatus.Failed ? delivery.ErrorMessage : null,
                    Context = new Dictionary<string, object>
                    {
                        { "EventType", delivery.EventType },
                        { "StatusCode", delivery.StatusCode },
                        { "RetryCount", delivery.RetryCount },
                        { "ResponseTime", delivery.ResponseTime?.TotalMilliseconds ?? 0 }
                    }
                });

                // Add individual attempt logs if available
                if (delivery.Attempts != null)
                {
                    foreach (var attempt in delivery.Attempts)
                    {
                        logs.Add(new WebhookDeliveryLog
                        {
                            Id = Guid.NewGuid(),
                            WebhookId = webhookId,
                            EventId = delivery.EventId,
                            Timestamp = attempt.AttemptedAt,
                            LogLevel = attempt.Status == WebhookDeliveryStatus.Success ? "Info" : "Warning",
                            Message = $"Delivery attempt {attempt.AttemptNumber}: HTTP {attempt.StatusCode}",
                            Details = attempt.ResponseBody,
                            Exception = attempt.ErrorMessage,
                            Context = new Dictionary<string, object>
                            {
                                { "AttemptNumber", attempt.AttemptNumber },
                                { "StatusCode", attempt.StatusCode }
                            }
                        });
                    }
                }
            }

            return logs.OrderByDescending(l => l.Timestamp).ToList();
        }

        /// <summary>
        /// Pauses a webhook subscription
        /// </summary>
        public async Task<bool> PauseWebhookAsync(Guid webhookId)
        {
            var webhook = await _webhookRepository.GetByIdAsync(webhookId);
            if (webhook == null) return false;

            webhook.IsPaused = true;
            webhook.UpdatedAt = DateTime.UtcNow;
            webhook.Metadata = webhook.Metadata ?? new Dictionary<string, object>();
            webhook.Metadata["PausedAt"] = DateTime.UtcNow;

            await _webhookRepository.UpdateAsync(webhook);
            return true;
        }

        /// <summary>
        /// Resumes a webhook subscription
        /// </summary>
        public async Task<bool> ResumeWebhookAsync(Guid webhookId)
        {
            var webhook = await _webhookRepository.GetByIdAsync(webhookId);
            if (webhook == null) return false;

            webhook.IsPaused = false;
            webhook.UpdatedAt = DateTime.UtcNow;
            webhook.Metadata = webhook.Metadata ?? new Dictionary<string, object>();
            webhook.Metadata["ResumedAt"] = DateTime.UtcNow;

            await _webhookRepository.UpdateAsync(webhook);
            return true;
        }

        /// <summary>
        /// Gets webhook performance metrics for a specific webhook over a time period
        /// </summary>
        public async Task<WebhookPerformanceMetrics> GetWebhookPerformanceMetricsAsync(Guid webhookId, DateTime startDate, DateTime endDate)
        {
            var deliveries = await _deliveryRepository.GetByWebhookIdAsync(webhookId);
            var filteredDeliveries = deliveries
                .Where(d => d.AttemptedAt >= startDate && d.AttemptedAt <= endDate)
                .ToList();

            var successfulDeliveries = filteredDeliveries.Where(d => d.Status == WebhookDeliveryStatus.Success).ToList();
            var failedDeliveries = filteredDeliveries.Where(d => d.Status == WebhookDeliveryStatus.Failed).ToList();

            var responseTimes = successfulDeliveries
                .Where(d => d.ResponseTime.HasValue)
                .Select(d => d.ResponseTime.Value.TotalMilliseconds)
                .ToList();

            var sortedResponseTimes = responseTimes.OrderBy(t => t).ToList();

            // Calculate status code distribution
            var statusCodeDistribution = filteredDeliveries
                .Where(d => d.StatusCode > 0)
                .GroupBy(d => d.StatusCode)
                .ToDictionary(g => g.Key, g => g.Count());

            // Calculate top errors
            var topErrors = failedDeliveries
                .Where(d => !string.IsNullOrEmpty(d.ErrorMessage))
                .GroupBy(d => d.ErrorMessage)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new WebhookErrorSummary
                {
                    ErrorMessage = g.Key,
                    Count = g.Count(),
                    FirstOccurrence = g.Min(d => d.AttemptedAt),
                    LastOccurrence = g.Max(d => d.AttemptedAt),
                    StatusCodes = g.Select(d => d.StatusCode).Distinct().ToList(),
                    Percentage = filteredDeliveries.Count > 0 ? (double)g.Count() / filteredDeliveries.Count * 100 : 0
                })
                .ToList();

            // Calculate availability (detect outage periods)
            var outages = new List<WebhookOutage>();
            var consecutiveFailures = new List<WebhookDelivery>();
            foreach (var delivery in filteredDeliveries.OrderBy(d => d.AttemptedAt))
            {
                if (delivery.Status == WebhookDeliveryStatus.Failed)
                {
                    consecutiveFailures.Add(delivery);
                }
                else
                {
                    if (consecutiveFailures.Count >= 3)
                    {
                        outages.Add(new WebhookOutage
                        {
                            StartTime = consecutiveFailures.First().AttemptedAt,
                            EndTime = delivery.AttemptedAt,
                            Duration = delivery.AttemptedAt - consecutiveFailures.First().AttemptedAt,
                            Reason = consecutiveFailures.First().ErrorMessage ?? "Unknown",
                            IsResolved = true
                        });
                    }
                    consecutiveFailures.Clear();
                }
            }

            var totalPeriod = (endDate - startDate).TotalMinutes;
            var totalDowntime = outages.Sum(o => o.Duration.TotalMinutes);
            var uptimePercentage = totalPeriod > 0 ? (totalPeriod - totalDowntime) / totalPeriod * 100 : 100;

            return new WebhookPerformanceMetrics
            {
                WebhookId = webhookId,
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalRequests = filteredDeliveries.Count,
                SuccessfulRequests = successfulDeliveries.Count,
                FailedRequests = failedDeliveries.Count,
                SuccessRate = filteredDeliveries.Count > 0 ? (double)successfulDeliveries.Count / filteredDeliveries.Count * 100 : 0,
                AverageResponseTime = responseTimes.Any() ? responseTimes.Average() : 0,
                MinResponseTime = responseTimes.Any() ? responseTimes.Min() : 0,
                MaxResponseTime = responseTimes.Any() ? responseTimes.Max() : 0,
                P95ResponseTime = sortedResponseTimes.Count > 0 ? sortedResponseTimes[(int)(sortedResponseTimes.Count * 0.95)] : 0,
                P99ResponseTime = sortedResponseTimes.Count > 0 ? sortedResponseTimes[(int)(sortedResponseTimes.Count * 0.99)] : 0,
                StatusCodeDistribution = statusCodeDistribution,
                TopErrors = topErrors,
                Availability = new WebhookAvailability
                {
                    UptimePercentage = uptimePercentage,
                    TotalDowntime = TimeSpan.FromMinutes(totalDowntime),
                    OutageCount = outages.Count,
                    LastOutageStart = outages.Any() ? outages.Last().StartTime : default,
                    LastOutageEnd = outages.Any() ? outages.Last().EndTime ?? DateTime.UtcNow : default,
                    Outages = outages
                }
            };
        }

        /// <summary>
        /// Creates a batch delivery of webhook events to multiple webhooks
        /// </summary>
        public async Task<WebhookBatchDeliveryResult> CreateBatchDeliveryAsync(WebhookBatchDeliveryRequest request)
        {
            var batchResult = new WebhookBatchDeliveryResult
            {
                BatchId = Guid.NewGuid(),
                ScheduledAt = request.ScheduledAt ?? DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                Status = WebhookBatchStatus.InProgress,
                DeliveryResults = new List<WebhookDeliveryResult>(),
                TotalWebhooks = request.WebhookIds?.Count ?? 0,
                SuccessfulDeliveries = 0,
                FailedDeliveries = 0
            };

            if (request.WebhookIds == null || !request.WebhookIds.Any())
            {
                batchResult.Status = WebhookBatchStatus.Failed;
                batchResult.ErrorMessage = "No webhook IDs provided";
                batchResult.CompletedAt = DateTime.UtcNow;
                return batchResult;
            }

            if (request.Events == null || !request.Events.Any())
            {
                batchResult.Status = WebhookBatchStatus.Failed;
                batchResult.ErrorMessage = "No events provided";
                batchResult.CompletedAt = DateTime.UtcNow;
                return batchResult;
            }

            var options = request.Options ?? new WebhookBatchOptions();

            foreach (var webhookId in request.WebhookIds)
            {
                var webhook = await _webhookRepository.GetByIdAsync(webhookId);
                if (webhook == null || !webhook.IsActive || webhook.IsPaused)
                {
                    batchResult.FailedDeliveries++;
                    batchResult.DeliveryResults.Add(new WebhookDeliveryResult
                    {
                        WebhookId = webhookId,
                        Success = false,
                        ErrorMessage = webhook == null ? "Webhook not found" : webhook.IsPaused ? "Webhook is paused" : "Webhook is inactive",
                        AttemptedAt = DateTime.UtcNow
                    });

                    if (!options.ContinueOnError)
                    {
                        batchResult.Status = WebhookBatchStatus.Failed;
                        batchResult.ErrorMessage = "Batch stopped due to error (ContinueOnError is false)";
                        batchResult.CompletedAt = DateTime.UtcNow;
                        return batchResult;
                    }
                    continue;
                }

                foreach (var webhookEvent in request.Events)
                {
                    try
                    {
                        var deliveryResult = await DeliverWebhookAsync(webhook, webhookEvent);
                        batchResult.DeliveryResults.Add(deliveryResult);

                        if (deliveryResult.Success)
                            batchResult.SuccessfulDeliveries++;
                        else
                            batchResult.FailedDeliveries++;

                        if (!deliveryResult.Success && !options.ContinueOnError)
                        {
                            batchResult.Status = WebhookBatchStatus.Failed;
                            batchResult.ErrorMessage = "Batch stopped due to delivery error";
                            batchResult.CompletedAt = DateTime.UtcNow;
                            return batchResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        batchResult.FailedDeliveries++;
                        batchResult.DeliveryResults.Add(new WebhookDeliveryResult
                        {
                            WebhookId = webhookId,
                            EventId = webhookEvent.Id,
                            Success = false,
                            ErrorMessage = ex.Message,
                            AttemptedAt = DateTime.UtcNow
                        });

                        if (!options.ContinueOnError)
                        {
                            batchResult.Status = WebhookBatchStatus.Failed;
                            batchResult.ErrorMessage = $"Batch stopped due to exception: {ex.Message}";
                            batchResult.CompletedAt = DateTime.UtcNow;
                            return batchResult;
                        }
                    }
                }
            }

            batchResult.Status = batchResult.FailedDeliveries > 0 && batchResult.SuccessfulDeliveries == 0
                ? WebhookBatchStatus.Failed
                : WebhookBatchStatus.Completed;
            batchResult.CompletedAt = DateTime.UtcNow;

            return batchResult;
        }
    }
}