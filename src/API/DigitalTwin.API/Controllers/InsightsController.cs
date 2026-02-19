using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.DTOs;

namespace DigitalTwin.API.Controllers
{
    [ApiController]
    [Route("api/insights")]
    [Authorize]
    public class InsightsController : ControllerBase
    {
        private readonly DigitalTwinDbContext _db;
        private readonly ILogger<InsightsController> _logger;

        public InsightsController(DigitalTwinDbContext db, ILogger<InsightsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        private string GetUserId() =>
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [HttpGet("emotions")]
        public async Task<IActionResult> GetEmotionInsights([FromQuery] string period = "week")
        {
            try
            {
                var userId = GetUserId();

                var since = period switch
                {
                    "month" => DateTime.UtcNow.AddMonths(-1),
                    "all" => DateTime.MinValue,
                    _ => DateTime.UtcNow.AddDays(-7) // "week" default
                };

                var memories = await _db.EmotionalMemories
                    .Where(m => m.UserId == userId && m.Timestamp >= since)
                    .OrderByDescending(m => m.Timestamp)
                    .ToListAsync();

                // Emotion distribution
                var emotionDistribution = memories
                    .GroupBy(m => m.EmotionType.ToString())
                    .ToDictionary(g => g.Key, g => (double)g.Count());

                // Mood timeline (valence by date)
                var moodTimeline = memories
                    .GroupBy(m => m.Timestamp.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new MoodTimelineEntry
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Valence = g.Average(m => m.Intensity)
                    })
                    .ToList();

                // Top emotions
                var totalCount = memories.Count;
                var topEmotions = memories
                    .GroupBy(m => m.EmotionType.ToString())
                    .Select(g => new TopEmotionEntry
                    {
                        Emotion = g.Key,
                        Count = g.Count(),
                        Percentage = totalCount > 0
                            ? Math.Round((double)g.Count() / totalCount * 100, 1)
                            : 0
                    })
                    .OrderByDescending(e => e.Count)
                    .Take(5)
                    .ToList();

                // Session stats
                var sessions = await _db.ConversationSessions
                    .Where(s => s.UserId == userId && s.StartedAt >= since)
                    .ToListAsync();

                var avgDuration = sessions
                    .Where(s => s.EndedAt.HasValue)
                    .Select(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes)
                    .DefaultIfEmpty(0)
                    .Average();

                var result = new EmotionInsightsDto
                {
                    EmotionDistribution = emotionDistribution,
                    MoodTimeline = moodTimeline,
                    SessionCount = sessions.Count,
                    AverageDurationMinutes = Math.Round(avgDuration, 1),
                    TopEmotions = topEmotions
                };

                return Ok(ApiResponse<EmotionInsightsDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching emotion insights");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch emotion insights"));
            }
        }
    }

    public class EmotionInsightsDto
    {
        public Dictionary<string, double> EmotionDistribution { get; set; } = new();
        public List<MoodTimelineEntry> MoodTimeline { get; set; } = new();
        public int SessionCount { get; set; }
        public double AverageDurationMinutes { get; set; }
        public List<TopEmotionEntry> TopEmotions { get; set; } = new();
    }

    public class MoodTimelineEntry
    {
        public string Date { get; set; } = string.Empty;
        public double Valence { get; set; }
    }

    public class TopEmotionEntry
    {
        public string Emotion { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}
