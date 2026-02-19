using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class CoachingService : ICoachingService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CoachingService> _logger;

        public CoachingService(
            DigitalTwinDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<CoachingService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<Goal> SetGoalAsync(Goal goal)
        {
            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();
            return goal;
        }

        public async Task<Goal?> UpdateGoalProgressAsync(Guid goalId, double progress)
        {
            var goal = await _context.Goals.FindAsync(goalId);
            if (goal == null) return null;

            goal.Progress = Math.Clamp(progress, 0.0, 1.0);
            goal.UpdatedAt = DateTime.UtcNow;

            if (goal.Progress >= 1.0)
                goal.Status = "completed";

            await _context.SaveChangesAsync();
            return goal;
        }

        public async Task<List<Goal>> GetGoalsAsync(string userId, string? status = null)
        {
            var query = _context.Goals.Where(g => g.UserId == userId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(g => g.Status == status);

            return await query.OrderByDescending(g => g.CreatedAt).ToListAsync();
        }

        public async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry)
        {
            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<List<JournalEntry>> GetJournalEntriesAsync(string userId, int limit = 20)
        {
            return await _context.JournalEntries
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<HabitRecord> LogHabitAsync(HabitRecord record)
        {
            var existing = await _context.HabitRecords
                .FirstOrDefaultAsync(h => h.UserId == record.UserId
                    && h.HabitName == record.HabitName
                    && h.Date == record.Date);

            if (existing != null)
            {
                existing.Completed = record.Completed;
                await _context.SaveChangesAsync();
                return existing;
            }

            _context.HabitRecords.Add(record);
            await _context.SaveChangesAsync();
            return record;
        }

        public async Task<int> GetHabitStreakAsync(string userId, string habitName)
        {
            var records = await _context.HabitRecords
                .Where(h => h.UserId == userId && h.HabitName == habitName && h.Completed)
                .OrderByDescending(h => h.Date)
                .ToListAsync();

            if (records.Count == 0) return 0;

            var streak = 0;
            var expectedDate = DateTime.UtcNow.Date;

            foreach (var record in records)
            {
                if (record.Date == expectedDate)
                {
                    streak++;
                    expectedDate = expectedDate.AddDays(-1);
                }
                else if (record.Date == expectedDate.AddDays(-1))
                {
                    // Allow skipping today if not logged yet
                    streak++;
                    expectedDate = record.Date.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        public async Task<string> GenerateCoachingInsightAsync(string userId)
        {
            try
            {
                var goals = await GetGoalsAsync(userId, "active");
                var journals = await GetJournalEntriesAsync(userId, 5);

                var context = "User context:\n";
                if (goals.Any())
                    context += $"Active goals: {string.Join(", ", goals.Select(g => $"{g.Title} ({g.Progress:P0})"))}\n";
                if (journals.Any())
                    context += $"Recent journal moods: {string.Join(", ", journals.Where(j => j.Mood != null).Select(j => j.Mood))}\n";

                var client = _httpClientFactory.CreateClient("LLM");
                var serviceKey = Environment.GetEnvironmentVariable("Services__ServiceKey") ?? "dev-service-key";
                client.DefaultRequestHeaders.Add("X-Service-Key", serviceKey);

                var response = await client.PostAsJsonAsync("/generate-response", new
                {
                    message = $"Generate a brief, personalized coaching insight based on this context. Be encouraging and actionable. {context}",
                    emotion = "calm",
                    context = "life_coaching"
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CoachingLlmResponse>();
                    return result?.Response ?? GetFallbackInsight(goals);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate coaching insight via LLM");
            }

            var fallbackGoals = await GetGoalsAsync(userId, "active");
            return GetFallbackInsight(fallbackGoals);
        }

        private static string GetFallbackInsight(List<Goal> goals)
        {
            if (!goals.Any())
                return "Consider setting a goal to work towards. Even small goals can build momentum and a sense of achievement.";

            var topGoal = goals.OrderByDescending(g => g.Progress).First();
            return $"Great progress on \"{topGoal.Title}\" ({topGoal.Progress:P0} complete)! Keep up the momentum — consistency is key to lasting change.";
        }

        private class CoachingLlmResponse
        {
            public string Response { get; set; } = string.Empty;
        }
    }
}
