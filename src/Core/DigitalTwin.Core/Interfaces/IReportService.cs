using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Report service interface for report generation and scheduling
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Creates a new report template
        /// </summary>
        Task<ReportTemplate> CreateReportTemplateAsync(ReportTemplate template);

        /// <summary>
        /// Gets all report templates
        /// </summary>
        Task<List<ReportTemplate>> GetReportTemplatesAsync();

        /// <summary>
        /// Gets a specific report template
        /// </summary>
        Task<ReportTemplate> GetReportTemplateAsync(Guid templateId);

        /// <summary>
        /// Updates a report template
        /// </summary>
        Task<ReportTemplate> UpdateReportTemplateAsync(ReportTemplate template);

        /// <summary>
        /// Deletes a report template
        /// </summary>
        Task<bool> DeleteReportTemplateAsync(Guid templateId);

        /// <summary>
        /// Generates a report based on template
        /// </summary>
        Task<GeneratedReport> GenerateReportAsync(Guid templateId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets generated reports
        /// </summary>
        Task<List<GeneratedReport>> GetGeneratedReportsAsync(int page = 1, int pageSize = 20);

        /// <summary>
        /// Gets a specific generated report
        /// </summary>
        Task<GeneratedReport> GetGeneratedReportAsync(Guid reportId);

        /// <summary>
        /// Schedules a report
        /// </summary>
        Task<ReportSchedule> ScheduleReportAsync(Guid templateId, ReportSchedule schedule);

        /// <summary>
        /// Gets scheduled reports
        /// </summary>
        Task<List<ReportSchedule>> GetScheduledReportsAsync();

        /// <summary>
        /// Updates a report schedule
        /// </summary>
        Task<ReportSchedule> UpdateReportScheduleAsync(ReportSchedule schedule);

        /// <summary>
        /// Deletes a report schedule
        /// </summary>
        Task<bool> DeleteReportScheduleAsync(Guid scheduleId);

        /// <summary>
        /// Gets available report types
        /// </summary>
        Task<List<ReportTypeInfo>> GetReportTypesAsync();

        /// <summary>
        /// Validates a report template
        /// </summary>
        Task<ReportValidationResult> ValidateReportTemplateAsync(ReportTemplate template);

        /// <summary>
        /// Gets report execution logs
        /// </summary>
        Task<List<ReportExecutionLog>> GetExecutionLogsAsync(Guid scheduleId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets report statistics
        /// </summary>
        Task<ReportStatistics> GetReportStatisticsAsync();

        /// <summary>
        /// Clones a report template
        /// </summary>
        Task<ReportTemplate> CloneReportTemplateAsync(Guid templateId, string newName);

        /// <summary>
        /// Exports a report template
        /// </summary>
        Task<string> ExportReportTemplateAsync(Guid templateId);

        /// <summary>
        /// Imports a report template
        /// </summary>
        Task<ReportTemplate> ImportReportTemplateAsync(string templateData);

        /// <summary>
        /// Gets report template builder configuration
        /// </summary>
        Task<ReportTemplateBuilder> GetTemplateBuilderAsync(ReportType type);

        /// <summary>
        /// Subscribes to a report
        /// </summary>
        Task<ReportSubscription> SubscribeToReportAsync(Guid templateId, ReportSubscription subscription);

        /// <summary>
        /// Unsubscribes from a report
        /// </summary>
        Task<bool> UnsubscribeFromReportAsync(Guid subscriptionId);

        /// <summary>
        /// Gets user report subscriptions
        /// </summary>
        Task<List<ReportSubscription>> GetUserSubscriptionsAsync(Guid userId);

        /// <summary>
        /// Updates report subscription
        /// </summary>
        Task<ReportSubscription> UpdateSubscriptionAsync(ReportSubscription subscription);
    }

    /// <summary>
    /// Report validation result
    /// </summary>
    public class ReportValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; }
        public List<ValidationWarning> Warnings { get; set; }
        public List<ValidationInfo> Info { get; set; }
    }

    /// <summary>
    /// Validation error
    /// </summary>
    public class ValidationError
    {
        public string Property { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
    }

    /// <summary>
    /// Validation warning
    /// </summary>
    public class ValidationWarning
    {
        public string Property { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
    }

    /// <summary>
    /// Validation info
    /// </summary>
    public class ValidationInfo
    {
        public string Property { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
    }

    /// <summary>
    /// Report statistics
    /// </summary>
    public class ReportStatistics
    {
        public int TotalTemplates { get; set; }
        public int ActiveTemplates { get; set; }
        public int TotalScheduledReports { get; set; }
        public int ActiveScheduledReports { get; set; }
        public int TotalGeneratedReports { get; set; }
        public int ReportsThisMonth { get; set; }
        public int ReportsThisWeek { get; set; }
        public int ReportsToday { get; set; }
        public Dictionary<ReportType, int> ReportsByType { get; set; }
        public Dictionary<ReportStatus, int> ReportsByStatus { get; set; }
        public Dictionary<ReportFormat, int> ReportsByFormat { get; set; }
        public double AverageGenerationTime { get; set; }
        public long TotalFileSize { get; set; }
        public List<MostUsedTemplate> MostUsedTemplates { get; set; }
        public List<RecentActivity> RecentActivity { get; set; }
    }

    /// <summary>
    /// Most used template
    /// </summary>
    public class MostUsedTemplate
    {
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; }
        public ReportType Type { get; set; }
        public int UsageCount { get; set; }
        public DateTime LastUsed { get; set; }
    }

    /// <summary>
    /// Recent activity
    /// </summary>
    public class RecentActivity
    {
        public DateTime Timestamp { get; set; }
        public string Activity { get; set; }
        public string User { get; set; }
        public string Details { get; set; }
        public Guid? ReportId { get; set; }
        public Guid? TemplateId { get; set; }
    }

    /// <summary>
    /// Report generation options
    /// </summary>
    public class ReportGenerationOptions
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<Guid> BuildingIds { get; set; }
        public ReportFormat Format { get; set; }
        public Dictionary<string, object> CustomParameters { get; set; }
        public bool IncludeRawData { get; set; }
        public bool IncludeCharts { get; set; }
        public bool IncludeTrends { get; set; }
        public bool IncludePredictions { get; set; }
        public string Theme { get; set; }
        public string LogoUrl { get; set; }
        public List<string> Recipients { get; set; }
        public bool SendEmail { get; set; }
        public bool SaveToFile { get; set; }
        public string CustomFileName { get; set; }
    }

    /// <summary>
    /// Report generation request
    /// </summary>
    public class ReportGenerationRequest
    {
        public Guid TemplateId { get; set; }
        public string Name { get; set; }
        public ReportGenerationOptions Options { get; set; }
        public string RequestedBy { get; set; }
        public bool IsAsync { get; set; }
        public string CallbackUrl { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Report generation response
    /// </summary>
    public class ReportGenerationResponse
    {
        public Guid RequestId { get; set; }
        public Guid? ReportId { get; set; }
        public ReportGenerationStatus Status { get; set; }
        public string Message { get; set; }
        public DateTime EstimatedCompletion { get; set; }
        public int ProgressPercentage { get; set; }
        public string DownloadUrl { get; set; }
        public long FileSize { get; set; }
        public string ErrorDetails { get; set; }
    }

    /// <summary>
    /// Report generation status
    /// </summary>
    public enum ReportGenerationStatus
    {
        Queued,
        Processing,
        Generating,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Report template builder request
    /// </summary>
    public class ReportTemplateBuilderRequest
    {
        public ReportType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> SelectedSections { get; set; }
        public Dictionary<string, object> SectionConfiguration { get; set; }
        public Dictionary<string, object> Filters { get; set; }
        public Dictionary<string, object> Formatting { get; set; }
        public ReportConfiguration Configuration { get; set; }
    }

    /// <summary>
    /// Report template preview
    /// </summary>
    public class ReportTemplatePreview
    {
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; }
        public ReportType Type { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string PreviewHtml { get; set; }
        public string PreviewImageUrl { get; set; }
        public List<PreviewSection> Sections { get; set; }
        public PreviewMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Preview section
    /// </summary>
    public class PreviewSection
    {
        public string SectionId { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string PreviewHtml { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    /// <summary>
    /// Preview metadata
    /// </summary>
    public class PreviewMetadata
    {
        public int TotalPages { get; set; }
        public List<string> Charts { get; set; }
        public List<string> Tables { get; set; }
        public List<string> KPIs { get; set; }
        public long EstimatedFileSize { get; set; }
        public TimeSpan EstimatedGenerationTime { get; set; }
    }

    /// <summary>
    /// Report sharing settings
    /// </summary>
    public class ReportSharingSettings
    {
        public Guid ReportId { get; set; }
        public bool IsPublic { get; set; }
        public List<string> SharedWithUsers { get; set; }
        public List<string> SharedWithGroups { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string ShareToken { get; set; }
        public bool AllowDownload { get; set; }
        public bool AllowPrint { get; set; }
        public bool AllowEdit { get; set; }
        public DateTime SharedAt { get; set; }
        public string SharedBy { get; set; }
    }

    /// <summary>
    /// Report version
    /// </summary>
    public class ReportVersion
    {
        public Guid Id { get; set; }
        public Guid ReportId { get; set; }
        public int VersionNumber { get; set; }
        public string VersionName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string ChangeDescription { get; set; }
        public bool IsCurrent { get; set; }
        public long FileSize { get; set; }
        public string FilePath { get; set; }
    }

    /// <summary>
    /// Report comment
    /// </summary>
    public class ReportComment
    {
        public Guid Id { get; set; }
        public Guid ReportId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string SectionId { get; set; }
        public int? PageNumber { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ResolvedBy { get; set; }
        public List<ReportComment> Replies { get; set; }
    }
}