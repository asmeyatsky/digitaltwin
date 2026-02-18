using System;
using System.Collections.Generic;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Entities
{
    /// <summary>
    /// Represents an emotional memory stored by the system
    /// </summary>
    public class EmotionalMemory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        public EmotionType EmotionType { get; set; }
        public EmotionType PrimaryEmotion { get; set; }
        public double Intensity { get; set; }
        public string Context { get; set; }
        public string Trigger { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ImportanceScore { get; set; }
        public List<EmotionType> AssociatedEmotions { get; set; } = new();
        public List<string> EmotionTags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Alias for EmotionType for backward compatibility
        /// </summary>
        public EmotionType Emotion => EmotionType;
    }

    /// <summary>
    /// Types of emotions detected
    /// </summary>
    [Obsolete("Use DigitalTwin.Core.Enums.Emotion enum with EmotionMapper instead.")]
    public enum EmotionType
    {
        Happy,
        Sad,
        Angry,
        Fearful,
        Surprised,
        Disgusted,
        Neutral,
        Excited,
        Anxious,
        Calm,
        Frustrated,
        Curious,
        Fear,
        Disgust,
        Surprise,
        Content
    }

    /// <summary>
    /// Represents a conversation session
    /// </summary>
    public class ConversationSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public List<ConversationMessage> Messages { get; set; } = new();
        public EmotionType DominantEmotion { get; set; }
        public EmotionType CurrentEmotionalState { get; set; }
        public int MessageCount { get; set; }
        public Dictionary<string, object> SessionContext { get; set; } = new();
        public Dictionary<string, object> ConversationContext { get; set; } = new();
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// A message within a conversation
    /// </summary>
    public class ConversationMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Role { get; set; } // "user" or "assistant"
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public EmotionType DetectedEmotion { get; set; }
    }

    /// <summary>
    /// Response from conversation processing
    /// </summary>
    public class ConversationResponse
    {
        public string Content { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }
        public bool Success { get; set; }
        public EmotionType DetectedEmotion { get; set; }
        public double Confidence { get; set; }
        public double EmotionalIntensity { get; set; }
        public double PersonalizationLevel { get; set; }
        public List<string> MemoryReferences { get; set; } = new();
        public DateTime? ResponseTime { get; set; }
        public Guid? SessionId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Memory from conversation context
    /// </summary>
    public class ConversationMemory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAccessed { get; set; }
        public double Importance { get; set; }
    }

    /// <summary>
    /// Trend data for emotional analysis
    /// </summary>
    public class EmotionalTrend
    {
        public Guid UserId { get; set; }
        public EmotionType Emotion { get; set; }
        public EmotionType DominantEmotion { get; set; }
        public double Frequency { get; set; }
        public double AverageIntensity { get; set; }
        public double EmotionalStability { get; set; }
        public double Confidence { get; set; }
        public TrendDirection TrendDirection { get; set; }
        public TimeSpan Period { get; set; }
        public DateTime AnalysisDate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
