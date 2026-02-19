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
    public class AchievementService : IAchievementService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(
            DigitalTwinDbContext context,
            ILogger<AchievementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CheckAndIncrementAsync(Guid userId, string achievementKey, int incrementBy = 1)
        {
            var definition = await _context.AchievementDefinitions
                .FirstOrDefaultAsync(d => d.Key == achievementKey);

            if (definition == null)
            {
                _logger.LogWarning("Achievement definition not found for key: {Key}", achievementKey);
                return false;
            }

            var userAchievement = await _context.UserAchievements
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementDefinitionId == definition.Id);

            if (userAchievement == null)
            {
                userAchievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementDefinitionId = definition.Id,
                    Progress = 0,
                    IsUnlocked = false
                };
                _context.UserAchievements.Add(userAchievement);
            }

            // Already unlocked — nothing to do
            if (userAchievement.IsUnlocked)
                return false;

            userAchievement.Progress += incrementBy;

            if (userAchievement.Progress >= definition.RequiredCount)
            {
                userAchievement.IsUnlocked = true;
                userAchievement.UnlockedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Achievement unlocked: {Key} for user {UserId}",
                    achievementKey, userId);

                await _context.SaveChangesAsync();
                return true;
            }

            await _context.SaveChangesAsync();
            return false;
        }

        public async Task<List<UserAchievementWithDefinition>> GetUserAchievementsAsync(Guid userId)
        {
            // Left join: all definitions with optional user progress
            var query = from d in _context.AchievementDefinitions
                        join ua in _context.UserAchievements.Where(u => u.UserId == userId)
                            on d.Id equals ua.AchievementDefinitionId into joined
                        from ua in joined.DefaultIfEmpty()
                        orderby d.Category, d.Title
                        select new UserAchievementWithDefinition
                        {
                            Id = d.Id,
                            UserId = userId,
                            Key = d.Key,
                            Title = d.Title,
                            Description = d.Description,
                            IconName = d.IconName,
                            Category = d.Category,
                            RequiredCount = d.RequiredCount,
                            Progress = ua != null ? ua.Progress : 0,
                            IsUnlocked = ua != null && ua.IsUnlocked,
                            UnlockedAt = ua != null ? ua.UnlockedAt : null
                        };

            return await query.ToListAsync();
        }

        public async Task<List<UserAchievementWithDefinition>> GetUnlockedAchievementsAsync(Guid userId)
        {
            var query = from d in _context.AchievementDefinitions
                        join ua in _context.UserAchievements.Where(u => u.UserId == userId && u.IsUnlocked)
                            on d.Id equals ua.AchievementDefinitionId
                        orderby ua.UnlockedAt descending
                        select new UserAchievementWithDefinition
                        {
                            Id = d.Id,
                            UserId = userId,
                            Key = d.Key,
                            Title = d.Title,
                            Description = d.Description,
                            IconName = d.IconName,
                            Category = d.Category,
                            RequiredCount = d.RequiredCount,
                            Progress = ua.Progress,
                            IsUnlocked = true,
                            UnlockedAt = ua.UnlockedAt
                        };

            return await query.ToListAsync();
        }

        public async Task SeedAchievementsAsync()
        {
            var builtIn = new List<AchievementDefinition>
            {
                new() { Key = "first_conversation", Title = "First Chat", Description = "Started your first conversation", IconName = "chat", Category = AchievementCategory.Milestone, RequiredCount = 1 },
                new() { Key = "week_streak", Title = "7-Day Streak", Description = "Chatted 7 days in a row", IconName = "fire", Category = AchievementCategory.Consistency, RequiredCount = 7 },
                new() { Key = "month_streak", Title = "30-Day Streak", Description = "Chatted 30 days in a row", IconName = "calendar", Category = AchievementCategory.Consistency, RequiredCount = 30 },
                new() { Key = "emotion_explorer", Title = "Emotion Explorer", Description = "Experienced 5 different emotions", IconName = "compass", Category = AchievementCategory.Emotional, RequiredCount = 5 },
                new() { Key = "memory_keeper", Title = "Memory Keeper", Description = "Created 50 emotional memories", IconName = "brain", Category = AchievementCategory.Emotional, RequiredCount = 50 },
                new() { Key = "goal_setter", Title = "Goal Setter", Description = "Set your first personal goal", IconName = "target", Category = AchievementCategory.Growth, RequiredCount = 1 },
                new() { Key = "goal_achiever", Title = "Goal Achiever", Description = "Completed 5 goals", IconName = "trophy", Category = AchievementCategory.Growth, RequiredCount = 5 },
                new() { Key = "journal_regular", Title = "Journal Regular", Description = "Wrote 10 journal entries", IconName = "book", Category = AchievementCategory.Growth, RequiredCount = 10 },
                new() { Key = "room_explorer", Title = "Room Explorer", Description = "Joined a shared experience", IconName = "users", Category = AchievementCategory.Social, RequiredCount = 1 },
                new() { Key = "voice_activated", Title = "Voice Activated", Description = "Used voice in a conversation", IconName = "mic", Category = AchievementCategory.Milestone, RequiredCount = 1 },
                new() { Key = "avatar_created", Title = "Avatar Created", Description = "Generated your personal avatar", IconName = "image", Category = AchievementCategory.Milestone, RequiredCount = 1 },
                new() { Key = "checkin_champion", Title = "Check-In Champion", Description = "Responded to 10 proactive check-ins", IconName = "bell", Category = AchievementCategory.Consistency, RequiredCount = 10 },
            };

            var existingKeys = await _context.AchievementDefinitions
                .Select(d => d.Key)
                .ToListAsync();

            var toAdd = builtIn.Where(a => !existingKeys.Contains(a.Key)).ToList();

            if (toAdd.Count > 0)
            {
                _context.AchievementDefinitions.AddRange(toAdd);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} achievement definitions", toAdd.Count);
            }
        }
    }
}
