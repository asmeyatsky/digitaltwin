using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Export service interface for data export functionality
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Exports data to PDF format
        /// </summary>
        Task<ExportResult> ExportToPDFAsync(ExportRequest request);

        /// <summary>
        /// Exports data to Excel format
        /// </summary>
        Task<ExportResult> ExportToExcelAsync(ExportRequest request);

        /// <summary>
        /// Exports data to CSV format
        /// </summary>
        Task<ExportResult> ExportToCSVAsync(ExportRequest request);

        /// <summary>
        /// Exports data to JSON format
        /// </summary>
        Task<ExportResult> ExportToJSONAsync(ExportRequest request);

        /// <summary>
        /// Exports data to HTML format
        /// </summary>
        Task<ExportResult> ExportToHTMLAsync(ExportRequest request);

        /// <summary>
        /// Gets supported export formats
        /// </summary>
        Task<List<ExportFormatInfo>> GetSupportedFormatsAsync();

        /// <summary>
        /// Exports multiple formats in parallel
        /// </summary>
        Task<List<ExportResult>> ExportToMultipleFormatsAsync(MultiExportRequest request);

        /// <summary>
        /// Gets export history
        /// </summary>
        Task<List<ExportHistory>> GetExportHistoryAsync(int page = 1, int pageSize = 20);

        /// <summary>
        /// Validates export request
        /// </summary>
        Task<ExportValidationResult> ValidateExportRequestAsync(ExportRequest request);

        /// <summary>
        /// Gets export status
        /// </summary>
        Task<ExportStatus> GetExportStatusAsync(Guid exportId);

        /// <summary>
        /// Cancels an export job
        /// </summary>
        Task<bool> CancelExportAsync(Guid exportId);

        /// <summary>
        /// Downloads exported file
        /// </summary>
        Task<ExportDownloadResult> DownloadExportAsync(Guid exportId);

        /// <summary>
        /// Deletes exported file
        /// </summary>
        Task<bool> DeleteExportAsync(Guid exportId);

        /// <summary>
        /// Shares exported file
        /// </summary>
        Task<ExportShareResult> ShareExportAsync(Guid exportId, ExportShareRequest shareRequest);

        /// <summary>
        /// Gets export statistics
        /// </summary>
        Task<ExportStatistics> GetExportStatisticsAsync();

        /// <summary>
        /// Previews export data
        /// </summary>
        Task<ExportPreview> PreviewExportAsync(ExportPreviewRequest previewRequest);
    }

    /// <summary>
    /// Export request
    /// </summary>
    public class ExportRequest
    {
        public string FileName { get; set; }
        public ReportFormat Format { get; set; }
        public object Data { get; set; }
        public ExportOptions Options { get; set; }
        public ExportTemplate Template { get; set; }
        public string RequestedBy { get; set; }
        public bool IsAsync { get; set; }
        public string CallbackUrl { get; set; }
        public long EstimatedSize { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Export options
    /// </summary>
    public class ExportOptions
    {
        public bool IncludeHeaders { get; set; } = true;
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeTimestamps { get; set; } = true;
        public bool IncludeMetadata { get; set; } = false;
        public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public string NumberFormat { get; set; } = "N2";
        public string Currency { get; set; } = "USD";
        public string TimeZone { get; set; } = "UTC";
        public List<string> IncludedFields { get; set; }
        public List<string> ExcludedFields { get; set; }
        public Dictionary<string, string> FieldMappings { get; set; }
        public Dictionary<string, object> CustomSettings { get; set; }
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Normal;
        public bool PasswordProtect { get; set; } = false;
        public string Password { get; set; }
        public bool Watermark { get; set; } = false;
        public string WatermarkText { get; set; }
        public ExportQuality Quality { get; set; } = ExportQuality.Standard;
    }

    /// <summary>
    /// Export template
    /// </summary>
    public class ExportTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ReportFormat Format { get; set; }
        public Dictionary<string, object> Settings { get; set; }
        public List<ExportField> Fields { get; set; }
        public Dictionary<string, object> Layout { get; set; }
        public string Theme { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Export field definition
    /// </summary>
    public class ExportField
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string DataType { get; set; }
        public string Format { get; set; }
        public bool IsIncluded { get; set; }
        public int Order { get; set; }
        public Dictionary<string, object> Settings { get; set; }
    }

    /// <summary>
    /// Export result
    /// </summary>
    public class ExportResult
    {
        public Guid Id { get; set; }
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public ReportFormat Format { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public ExportValidationResult Validation { get; set; }
    }

    /// <summary>
    /// Multi-format export request
    /// </summary>
    public class MultiExportRequest
    {
        public string FileName { get; set; }
        public object Data { get; set; }
        public List<ReportFormat> Formats { get; set; }
        public ExportOptions Options { get; set; }
        public ExportTemplate Template { get; set; }
        public string RequestedBy { get; set; }
        public bool IsAsync { get; set; }
        public string CallbackUrl { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Export format information
    /// </summary>
    public class ExportFormatInfo
    {
        public ReportFormat Format { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Extension { get; set; }
        public string MimeType { get; set; }
        public long MaxSize { get; set; }
        public List<string> SupportedDataTypes { get; set; }
        public List<string> Features { get; set; }
        public bool SupportsCharts { get; set; }
        public bool SupportsImages { get; set; }
        public bool SupportsFormatting { get; set; }
        public bool SupportsCompression { get; set; }
        public bool SupportsPasswordProtection { get; set; }
    }

    /// <summary>
    /// Export history entry
    /// </summary>
    public class ExportHistory
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public ReportFormat Format { get; set; }
        public DateTime GeneratedAt { get; set; }
        public long FileSize { get; set; }
        public string GeneratedBy { get; set; }
        public ExportStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public int DownloadCount { get; set; }
        public DateTime? LastDownloadedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string FileUrl { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Export validation result
    /// </summary>
    public class ExportValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; }
        public List<ValidationWarning> Warnings { get; set; }
        public List<ValidationInfo> Info { get; set; }
        public long EstimatedSize { get; set; }
        public TimeSpan EstimatedTime { get; set; }
        public List<string> Recommendations { get; set; }
    }

    /// <summary>
    /// Export download result
    /// </summary>
    public class ExportDownloadResult
    {
        public bool Success { get; set; }
        public byte[] FileContent { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public DateTime LastModified { get; set; }
        public string ETag { get; set; }
        public bool SupportsRangeRequests { get; set; }
        public long ContentLength { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Export share request
    /// </summary>
    public class ExportShareRequest
    {
        public List<string> EmailAddresses { get; set; }
        public string Message { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool AllowDownload { get; set; } = true;
        public bool AllowEdit { get; set; } = false;
        public bool RequirePassword { get; set; } = false;
        public string Password { get; set; }
        public int DownloadLimit { get; set; }
        public bool TrackDownloads { get; set; } = true;
    }

    /// <summary>
    /// Export share result
    /// </summary>
    public class ExportShareResult
    {
        public bool Success { get; set; }
        public string ShareUrl { get; set; }
        public string ShareToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int DownloadLimit { get; set; }
        public int DownloadCount { get; set; }
        public List<ShareRecipient> Recipients { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Share recipient
    /// </summary>
    public class ShareRecipient
    {
        public string Email { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? AccessedAt { get; set; }
        public bool HasAccessed { get; set; }
        public int DownloadCount { get; set; }
    }

    /// <summary>
    /// Export statistics
    /// </summary>
    public class ExportStatistics
    {
        public int TotalExports { get; set; }
        public int ExportsThisMonth { get; set; }
        public int ExportsThisWeek { get; set; }
        public int ExportsToday { get; set; }
        public long TotalDataExported { get; set; }
        public Dictionary<ReportFormat, int> ExportsByFormat { get; set; }
        public Dictionary<string, int> ExportsByUser { get; set; }
        public Dictionary<string, int> ExportsByDataType { get; set; }
        public double AverageExportTime { get; set; }
        public List<MostExportedData> MostExportedData { get; set; }
        public List<ExportTrend> Trends { get; set; }
        public List<ExportActivity> RecentActivity { get; set; }
    }

    /// <summary>
    /// Most exported data
    /// </summary>
    public class MostExportedData
    {
        public string DataType { get; set; }
        public int ExportCount { get; set; }
        public DateTime LastExported { get; set; }
        public long TotalSize { get; set; }
    }

    /// <summary>
    /// Export trend
    /// </summary>
    public class ExportTrend
    {
        public DateTime Date { get; set; }
        public int ExportCount { get; set; }
        public long DataSize { get; set; }
        public Dictionary<ReportFormat, int> Formats { get; set; }
    }

    /// <summary>
    /// Export activity
    /// </summary>
    public class ExportActivity
    {
        public DateTime Timestamp { get; set; }
        public string User { get; set; }
        public string Action { get; set; }
        public string FileName { get; set; }
        public ReportFormat Format { get; set; }
        public long FileSize { get; set; }
        public string Details { get; set; }
    }

    /// <summary>
    /// Export preview request
    /// </summary>
    public class ExportPreviewRequest
    {
        public object Data { get; set; }
        public ReportFormat Format { get; set; }
        public ExportOptions Options { get; set; }
        public ExportTemplate Template { get; set; }
        public int MaxRows { get; set; } = 10;
        public bool IncludeFormatting { get; set; } = false;
    }

    /// <summary>
    /// Export preview
    /// </summary>
    public class ExportPreview
    {
        public bool Success { get; set; }
        public string Content { get; set; }
        public ReportFormat Format { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public List<string> Headers { get; set; }
        public List<List<object>> SampleData { get; set; }
        public long EstimatedFileSize { get; set; }
        public TimeSpan EstimatedGenerationTime { get; set; }
        public List<string> Warnings { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Export status
    /// </summary>
    public class ExportStatus
    {
        public Guid Id { get; set; }
        public ExportJobStatus Status { get; set; }
        public int ProgressPercentage { get; set; }
        public string CurrentOperation { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan? EstimatedRemainingTime { get; set; }
        public long ProcessedRecords { get; set; }
        public long TotalRecords { get; set; }
        public string ErrorMessage { get; set; }
        public List<ExportJobStep> Steps { get; set; }
    }

    /// <summary>
    /// Export job step
    /// </summary>
    public class ExportJobStep
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ExportJobStepStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public int ProgressPercentage { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Enums for export system
    /// </summary>
    public enum ExportStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Expired
    }

    public enum ExportJobStatus
    {
        Queued,
        Processing,
        Validating,
        Generating,
        Saving,
        Completed,
        Failed,
        Cancelled
    }

    public enum ExportJobStepStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Skipped
    }

    public enum CompressionLevel
    {
        None,
        Low,
        Normal,
        High,
        Maximum
    }

    public enum ExportQuality
    {
        Low,
        Standard,
        High,
        Premium
    }
}