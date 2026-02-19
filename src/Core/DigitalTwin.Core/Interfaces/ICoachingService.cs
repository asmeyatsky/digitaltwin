using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface ICoachingService
    {
        Task<Goal> SetGoalAsync(Goal goal);
        Task<Goal?> UpdateGoalProgressAsync(Guid goalId, double progress);
        Task<List<Goal>> GetGoalsAsync(string userId, string? status = null);
        Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry);
        Task<List<JournalEntry>> GetJournalEntriesAsync(string userId, int limit = 20);
        Task<HabitRecord> LogHabitAsync(HabitRecord record);
        Task<int> GetHabitStreakAsync(string userId, string habitName);
        Task<string> GenerateCoachingInsightAsync(string userId);
    }
}
