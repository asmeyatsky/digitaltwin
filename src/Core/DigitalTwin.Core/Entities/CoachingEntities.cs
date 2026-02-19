using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.Entities
{
    public class Goal
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // health, career, relationships, personal_growth, etc.
        public DateTime? TargetDate { get; set; }
        public double Progress { get; set; } // 0.0 to 1.0
        public string Status { get; set; } = "active"; // active, completed, abandoned
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class JournalEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Mood { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class HabitRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string HabitName { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    }
}
