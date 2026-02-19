using System;

namespace DigitalTwin.Core.Entities
{
    public enum ReportReason
    {
        Harassment,
        Spam,
        SelfHarm,
        Inappropriate,
        Misinformation,
        Other
    }

    public enum ModerationReportStatus
    {
        Pending,
        Reviewed,
        Actioned,
        Dismissed
    }

    public enum ModerationAction
    {
        None,
        Warning,
        ContentRemoved,
        UserSuspended,
        UserBanned
    }

    public enum ContentType
    {
        Post,
        Reply,
        Message
    }

    public class ContentReport
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReporterUserId { get; set; }
        public ContentType ContentType { get; set; }
        public Guid ContentId { get; set; }
        public ReportReason Reason { get; set; }
        public string? Description { get; set; }
        public ModerationReportStatus Status { get; set; } = ModerationReportStatus.Pending;
        public ModerationAction Action { get; set; } = ModerationAction.None;
        public Guid? ReviewedByUserId { get; set; }
        public string? ReviewNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }

    public class AutoModerationResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public ContentType ContentType { get; set; }
        public Guid ContentId { get; set; }
        public bool IsFlagged { get; set; }
        public string? FlagReason { get; set; }
        public double Confidence { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
