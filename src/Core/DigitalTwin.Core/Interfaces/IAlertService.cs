using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Alert service interface for real-time alert management
    /// </summary>
    public interface IAlertService
    {
        /// <summary>
        /// Creates a new alert rule
        /// </summary>
        Task<AlertRule> CreateAlertRuleAsync(AlertRule rule);

        /// <summary>
        /// Gets all alert rules
        /// </summary>
        Task<List<AlertRule>> GetAlertRulesAsync();

        /// <summary>
        /// Gets alert rules for a specific building
        /// </summary>
        Task<List<AlertRule>> GetAlertRulesAsync(Guid buildingId);

        /// <summary>
        /// Updates an alert rule
        /// </summary>
        Task<AlertRule> UpdateAlertRuleAsync(AlertRule rule);

        /// <summary>
        /// Deletes an alert rule
        /// </summary>
        Task<bool> DeleteAlertRuleAsync(Guid ruleId);

        /// <summary>
        /// Gets active alerts
        /// </summary>
        Task<List<Alert>> GetActiveAlertsAsync();

        /// <summary>
        /// Gets alerts for a specific building
        /// </summary>
        Task<List<Alert>> GetAlertsAsync(Guid buildingId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets alert by ID
        /// </summary>
        Task<Alert> GetAlertAsync(Guid alertId);

        /// <summary>
        /// Acknowledges an alert
        /// </summary>
        Task<bool> AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy);

        /// <summary>
        /// Resolves an alert
        /// </summary>
        Task<bool> ResolveAlertAsync(Guid alertId, string resolvedBy, string resolutionNotes = null);

        /// <summary>
        /// Escalates an alert
        /// </summary>
        Task<bool> EscalateAlertAsync(Guid alertId, string escalatedBy, string escalationLevel);

        /// <summary>
        /// Processes real-time sensor data to trigger alerts
        /// </summary>
        Task ProcessSensorDataAsync(SensorReading sensorReading);

        /// <summary>
        /// Gets alert statistics
        /// </summary>
        Task<AlertStatistics> GetAlertStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets alert history with pagination
        /// </summary>
        Task<AlertHistoryResult> GetAlertHistoryAsync(AlertHistoryRequest request);

        /// <summary>
        /// Subscribes to alert notifications
        /// </summary>
        Task<AlertSubscription> SubscribeToAlertsAsync(AlertSubscription subscription);

        /// <summary>
        /// Unsubscribes from alert notifications
        /// </summary>
        Task<bool> UnsubscribeFromAlertsAsync(Guid subscriptionId);

        /// <summary>
        /// Gets user alert subscriptions
        /// </summary>
        Task<List<AlertSubscription>> GetUserSubscriptionsAsync(string userId);

        /// <summary>
        /// Tests alert rule configuration
        /// </summary>
        Task<AlertRuleTestResult> TestAlertRuleAsync(AlertRule rule, SensorReading testReading);

        /// <summary>
        /// Gets alert escalation policies
        /// </summary>
        Task<List<EscalationPolicy>> GetEscalationPoliciesAsync();

        /// <summary>
        /// Creates manual alert
        /// </summary>
        Task<Alert> CreateManualAlertAsync(ManualAlertRequest request);

        /// <summary>
        /// Gets alert templates
        /// </summary>
        Task<List<AlertTemplate>> GetAlertTemplatesAsync();

        /// <summary>
        /// Bulk updates alert status
        /// </summary>
        Task<BulkAlertUpdateResult> BulkUpdateAlertStatusAsync(BulkAlertUpdateRequest request);

        /// <summary>
        /// Gets alert correlations and patterns
        /// </summary>
        Task<AlertCorrelationAnalysis> GetAlertCorrelationAnalysisAsync(Guid buildingId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets alert performance metrics
        /// </summary>
        Task<AlertPerformanceMetrics> GetAlertPerformanceMetricsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Merges duplicate alerts
        /// </summary>
        Task<AlertMergeResult> MergeDuplicateAlertsAsync(List<Guid> alertIds);

        /// <summary>
        /// Snoozes an alert temporarily
        /// </summary>
        Task<bool> SnoozeAlertAsync(Guid alertId, string snoozedBy, TimeSpan snoozeDuration, string snoozeReason);

        /// <summary>
        /// Gets alert predictions based on trends
        /// </summary>
        Task<List<AlertPrediction>> GetAlertPredictionsAsync(Guid buildingId, TimeSpan predictionHorizon);

        /// <summary>
        /// Gets alert routing rules
        /// </summary>
        Task<List<AlertRoutingRule>> GetAlertRoutingRulesAsync();

        /// <summary>
        /// Updates alert routing rules
        /// </summary>
        Task<bool> UpdateAlertRoutingRulesAsync(List<AlertRoutingRule> rules);

        /// <summary>
        /// Gets alert analytics dashboard data
        /// </summary>
        Task<AlertDashboardData> GetAlertDashboardDataAsync(Guid buildingId, DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Notification service interface
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends notification through specified channel
        /// </summary>
        Task<bool> SendNotificationAsync(NotificationRequest request);

        /// <summary>
        /// Sends bulk notifications
        /// </summary>
        Task<BulkNotificationResult> SendBulkNotificationsAsync(List<NotificationRequest> requests);

        /// <summary>
        /// Gets notification delivery status
        /// </summary>
        Task<NotificationStatus> GetNotificationStatusAsync(Guid notificationId);

        /// <summary>
        /// Gets notification history
        /// </summary>
        Task<List<NotificationRecord>> GetNotificationHistoryAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets supported notification channels
        /// </summary>
        Task<List<NotificationChannelInfo>> GetSupportedChannelsAsync();

        /// <summary>
        /// Tests notification channel configuration
        /// </summary>
        Task<ChannelTestResult> TestNotificationChannelAsync(NotificationChannel channel, string testRecipient);
    }

    /// <summary>
    /// Alert rule repository interface
    /// </summary>
    public interface IAlertRuleRepository
    {
        Task<AlertRule> GetByIdAsync(Guid id);
        Task<List<AlertRule>> GetAllAsync();
        Task<List<AlertRule>> GetByBuildingIdAsync(Guid buildingId);
        Task<List<AlertRule>> GetBySensorIdAsync(Guid sensorId);
        Task<AlertRule> AddAsync(AlertRule rule);
        Task<AlertRule> UpdateAsync(AlertRule rule);
        Task<bool> DeleteAsync(Guid id);
    }

    /// <summary>
    /// Alert repository interface
    /// </summary>
    public interface IAlertRepository
    {
        Task<Alert> GetByIdAsync(Guid id);
        Task<List<Alert>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Alert>> GetActiveAlertsAsync();
        Task<List<Alert>> GetByBuildingIdAsync(Guid buildingId, DateTime? startDate = null, DateTime? endDate = null);
        Task<PaginatedResult<Alert>> GetWithPaginationAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null, AlertSeverity? severity = null, AlertStatus? status = null, string type = null);
        Task<List<Alert>> GetRecentAlertsAsync(Guid ruleId, TimeSpan timeWindow);
        Task<Alert> AddAsync(Alert alert);
        Task<Alert> UpdateAsync(Alert alert);
        Task<bool> DeleteAsync(Guid id);
        Task<List<AlertSubscription>> GetUserSubscriptionsAsync(string userId);
        Task<List<AlertSubscription>> GetSubscriptionsByBuildingIdAsync(Guid buildingId);
        Task<List<AlertSubscription>> GetEscalationSubscriptionsAsync(Guid buildingId, AlertSeverity severity);
        Task<AlertSubscription> AddSubscriptionAsync(AlertSubscription subscription);
        Task<bool> DeleteSubscriptionAsync(Guid subscriptionId);
    }

    /// <summary>
    /// Alert data transfer objects
    /// </summary>
    public class Alert
    {
        public Guid Id { get; set; }
        public Guid RuleId { get; set; }
        public Guid BuildingId { get; set; }
        public string Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; }
        public AlertStatus Status { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string AcknowledgedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ResolvedBy { get; set; }
        public string ResolutionNotes { get; set; }
        public DateTime? EscalatedAt { get; set; }
        public string EscalatedBy { get; set; }
        public string EscalationLevel { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public bool IsTest { get; set; }
        public List<AlertAction> Actions { get; set; }
    }

    public class AlertRule
    {
        public Guid Id { get; set; }
        public Guid BuildingId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public List<AlertCondition> Conditions { get; set; }
        public List<string> Recipients { get; set; }
        public List<NotificationChannel> NotificationChannels { get; set; }
        public bool IsActive { get; set; }
        public TimeSpan? FrequencyLimit { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public List<string> Tags { get; set; }
    }

    public class AlertCondition
    {
        public string Parameter { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string LogicalOperator { get; set; } // AND, OR
        public int Order { get; set; }
    }

    public class AlertSubscription
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public Guid BuildingId { get; set; }
        public List<string> AlertTypes { get; set; }
        public List<AlertSeverity> Severities { get; set; }
        public List<NotificationChannel> Channels { get; set; }
        public bool IsActive { get; set; }
        public DateTime SubscribedAt { get; set; }
        public DateTime? UnsubscribedAt { get; set; }
        public Dictionary<string, object> Preferences { get; set; }
    }

    public class AlertStatistics
    {
        public int TotalAlerts { get; set; }
        public int ActiveAlerts { get; set; }
        public int AcknowledgedAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        public int EscalatedAlerts { get; set; }
        public Dictionary<AlertSeverity, int> AlertsBySeverity { get; set; }
        public Dictionary<string, int> AlertsByType { get; set; }
        public TimeSpan AverageResolutionTime { get; set; }
        public List<AlertSourceStat> TopAlertSources { get; set; }
        public List<AlertTrend> AlertTrends { get; set; }
        public double EscalationRate { get; set; }
    }

    public class AlertSourceStat
    {
        public string Source { get; set; }
        public int Count { get; set; }
    }

    public class AlertTrend
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public Dictionary<AlertSeverity, int> Severity { get; set; }
    }

    public class AlertHistoryResult
    {
        public List<Alert> Alerts { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class AlertHistoryRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public AlertSeverity? Severity { get; set; }
        public AlertStatus? Status { get; set; }
        public string Type { get; set; }
        public Guid? BuildingId { get; set; }
        public string SortBy { get; set; } = "Timestamp";
        public string SortOrder { get; set; } = "desc";
    }

    public class AlertRuleTestResult
    {
        public Guid RuleId { get; set; }
        public DateTime TestTimestamp { get; set; }
        public SensorReading TestReading { get; set; }
        public bool TestPassed { get; set; }
        public bool WouldTrigger { get; set; }
        public Alert PreviewAlert { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class EscalationPolicy
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TriggerCondition { get; set; }
        public string EscalationLevel { get; set; }
        public TimeSpan EscalationDelay { get; set; }
        public List<string> EscalationActions { get; set; }
        public List<string> EscalationRecipients { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ManualAlertRequest
    {
        public Guid BuildingId { get; set; }
        public string Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public string CreatedBy { get; set; }
        public List<string> Recipients { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class AlertTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public AlertSeverity DefaultSeverity { get; set; }
        public string TitleTemplate { get; set; }
        public string DescriptionTemplate { get; set; }
        public List<string> DefaultRecipients { get; set; }
        public List<NotificationChannel> DefaultChannels { get; set; }
        public Dictionary<string, object> DefaultConditions { get; set; }
    }

    public class BulkAlertUpdateRequest
    {
        public List<Guid> AlertIds { get; set; }
        public AlertStatus Status { get; set; }
        public string UpdatedBy { get; set; }
        public string ResolutionNotes { get; set; }
    }

    public class BulkAlertUpdateResult
    {
        public int TotalAlerts { get; set; }
        public int SuccessfulUpdates { get; set; }
        public int FailedUpdates { get; set; }
        public List<string> Errors { get; set; }
    }

    public class AlertCorrelationAnalysis
    {
        public List<AlertPattern> Patterns { get; set; }
        public List<AlertCluster> Clusters { get; set; }
        public List<AlertSequence> Sequences { get; set; }
        public Dictionary<string, double> CorrelationCoefficients { get; set; }
        public List<string> Recommendations { get; set; }
    }

    public class AlertPattern
    {
        public string Pattern { get; set; }
        public int Frequency { get; set; }
        public TimeSpan AverageInterval { get; set; }
        public List<Alert> ExampleAlerts { get; set; }
        public double Confidence { get; set; }
    }

    public class AlertCluster
    {
        public int ClusterId { get; set; }
        public List<Alert> Alerts { get; set; }
        public AlertType DominantType { get; set; }
        public AlertSeverity DominantSeverity { get; set; }
        public TimeSpan TimeWindow { get; set; }
        public string CommonSource { get; set; }
    }

    public class AlertSequence
    {
        public List<Alert> Alerts { get; set; }
        public TimeSpan AverageDelay { get; set; }
        public int OccurrenceCount { get; set; }
        public double Probability { get; set; }
        public string SequenceDescription { get; set; }
    }

    public class AlertPerformanceMetrics
    {
        public double MeanTimeToAcknowledge { get; set; }
        public double MeanTimeToResolve { get; set; }
        public double FirstContactResolutionRate { get; set; }
        public double EscalationRate { get; set; }
        public double FalsePositiveRate { get; set; }
        public double DetectionAccuracy { get; set; }
        public List<ChannelPerformanceMetrics> ChannelMetrics { get; set; }
        public List<AlertTypePerformanceMetrics> TypeMetrics { get; set; }
    }

    public class ChannelPerformanceMetrics
    {
        public NotificationChannel Channel { get; set; }
        public int TotalSent { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public double DeliveryRate { get; set; }
        public TimeSpan AverageDeliveryTime { get; set; }
        public double CostPerNotification { get; set; }
    }

    public class AlertTypePerformanceMetrics
    {
        public string AlertType { get; set; }
        public int TotalAlerts { get; set; }
        public int TruePositives { get; set; }
        public int FalsePositives { get; set; }
        public double Accuracy { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public TimeSpan MeanResolutionTime { get; set; }
    }

    public class AlertMergeResult
    {
        public Alert MergedAlert { get; set; }
        public List<Alert> MergedAlerts { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AlertPrediction
    {
        public DateTime PredictedTimestamp { get; set; }
        public string PredictedType { get; set; }
        public AlertSeverity PredictedSeverity { get; set; }
        public double Confidence { get; set; }
        public string PredictedSource { get; set; }
        public List<string> ContributingFactors { get; set; }
        public Dictionary<string, object> PredictionDetails { get; set; }
    }

    public class AlertRoutingRule
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<string> Conditions { get; set; }
        public List<string> Recipients { get; set; }
        public List<NotificationChannel> Channels { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AlertDashboardData
    {
        public AlertSummary Summary { get; set; }
        public List<Alert> RecentAlerts { get; set; }
        public List<AlertTrend> Trends { get; set; }
        public List<Alert> CriticalAlerts { get; set; }
        public List<Alert> UnacknowledgedAlerts { get; set; }
        public Dictionary<string, int> AlertsBySource { get; set; }
        public AlertPerformanceMetrics Performance { get; set; }
    }

    public class AlertSummary
    {
        public int TotalActive { get; set; }
        public int Critical { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
        public int Unacknowledged { get; set; }
        public int Escalated { get; set; }
        public TimeSpan AverageResolutionTime { get; set; }
    }

    public class AlertAction
    {
        public string Type { get; set; }
        public string Label { get; set; }
        public string Url { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class NotificationRequest
    {
        public Guid? AlertId { get; set; }
        public NotificationChannel Channel { get; set; }
        public List<string> Recipients { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public AlertSeverity Severity { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public bool IsHighPriority { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public string Template { get; set; }
    }

    public class NotificationRecord
    {
        public Guid Id { get; set; }
        public Guid? AlertId { get; set; }
        public string UserId { get; set; }
        public NotificationChannel Channel { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public NotificationStatus Status { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class BulkNotificationResult
    {
        public int TotalRequested { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    public class NotificationChannelInfo
    {
        public NotificationChannel Channel { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsConfigured { get; set; }
        public bool SupportsDeliveryConfirmation { get; set; }
        public bool SupportsScheduling { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public List<string> SupportedFormats { get; set; }
        public TimeSpan MaxDeliveryTime { get; set; }
        public double CostPerMessage { get; set; }
    }

    public class ChannelTestResult
    {
        public NotificationChannel Channel { get; set; }
        public bool Success { get; set; }
        public string TestRecipient { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public TimeSpan DeliveryTime { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> TestDetails { get; set; }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Enums for alert system
    /// </summary>
    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum AlertStatus
    {
        Active,
        Acknowledged,
        Resolved,
        Escalated,
        Suppressed,
        Closed
    }

    public enum NotificationChannel
    {
        Email,
        SMS,
        Voice,
        Push,
        Webhook,
        Slack,
        Teams,
        Desktop,
        Mobile
    }

    public enum NotificationStatus
    {
        Pending,
        Sent,
        Delivered,
        Read,
        Failed,
        Bounced,
        Rejected
    }
}