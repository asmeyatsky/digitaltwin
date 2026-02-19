using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.API.Controllers
{
    [ApiController]
    [Route("api/moderation")]
    [Authorize]
    public class ModerationController : ControllerBase
    {
        private readonly IModerationService _moderationService;
        private readonly ILogger<ModerationController> _logger;

        public ModerationController(IModerationService moderationService, ILogger<ModerationController> logger)
        {
            _moderationService = moderationService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }

        private bool IsAdminOrModerator()
        {
            return User.IsInRole("admin") || User.IsInRole("moderator");
        }

        /// <summary>
        /// Report content for moderation review.
        /// </summary>
        [HttpPost("report")]
        public async Task<IActionResult> ReportContent([FromBody] ReportContentRequest request)
        {
            try
            {
                var report = await _moderationService.ReportContentAsync(
                    GetUserId(), request.ContentType, request.ContentId,
                    request.Reason, request.Description);
                return Ok(ApiResponse<ContentReport>.Ok(report));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting content");
                return StatusCode(500, ApiResponse.Fail("Failed to report content"));
            }
        }

        /// <summary>
        /// Get pending moderation reports (admin/moderator only).
        /// </summary>
        [HttpGet("reports")]
        public async Task<IActionResult> GetPendingReports(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (!IsAdminOrModerator())
                return Forbid();

            try
            {
                var result = await _moderationService.GetPendingReportsAsync(page, pageSize);
                return Ok(ApiResponse<PaginatedReports>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending reports");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch pending reports"));
            }
        }

        /// <summary>
        /// Review a report and take action (admin/moderator only).
        /// </summary>
        [HttpPost("reports/{id}/review")]
        public async Task<IActionResult> ReviewReport(Guid id, [FromBody] ReviewReportRequest request)
        {
            if (!IsAdminOrModerator())
                return Forbid();

            try
            {
                var report = await _moderationService.ReviewReportAsync(
                    GetUserId(), id, request.Action, request.Notes);
                return Ok(ApiResponse<ContentReport>.Ok(report));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing report {ReportId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to review report"));
            }
        }

        /// <summary>
        /// Dismiss a report (admin/moderator only).
        /// </summary>
        [HttpPost("reports/{id}/dismiss")]
        public async Task<IActionResult> DismissReport(Guid id, [FromBody] DismissReportRequest request)
        {
            if (!IsAdminOrModerator())
                return Forbid();

            try
            {
                var report = await _moderationService.DismissReportAsync(
                    GetUserId(), id, request.Notes);
                return Ok(ApiResponse<ContentReport>.Ok(report));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing report {ReportId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to dismiss report"));
            }
        }

        /// <summary>
        /// Get moderation statistics (admin/moderator only).
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetModerationStats()
        {
            if (!IsAdminOrModerator())
                return Forbid();

            try
            {
                var stats = await _moderationService.GetModerationStatsAsync();
                return Ok(ApiResponse<ModerationStats>.Ok(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching moderation stats");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch moderation stats"));
            }
        }
    }

    public class ReportContentRequest
    {
        public ContentType ContentType { get; set; }
        public Guid ContentId { get; set; }
        public ReportReason Reason { get; set; }
        public string? Description { get; set; }
    }

    public class ReviewReportRequest
    {
        public ModerationAction Action { get; set; }
        public string? Notes { get; set; }
    }

    public class DismissReportRequest
    {
        public string? Notes { get; set; }
    }
}
