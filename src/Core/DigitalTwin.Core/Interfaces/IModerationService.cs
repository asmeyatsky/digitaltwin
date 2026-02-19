using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface IModerationService
    {
        Task<ContentReport> ReportContentAsync(Guid reporterUserId, ContentType contentType, Guid contentId, ReportReason reason, string? description);
        Task<AutoModerationCheckResult> AutoModerateAsync(string content);
        Task<ContentReport> ReviewReportAsync(Guid reviewerUserId, Guid reportId, ModerationAction action, string? notes);
        Task<ContentReport> DismissReportAsync(Guid reviewerUserId, Guid reportId, string? notes);
        Task<PaginatedReports> GetPendingReportsAsync(int page, int pageSize);
        Task<List<ContentReport>> GetReportsByContentAsync(ContentType contentType, Guid contentId);
        Task<ModerationStats> GetModerationStatsAsync();
    }

    public class AutoModerationCheckResult
    {
        public bool IsFlagged { get; set; }
        public string? Reason { get; set; }
        public double Confidence { get; set; }
    }

    public class PaginatedReports
    {
        public List<ContentReport> Reports { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class ModerationStats
    {
        public int PendingCount { get; set; }
        public int ActionedCount { get; set; }
        public int DismissedCount { get; set; }
        public int TotalCount { get; set; }
    }
}
