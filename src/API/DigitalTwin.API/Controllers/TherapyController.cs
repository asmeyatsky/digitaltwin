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
    [Route("api/therapy")]
    [Authorize]
    public class TherapyController : ControllerBase
    {
        private readonly ITherapyService _therapyService;
        private readonly ILogger<TherapyController> _logger;

        public TherapyController(ITherapyService therapyService, ILogger<TherapyController> logger)
        {
            _therapyService = therapyService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }

        /// <summary>
        /// List therapists with optional specialization filter and pagination.
        /// </summary>
        [HttpGet("therapists")]
        public async Task<IActionResult> GetTherapists(
            [FromQuery] string? specialization,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var (therapists, totalCount) = await _therapyService.GetTherapistsAsync(specialization, page, pageSize);
                return Ok(ApiResponse<object>.Ok(new
                {
                    Therapists = therapists,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching therapists");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch therapists"));
            }
        }

        /// <summary>
        /// Get a specific therapist by ID.
        /// </summary>
        [HttpGet("therapists/{id}")]
        public async Task<IActionResult> GetTherapist(Guid id)
        {
            try
            {
                var therapist = await _therapyService.GetTherapistByIdAsync(id);
                if (therapist == null)
                    return NotFound(ApiResponse.Fail("Therapist not found"));

                return Ok(ApiResponse<TherapistProfile>.Ok(therapist));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching therapist");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch therapist"));
            }
        }

        /// <summary>
        /// Book a session with a therapist.
        /// </summary>
        [HttpPost("sessions")]
        public async Task<IActionResult> BookSession([FromBody] BookSessionRequest request)
        {
            try
            {
                var session = await _therapyService.BookSessionAsync(GetUserId(), request.TherapistId, request.ScheduledAt);
                return Ok(ApiResponse<TherapySession>.Ok(session));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking therapy session");
                return StatusCode(500, ApiResponse.Fail("Failed to book session"));
            }
        }

        /// <summary>
        /// Cancel a scheduled session.
        /// </summary>
        [HttpPost("sessions/{id}/cancel")]
        public async Task<IActionResult> CancelSession(Guid id)
        {
            try
            {
                await _therapyService.CancelSessionAsync(GetUserId(), id);
                return Ok(ApiResponse.Ok("Session cancelled successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling therapy session");
                return StatusCode(500, ApiResponse.Fail("Failed to cancel session"));
            }
        }

        /// <summary>
        /// Get the current user's therapy sessions.
        /// </summary>
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var (sessions, totalCount) = await _therapyService.GetUserSessionsAsync(GetUserId(), page, pageSize);
                return Ok(ApiResponse<object>.Ok(new
                {
                    Sessions = sessions,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching therapy sessions");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch sessions"));
            }
        }

        /// <summary>
        /// Get screening questionnaire questions for a given type.
        /// </summary>
        [HttpGet("screening/{type}/questions")]
        public async Task<IActionResult> GetScreeningQuestions(ScreeningType type)
        {
            try
            {
                var (screeningType, questions) = await _therapyService.GetScreeningQuestionsAsync(type);
                return Ok(ApiResponse<object>.Ok(new
                {
                    Type = screeningType.ToString(),
                    Questions = questions
                }));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching screening questions");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch screening questions"));
            }
        }

        /// <summary>
        /// Submit screening responses and get scored result.
        /// </summary>
        [HttpPost("screening/{type}/submit")]
        public async Task<IActionResult> SubmitScreening(ScreeningType type, [FromBody] SubmitScreeningRequest request)
        {
            try
            {
                var screening = await _therapyService.SubmitScreeningAsync(GetUserId(), type, request.Responses);
                return Ok(ApiResponse<ClinicalScreening>.Ok(screening));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting screening");
                return StatusCode(500, ApiResponse.Fail("Failed to submit screening"));
            }
        }

        /// <summary>
        /// Get the user's screening history.
        /// </summary>
        [HttpGet("screening/history")]
        public async Task<IActionResult> GetScreeningHistory()
        {
            try
            {
                var history = await _therapyService.GetScreeningHistoryAsync(GetUserId());
                return Ok(ApiResponse<List<ClinicalScreening>>.Ok(history));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching screening history");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch screening history"));
            }
        }

        /// <summary>
        /// Get the user's therapist referrals.
        /// </summary>
        [HttpGet("referrals")]
        public async Task<IActionResult> GetReferrals()
        {
            try
            {
                var referrals = await _therapyService.GetUserReferralsAsync(GetUserId());
                return Ok(ApiResponse<List<TherapistReferral>>.Ok(referrals));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching referrals");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch referrals"));
            }
        }
    }

    public class BookSessionRequest
    {
        public Guid TherapistId { get; set; }
        public DateTime ScheduledAt { get; set; }
    }

    public class SubmitScreeningRequest
    {
        public List<int> Responses { get; set; } = new();
    }
}
