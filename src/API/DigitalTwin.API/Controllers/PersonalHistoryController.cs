using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Enums;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.API.Controllers
{
    [ApiController]
    [Route("api/personal-history")]
    [Authorize]
    public class PersonalHistoryController : ControllerBase
    {
        private readonly IPersonalHistoryService _personalHistoryService;
        private readonly ILogger<PersonalHistoryController> _logger;

        public PersonalHistoryController(
            IPersonalHistoryService personalHistoryService,
            ILogger<PersonalHistoryController> logger)
        {
            _personalHistoryService = personalHistoryService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var raw = User.FindFirst("userId")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? string.Empty;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }

        // POST /api/personal-history/events
        [HttpPost("events")]
        public async Task<IActionResult> AddLifeEvent([FromBody] AddLifeEventRequest request)
        {
            try
            {
                var evt = new LifeEvent
                {
                    Title = request.Title,
                    Description = request.Description,
                    EventDate = request.EventDate,
                    Category = request.Category,
                    EmotionalImpact = request.EmotionalImpact,
                    IsRecurring = request.IsRecurring
                };

                var result = await _personalHistoryService.AddLifeEventAsync(GetUserId(), evt);
                return Ok(ApiResponse<LifeEvent>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding life event");
                return StatusCode(500, ApiResponse.Fail("Failed to add life event"));
            }
        }

        // PUT /api/personal-history/events/{id}
        [HttpPut("events/{id}")]
        public async Task<IActionResult> UpdateLifeEvent(Guid id, [FromBody] AddLifeEventRequest request)
        {
            try
            {
                var evt = new LifeEvent
                {
                    Title = request.Title,
                    Description = request.Description,
                    EventDate = request.EventDate,
                    Category = request.Category,
                    EmotionalImpact = request.EmotionalImpact,
                    IsRecurring = request.IsRecurring
                };

                var result = await _personalHistoryService.UpdateLifeEventAsync(GetUserId(), id, evt);
                if (result == null)
                    return NotFound(ApiResponse.Fail("Life event not found"));

                return Ok(ApiResponse<LifeEvent>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating life event");
                return StatusCode(500, ApiResponse.Fail("Failed to update life event"));
            }
        }

        // DELETE /api/personal-history/events/{id}
        [HttpDelete("events/{id}")]
        public async Task<IActionResult> DeleteLifeEvent(Guid id)
        {
            try
            {
                var deleted = await _personalHistoryService.DeleteLifeEventAsync(GetUserId(), id);
                if (!deleted)
                    return NotFound(ApiResponse.Fail("Life event not found"));

                return Ok(ApiResponse<object>.Ok(new { deleted = true }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting life event");
                return StatusCode(500, ApiResponse.Fail("Failed to delete life event"));
            }
        }

        // GET /api/personal-history/timeline?start={date}&end={date}
        [HttpGet("timeline")]
        public async Task<IActionResult> GetTimeline([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            try
            {
                var events = await _personalHistoryService.GetTimelineAsync(GetUserId(), start, end);
                return Ok(ApiResponse<List<LifeEvent>>.Ok(events));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching timeline");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch timeline"));
            }
        }

        // GET /api/personal-history/upcoming
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingEvents([FromQuery] int daysAhead = 30)
        {
            try
            {
                var events = await _personalHistoryService.GetUpcomingEventsAsync(GetUserId(), daysAhead);
                return Ok(ApiResponse<List<LifeEvent>>.Ok(events));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching upcoming events");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch upcoming events"));
            }
        }

        // GET /api/personal-history/context
        [HttpGet("context")]
        public async Task<IActionResult> GetPersonalContext()
        {
            try
            {
                var context = await _personalHistoryService.GetPersonalContextAsync(GetUserId());
                return Ok(ApiResponse<PersonalContext?>.Ok(context));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching personal context");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch personal context"));
            }
        }

        // PUT /api/personal-history/context
        [HttpPut("context")]
        public async Task<IActionResult> UpdatePersonalContext([FromBody] UpdatePersonalContextRequest request)
        {
            try
            {
                var context = new PersonalContext
                {
                    CulturalBackground = request.CulturalBackground,
                    CommunicationPreferences = request.CommunicationPreferences,
                    ImportantPeople = request.ImportantPeople,
                    Values = request.Values
                };

                var result = await _personalHistoryService.UpdatePersonalContextAsync(GetUserId(), context);
                return Ok(ApiResponse<PersonalContext>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating personal context");
                return StatusCode(500, ApiResponse.Fail("Failed to update personal context"));
            }
        }
    }

    public class AddLifeEventRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public LifeEventCategory Category { get; set; } = LifeEventCategory.Milestone;
        public Emotion EmotionalImpact { get; set; } = Emotion.Neutral;
        public bool IsRecurring { get; set; }
    }

    public class UpdatePersonalContextRequest
    {
        public string CulturalBackground { get; set; } = string.Empty;
        public string CommunicationPreferences { get; set; } = "{}";
        public string ImportantPeople { get; set; } = "[]";
        public string Values { get; set; } = "[]";
    }
}
