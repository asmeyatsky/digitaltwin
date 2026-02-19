using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CoachingController : ControllerBase
    {
        private readonly ICoachingService _coachingService;
        private readonly ILogger<CoachingController> _logger;

        public CoachingController(ICoachingService coachingService, ILogger<CoachingController> logger)
        {
            _coachingService = coachingService;
            _logger = logger;
        }

        private string GetUserId() =>
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        // Goals
        [HttpPost("goals")]
        public async Task<IActionResult> CreateGoal([FromBody] CreateGoalRequest request)
        {
            try
            {
                var goal = new Goal
                {
                    UserId = GetUserId(),
                    Title = request.Title,
                    Description = request.Description,
                    Category = request.Category,
                    TargetDate = request.TargetDate
                };

                var result = await _coachingService.SetGoalAsync(goal);
                return Ok(ApiResponse<Goal>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal");
                return StatusCode(500, ApiResponse.Fail("Failed to create goal"));
            }
        }

        [HttpGet("goals")]
        public async Task<IActionResult> GetGoals([FromQuery] string? status = null)
        {
            try
            {
                var goals = await _coachingService.GetGoalsAsync(GetUserId(), status);
                return Ok(ApiResponse<List<Goal>>.Ok(goals));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching goals");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch goals"));
            }
        }

        [HttpPut("goals/{id}/progress")]
        public async Task<IActionResult> UpdateGoalProgress(Guid id, [FromBody] UpdateProgressRequest request)
        {
            try
            {
                var goal = await _coachingService.UpdateGoalProgressAsync(id, request.Progress);
                if (goal == null)
                    return NotFound(ApiResponse.Fail("Goal not found"));

                return Ok(ApiResponse<Goal>.Ok(goal));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal progress");
                return StatusCode(500, ApiResponse.Fail("Failed to update goal progress"));
            }
        }

        // Journal
        [HttpPost("journal")]
        public async Task<IActionResult> CreateJournalEntry([FromBody] CreateJournalRequest request)
        {
            try
            {
                var entry = new JournalEntry
                {
                    UserId = GetUserId(),
                    Content = request.Content,
                    Mood = request.Mood,
                    Tags = request.Tags ?? new List<string>()
                };

                var result = await _coachingService.CreateJournalEntryAsync(entry);
                return Ok(ApiResponse<JournalEntry>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating journal entry");
                return StatusCode(500, ApiResponse.Fail("Failed to create journal entry"));
            }
        }

        [HttpGet("journal")]
        public async Task<IActionResult> GetJournalEntries([FromQuery] int limit = 20)
        {
            try
            {
                var entries = await _coachingService.GetJournalEntriesAsync(GetUserId(), limit);
                return Ok(ApiResponse<List<JournalEntry>>.Ok(entries));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching journal entries");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch journal entries"));
            }
        }

        // Habits
        [HttpPost("habits")]
        public async Task<IActionResult> LogHabit([FromBody] LogHabitRequest request)
        {
            try
            {
                var record = new HabitRecord
                {
                    UserId = GetUserId(),
                    HabitName = request.HabitName,
                    Completed = request.Completed,
                    Date = request.Date?.Date ?? DateTime.UtcNow.Date
                };

                var result = await _coachingService.LogHabitAsync(record);
                return Ok(ApiResponse<HabitRecord>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging habit");
                return StatusCode(500, ApiResponse.Fail("Failed to log habit"));
            }
        }

        [HttpGet("habits/{habitName}/streak")]
        public async Task<IActionResult> GetHabitStreak(string habitName)
        {
            try
            {
                var streak = await _coachingService.GetHabitStreakAsync(GetUserId(), habitName);
                return Ok(ApiResponse<object>.Ok(new { habitName, streak }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching habit streak");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch habit streak"));
            }
        }

        // Insights
        [HttpGet("insights")]
        public async Task<IActionResult> GetInsight()
        {
            try
            {
                var insight = await _coachingService.GenerateCoachingInsightAsync(GetUserId());
                return Ok(ApiResponse<object>.Ok(new { insight }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating coaching insight");
                return StatusCode(500, ApiResponse.Fail("Failed to generate insight"));
            }
        }
    }

    public class CreateGoalRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime? TargetDate { get; set; }
    }

    public class UpdateProgressRequest
    {
        public double Progress { get; set; }
    }

    public class CreateJournalRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? Mood { get; set; }
        public List<string>? Tags { get; set; }
    }

    public class LogHabitRequest
    {
        public string HabitName { get; set; } = string.Empty;
        public bool Completed { get; set; } = true;
        public DateTime? Date { get; set; }
    }
}
