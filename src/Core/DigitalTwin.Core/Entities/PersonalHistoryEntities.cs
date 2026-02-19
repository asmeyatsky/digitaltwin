using System;
using System.Collections.Generic;
using DigitalTwin.Core.Enums;

namespace DigitalTwin.Core.Entities
{
    public enum LifeEventCategory
    {
        Career,
        Relationship,
        Health,
        Education,
        Milestone,
        Loss,
        Achievement,
        Travel
    }

    public class LifeEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public LifeEventCategory Category { get; set; } = LifeEventCategory.Milestone;
        public Emotion EmotionalImpact { get; set; } = Emotion.Neutral;
        public bool IsRecurring { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class PersonalContext
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string CulturalBackground { get; set; } = string.Empty;
        public string CommunicationPreferences { get; set; } = "{}"; // JSON
        public string ImportantPeople { get; set; } = "[]"; // JSON list
        public string Values { get; set; } = "[]"; // JSON list
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Summary object returned by GetContextForConversationAsync
    /// for LLM prompt enrichment.
    /// </summary>
    public class ConversationLifeContext
    {
        public List<LifeEvent> RecentEvents { get; set; } = new();
        public List<LifeEvent> UpcomingEvents { get; set; } = new();
        public PersonalContext? PersonalContext { get; set; }
    }
}
