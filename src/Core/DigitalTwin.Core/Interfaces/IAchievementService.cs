using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface IAchievementService
    {
        Task<bool> CheckAndIncrementAsync(Guid userId, string achievementKey, int incrementBy = 1);
        Task<List<UserAchievementWithDefinition>> GetUserAchievementsAsync(Guid userId);
        Task<List<UserAchievementWithDefinition>> GetUnlockedAchievementsAsync(Guid userId);
        Task SeedAchievementsAsync();
    }

    /// <summary>
    /// Projection that joins UserAchievement with its AchievementDefinition
    /// for API responses.
    /// </summary>
    public class UserAchievementWithDefinition
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconName { get; set; } = string.Empty;
        public AchievementCategory Category { get; set; }
        public int Progress { get; set; }
        public int RequiredCount { get; set; }
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
    }
}
