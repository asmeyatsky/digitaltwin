using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Webhook service interface for event notifications
    /// </summary>
    public interface IWebhookService
    {
        /// <summary>
        /// Creates a new webhook subscription
        /// </summary>
        Task<WebhookSubscription> CreateWebhookAsync(WebhookSubscription webhook);

        /// <summary>
        /// Gets all webhook subscriptions
        /// </summary>
        Task<List<WebhookSubscription>> GetWebhooksAsync();

        /// <summary>
        /// Gets webhooks for a specific building
        /// </summary>
        Task<List<WebhookSubscription>> GetWebhooksAsync(Guid buildingId);

        /// <summary>
        /// Gets webhook by ID
        /// </summary>
        Task<WebhookSubscription> GetWebhookAsync(Guid webhookId);

        /// <summary>
        /// Updates a webhook subscription
        /// </summary>
        Task<WebhookSubscription> UpdateWebhookAsync(WebhookSubscription webhook);

        /// <summary>
        /// Deletes a webhook subscription
        /// </summary>
        Task<bool> DeleteWebhookAsync(Guid webhookId);

        /// <summary>
        /// Triggers webhook events
        /// </summary>
        Task<List<WebhookDeliveryResult>> TriggerWebhooksAsync(WebhookEvent webhookEvent);

        /// <summary>
        /// Retries failed webhook deliveries
        /// </summary>
        Task<List<WebhookDeliveryResult>> RetryFailedDeliveriesAsync();

        /// <summary>
        /// Gets webhook delivery history
        /// </summary>
        Task<WebhookDeliveryHistory> GetDeliveryHistoryAsync(WebhookDeliveryHistoryRequest request);

        /// <summary>
        /// Gets webhook statistics
        /// </summary>
        Task<WebhookStatistics> GetWebhookStatisticsAsync();

        /// <summary>
        /// Tests webhook endpoint
        /// </summary>
        Task<WebhookTestResult> TestWebhookAsync(WebhookTestRequest request);

        /// <summary>
        /// Gets webhook event types
        /// </summary>
        Task<List<WebhookEventType>> GetWebhookEventTypesAsync();

        /// <summary>
        /// Creates webhook signature
        /// </summary>
        string CreateWebhookSignature(string payload, string secret);

        /// <summary>
        /// Verifies webhook signature
        /// </summary>
        bool VerifyWebhookSignature(string payload, string signature, string secret);

        /// <summary>
        /// Gets webhook delivery logs
        /// </summary>
        Task<List<WebhookDeliveryLog>> GetDeliveryLogsAsync(Guid webhookId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Pauses webhook subscription
        /// </summary>
        Task<bool> PauseWebhookAsync(Guid webhookId);

        /// <summary>
        /// Resumes webhook subscription
        /// </summary>
        Task<bool> ResumeWebhookAsync(Guid webhookId);

        /// <summary>
        /// Gets webhook performance metrics
        /// </summary>
        Task<WebhookPerformanceMetrics> GetWebhookPerformanceMetricsAsync(Guid webhookId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Creates webhook batch delivery
        /// </summary>
        Task<WebhookBatchDeliveryResult> CreateBatchDeliveryAsync(WebhookBatchDeliveryRequest request);
    }

    /// <summary>
    /// Webhook event processor interface
    /// </summary>
    public interface IWebhookEventProcessor
    {
        string EventType { get; }
        Task<WebhookEvent> ProcessEventAsync(object eventData);
        Task<bool> ValidateEventAsync(WebhookEvent webhookEvent);
    }

    /// <summary>
    /// Webhook repository interface
    /// </summary>
    public interface IWebhookRepository
    {
        Task<WebhookSubscription> GetByIdAsync(Guid id);
        Task<List<WebhookSubscription>> GetAllAsync();
        Task<List<WebhookSubscription>> GetByBuildingIdAsync(Guid buildingId);
        Task<WebhookSubscription> AddAsync(WebhookSubscription webhook);
        Task<WebhookSubscription> UpdateAsync(WebhookSubscription webhook);
        Task<bool> DeleteAsync(Guid id);
    }

    /// <summary>
    /// Webhook delivery repository interface
    /// </summary>
    public interface IWebhookDeliveryRepository
    {
        Task<WebhookDelivery> GetByIdAsync(Guid id);
        Task<List<WebhookDelivery>> GetByWebhookIdAsync(Guid webhookId);
        Task<List<WebhookDelivery>> GetFailedDeliveriesAsync();
        Task<List<WebhookDelivery>> GetRecentDeliveriesAsync(DateTime since);
        Task<PaginatedResult<WebhookDelivery>> GetWithPaginationAsync(int page, int pageSize, Guid? webhookId = null, WebhookDeliveryStatus? status = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<WebhookDelivery> AddAsync(WebhookDelivery delivery);
        Task<WebhookDelivery> UpdateAsync(WebhookDelivery delivery);
        Task<bool> DeleteAsync(Guid id);
    }

    /// <summary>
    /// Webhook subscription
    /// </summary>
    public class WebhookSubscription
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid BuildingId { get; set; }
        public string Url { get; set; }
        public string Secret { get; set; }
        public List<string> EventTypes { get; set; }
        public List<WebhookFilter> Filters { get; set; }
        public bool IsActive { get; set; }
        public bool IsPaused { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
        public int TotalTriggers { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public WebhookRetryPolicy RetryPolicy { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public string CreatedBy { get; set; }
        public List<WebhookHeader> CustomHeaders { get; set; }
        public WebhookAuthentication Authentication { get; set; }
    }

    /// <summary>
    /// Webhook event
    /// </summary>
    public class WebhookEvent
    {
        public Guid Id { get; set; }
        public string EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid BuildingId { get; set; }
        public object Data { get; set; }
        public string Source { get; set; }
        public string Version { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Webhook delivery
    /// </summary>
    public class WebhookDelivery
    {
        public Guid Id { get; set; }
        public Guid WebhookId { get; set; }
        public Guid EventId { get; set; }
        public string EventType { get; set; }
        public DateTime AttemptedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public WebhookDeliveryStatus Status { get; set; }
        public int StatusCode { get; set; }
        public string ResponseBody { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan? ResponseTime { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public string OriginalPayload { get; set; }
        public List<WebhookDeliveryAttempt> Attempts { get; set; }
    }

    /// <summary>
    /// Webhook delivery attempt
    /// </summary>
    public class WebhookDeliveryAttempt
    {
        public int AttemptNumber { get; set; }
        public DateTime AttemptedAt { get; set; }
        public WebhookDeliveryStatus Status { get; set; }
        public int StatusCode { get; set; }
        public string ResponseBody { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan? ResponseTime { get; set; }
    }

    /// <summary>
    /// Webhook filter
    /// </summary>
    public class WebhookFilter
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public string LogicalOperator { get; set; } // AND, OR
    }

    /// <summary>
    /// Webhook retry policy
    /// </summary>
    public class WebhookRetryPolicy
    {
        public int MaxRetries { get; set; } = 3;
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromHours(1);
        public string BackoffType { get; set; } = "exponential"; // linear, exponential
        public List<WebhookRetryCondition> RetryConditions { get; set; }
    }

    /// <summary>
    /// Webhook retry condition
    /// </summary>
    public class WebhookRetryCondition
    {
        public List<int> StatusCodes { get; set; }
        public List<string> ErrorMessages { get; set; }
        public TimeSpan MinDelay { get; set; }
    }

    /// <summary>
    /// Webhook header
    /// </summary>
    public class WebhookHeader
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsRequired { get; set; }
    }

    /// <summary>
    /// Webhook authentication
    /// </summary>
    public class WebhookAuthentication
    {
        public string Type { get; set; } // none, basic, bearer, api_key
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public string ApiKey { get; set; }
        public string ApiKeyHeader { get; set; }
        public Dictionary<string, object> AdditionalParameters { get; set; }
    }

    /// <summary>
    /// Webhook delivery result
    /// </summary>
    public class WebhookDeliveryResult
    {
        public Guid? DeliveryId { get; set; }
        public Guid WebhookId { get; set; }
        public Guid EventId { get; set; }
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime AttemptedAt { get; set; }
        public TimeSpan? ResponseTime { get; set; }
        public int RetryCount { get; set; }
    }

    /// <summary>
    /// Webhook delivery history request
    /// </summary>
    public class WebhookDeliveryHistoryRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? WebhookId { get; set; }
        public WebhookDeliveryStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string EventType { get; set; }
        public string SortBy { get; set; } = "AttemptedAt";
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// Webhook delivery history
    /// </summary>
    public class WebhookDeliveryHistory
    {
        public List<WebhookDelivery> Deliveries { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Webhook statistics
    /// </summary>
    public class WebhookStatistics
    {
        public int TotalWebhooks { get; set; }
        public int ActiveWebhooks { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public double AverageResponseTime { get; set; }
        public List<string> TopEventTypes { get; set; }
        public Dictionary<bool, int> WebhookByStatus { get; set; }
        public List<WebhookDeliveryTrend> DeliveryTrends { get; set; }
        public double ErrorRate { get; set; }
        public List<WebhookPerformanceSummary> TopPerformers { get; set; }
        public List<WebhookPerformanceSummary> WorstPerformers { get; set; }
    }

    /// <summary>
    /// Webhook delivery trend
    /// </summary>
    public class WebhookDeliveryTrend
    {
        public DateTime Date { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public double SuccessRate { get; set; }
        public double AverageResponseTime { get; set; }
    }

    /// <summary>
    /// Webhook performance summary
    /// </summary>
    public class WebhookPerformanceSummary
    {
        public Guid WebhookId { get; set; }
        public string WebhookName { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public double SuccessRate { get; set; }
        public double AverageResponseTime { get; set; }
        public DateTime LastSuccessfulDelivery { get; set; }
        public DateTime LastAttempt { get; set; }
    }

    /// <summary>
    /// Webhook test request
    /// </summary>
    public class WebhookTestRequest
    {
        public Guid WebhookId { get; set; }
        public string Url { get; set; }
        public Guid BuildingId { get; set; }
        public string Secret { get; set; }
        public List<WebhookHeader> CustomHeaders { get; set; }
        public WebhookAuthentication Authentication { get; set; }
        public Dictionary<string, object> TestPayload { get; set; }
    }

    /// <summary>
    /// Webhook test result
    /// </summary>
    public class WebhookTestResult
    {
        public Guid WebhookId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public string ResponseBody { get; set; }
        public DateTime TestTimestamp { get; set; }
        public TimeSpan? ResponseTime { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Warnings { get; set; }
        public Dictionary<string, object> RequestHeaders { get; set; }
        public Dictionary<string, object> ResponseHeaders { get; set; }
    }

    /// <summary>
    /// Webhook event type
    /// </summary>
    public class WebhookEventType
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public object SamplePayload { get; set; }
        public List<string> RequiredFields { get; set; }
        public List<string> OptionalFields { get; set; }
        public bool IsDeprecated { get; set; }
        public string DeprecationMessage { get; set; }
    }

    /// <summary>
    /// Webhook delivery log
    /// </summary>
    public class WebhookDeliveryLog
    {
        public Guid Id { get; set; }
        public Guid WebhookId { get; set; }
        public Guid EventId { get; set; }
        public DateTime Timestamp { get; set; }
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string Exception { get; set; }
        public Dictionary<string, object> Context { get; set; }
    }

    /// <summary>
    /// Webhook performance metrics
    /// </summary>
    public class WebhookPerformanceMetrics
    {
        public Guid WebhookId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double SuccessRate { get; set; }
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double P95ResponseTime { get; set; }
        public double P99ResponseTime { get; set; }
        public Dictionary<int, int> StatusCodeDistribution { get; set; }
        public List<WebhookErrorSummary> TopErrors { get; set; }
        public WebhookAvailability Availability { get; set; }
    }

    /// <summary>
    /// Webhook error summary
    /// </summary>
    public class WebhookErrorSummary
    {
        public string ErrorMessage { get; set; }
        public int Count { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
        public List<int> StatusCodes { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Webhook availability
    /// </summary>
    public class WebhookAvailability
    {
        public double UptimePercentage { get; set; }
        public TimeSpan TotalDowntime { get; set; }
        public int OutageCount { get; set; }
        public DateTime LastOutageStart { get; set; }
        public DateTime LastOutageEnd { get; set; }
        public List<WebhookOutage> Outages { get; set; }
    }

    /// <summary>
    /// Webhook outage
    /// </summary>
    public class WebhookOutage
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string Reason { get; set; }
        public bool IsResolved { get; set; }
    }

    /// <summary>
    /// Webhook batch delivery request
    /// </summary>
    public class WebhookBatchDeliveryRequest
    {
        public List<Guid> WebhookIds { get; set; }
        public List<WebhookEvent> Events { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public WebhookBatchOptions Options { get; set; }
    }

    /// <summary>
    /// Webhook batch delivery result
    /// </summary>
    public class WebhookBatchDeliveryResult
    {
        public Guid BatchId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public WebhookBatchStatus Status { get; set; }
        public List<WebhookDeliveryResult> DeliveryResults { get; set; }
        public int TotalWebhooks { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Webhook batch options
    /// </summary>
    public class WebhookBatchOptions
    {
        public bool ContinueOnError { get; set; } = true;
        public int MaxConcurrentDeliveries { get; set; } = 10;
        public TimeSpan TimeoutPerWebhook { get; set; } = TimeSpan.FromSeconds(30);
        public bool SendSummaryNotification { get; set; } = false;
        public List<string> SummaryNotificationRecipients { get; set; }
    }

    /// <summary>
    /// Paginated result
    /// </summary>
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Enums for webhook system
    /// </summary>
    public enum WebhookDeliveryStatus
    {
        Pending,
        Success,
        Failed,
        Retrying,
        Cancelled
    }

    public enum WebhookBatchStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }
}