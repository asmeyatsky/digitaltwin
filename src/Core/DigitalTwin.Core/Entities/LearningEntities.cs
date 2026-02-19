using System;

namespace DigitalTwin.Core.Entities
{
    public enum LearningCategory
    {
        EmotionalIntelligence,
        Mindfulness,
        Communication,
        StressManagement,
        Resilience,
        SelfCare
    }

    public class LearningPath
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public LearningCategory Category { get; set; }
        public int EstimatedMinutes { get; set; }
        public int ModuleCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class LearningModule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PathId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ExercisePrompt { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class UserLearningProgress
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid PathId { get; set; }
        public int CurrentModuleIndex { get; set; }
        public string CompletedModules { get; set; } = "[]";
        public string ReflectionNotes { get; set; } = "{}";
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
