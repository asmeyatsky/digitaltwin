using System;

namespace DigitalTwin.Core.Entities
{
    public enum AchievementCategory
    {
        Emotional,
        Social,
        Growth,
        Consistency,
        Milestone
    }

    public class AchievementDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconName { get; set; } = string.Empty;
        public AchievementCategory Category { get; set; }
        public int RequiredCount { get; set; } = 1;
    }

    public class UserAchievement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid AchievementDefinitionId { get; set; }
        public int Progress { get; set; } = 0;
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
