using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class ModerationService : IModerationService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<ModerationService> _logger;

        // Keyword lists for auto-moderation (extends SafetyPlugin patterns)
        private static readonly string[] SelfHarmKeywords = new[]
        {
            "suicide", "kill myself", "end my life", "want to die",
            "self-harm", "hurt myself", "cutting myself", "no reason to live",
            "overdose", "jump off", "hang myself"
        };

        private static readonly string[] ViolenceKeywords = new[]
        {
            "kill you", "murder", "shoot you", "stab you", "beat you up",
            "threat", "bomb", "attack you", "hurt you", "destroy you"
        };

        private static readonly string[] HateSpeechKeywords = new[]
        {
            "hate speech", "racial slur", "bigot", "supremacist",
            "discriminate", "dehumanize", "ethnic cleansing", "genocide"
        };

        private static readonly string[] SpamKeywords = new[]
        {
            "buy now", "click here", "free money", "act now", "limited offer",
            "congratulations you won", "nigerian prince", "wire transfer",
            "make money fast", "guaranteed income"
        };

        public ModerationService(DigitalTwinDbContext context, ILogger<ModerationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ContentReport> ReportContentAsync(
            Guid reporterUserId, ContentType contentType, Guid contentId,
            ReportReason reason, string? description)
        {
            var report = new ContentReport
            {
                ReporterUserId = reporterUserId,
                ContentType = contentType,
                ContentId = contentId,
                Reason = reason,
                Description = description,
                Status = ModerationReportStatus.Pending,
                Action = ModerationAction.None,
                CreatedAt = DateTime.UtcNow
            };

            _context.ContentReports.Add(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Content report created: {ReportId} by user {UserId} for {ContentType}/{ContentId} reason={Reason}",
                report.Id, reporterUserId, contentType, contentId, reason);

            return report;
        }

        public Task<AutoModerationCheckResult> AutoModerateAsync(string content)
        {
            var contentLower = content.ToLowerInvariant();
            var matchedReasons = new List<string>();
            int totalMatches = 0;

            // Check self-harm keywords
            var selfHarmMatches = SelfHarmKeywords.Count(k => contentLower.Contains(k));
            if (selfHarmMatches > 0)
            {
                matchedReasons.Add("self-harm");
                totalMatches += selfHarmMatches;
            }

            // Check violence keywords
            var violenceMatches = ViolenceKeywords.Count(k => contentLower.Contains(k));
            if (violenceMatches > 0)
            {
                matchedReasons.Add("violence");
                totalMatches += violenceMatches;
            }

            // Check hate speech keywords
            var hateMatches = HateSpeechKeywords.Count(k => contentLower.Contains(k));
            if (hateMatches > 0)
            {
                matchedReasons.Add("hate speech");
                totalMatches += hateMatches;
            }

            // Check spam keywords
            var spamMatches = SpamKeywords.Count(k => contentLower.Contains(k));
            if (spamMatches > 0)
            {
                matchedReasons.Add("spam");
                totalMatches += spamMatches;
            }

            var isFlagged = totalMatches > 0;
            // Confidence scales with number of matches: 1 match = 0.5, 2 = 0.7, 3+ = 0.9, capped at 1.0
            var confidence = isFlagged
                ? Math.Min(1.0, 0.3 + (totalMatches * 0.2))
                : 0.0;

            var reason = isFlagged
                ? string.Join(", ", matchedReasons)
                : null;

            if (isFlagged)
            {
                _logger.LogWarning(
                    "Auto-moderation flagged content: reasons={Reasons}, confidence={Confidence:F2}, matches={Matches}",
                    reason, confidence, totalMatches);
            }

            return Task.FromResult(new AutoModerationCheckResult
            {
                IsFlagged = isFlagged,
                Reason = reason,
                Confidence = confidence
            });
        }

        public async Task<ContentReport> ReviewReportAsync(
            Guid reviewerUserId, Guid reportId, ModerationAction action, string? notes)
        {
            var report = await _context.ContentReports.FindAsync(reportId);
            if (report == null)
                throw new InvalidOperationException("Report not found.");

            if (report.Status != ModerationReportStatus.Pending)
                throw new InvalidOperationException("Report has already been reviewed.");

            report.Status = action == ModerationAction.None ? ModerationReportStatus.Reviewed : ModerationReportStatus.Actioned;
            report.Action = action;
            report.ReviewedByUserId = reviewerUserId;
            report.ReviewNotes = notes;
            report.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Report {ReportId} reviewed by {ReviewerId}: status={Status}, action={Action}",
                reportId, reviewerUserId, report.Status, action);

            return report;
        }

        public async Task<ContentReport> DismissReportAsync(
            Guid reviewerUserId, Guid reportId, string? notes)
        {
            var report = await _context.ContentReports.FindAsync(reportId);
            if (report == null)
                throw new InvalidOperationException("Report not found.");

            if (report.Status != ModerationReportStatus.Pending)
                throw new InvalidOperationException("Report has already been reviewed.");

            report.Status = ModerationReportStatus.Dismissed;
            report.Action = ModerationAction.None;
            report.ReviewedByUserId = reviewerUserId;
            report.ReviewNotes = notes;
            report.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Report {ReportId} dismissed by {ReviewerId}",
                reportId, reviewerUserId);

            return report;
        }

        public async Task<PaginatedReports> GetPendingReportsAsync(int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.ContentReports
                .Where(r => r.Status == ModerationReportStatus.Pending)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();

            var reports = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedReports
            {
                Reports = reports,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<ContentReport>> GetReportsByContentAsync(
            ContentType contentType, Guid contentId)
        {
            return await _context.ContentReports
                .Where(r => r.ContentType == contentType && r.ContentId == contentId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<ModerationStats> GetModerationStatsAsync()
        {
            var reports = await _context.ContentReports
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return new ModerationStats
            {
                PendingCount = reports.FirstOrDefault(r => r.Status == ModerationReportStatus.Pending)?.Count ?? 0,
                ActionedCount = reports.FirstOrDefault(r => r.Status == ModerationReportStatus.Actioned)?.Count ?? 0,
                DismissedCount = reports.FirstOrDefault(r => r.Status == ModerationReportStatus.Dismissed)?.Count ?? 0,
                TotalCount = reports.Sum(r => r.Count)
            };
        }
    }
}
