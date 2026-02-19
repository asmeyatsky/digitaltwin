using System;

namespace DigitalTwin.Core.Entities
{
    public class CheckInRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public DateTime? SentAt { get; set; }
        public string Type { get; set; } = "daily"; // daily, weekly, mood_triggered
        public string? EmotionContext { get; set; }
        public string? Response { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
