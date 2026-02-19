using System;

namespace DigitalTwin.Core.Entities
{
    public enum SessionStatus
    {
        Scheduled,
        Completed,
        Cancelled,
        NoShow
    }

    public enum ScreeningType
    {
        PHQ9,
        GAD7,
        PSS10,
        WHO5
    }

    public enum ReferralUrgency
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class TherapistProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Credentials { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Specializations { get; set; } = "[]"; // JSON array
        public string Availability { get; set; } = "{}"; // JSON
        public decimal RatePerSession { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TherapySession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TherapistId { get; set; }
        public Guid ClientUserId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; } = 50;
        public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ClinicalScreening
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public ScreeningType Type { get; set; }
        public string Responses { get; set; } = "[]"; // JSON array of ints
        public int Score { get; set; }
        public string Severity { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }

    public class TherapistReferral
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public ReferralUrgency Urgency { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
