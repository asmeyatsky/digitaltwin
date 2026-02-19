using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CheckInController : ControllerBase
    {
        private readonly IProactiveCheckInService _checkInService;
        private readonly ILogger<CheckInController> _logger;

        public CheckInController(
            IProactiveCheckInService checkInService,
            ILogger<CheckInController> logger)
        {
            _checkInService = checkInService;
            _logger = logger;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingCheckIns()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var checkIns = await _checkInService.GetPendingCheckInsAsync(userId);
            return Ok(new { success = true, data = checkIns });
        }

        [HttpPost("{id}/respond")]
        public async Task<IActionResult> RespondToCheckIn(Guid id, [FromBody] CheckInRespondRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _checkInService.RespondToCheckInAsync(id, request.Response);
            return Ok(new { success = true });
        }

        [HttpPost("evaluate")]
        public async Task<IActionResult> EvaluateCheckIn()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var suggestion = await _checkInService.EvaluateCheckInAsync(userId);
            if (suggestion == null)
                return Ok(new { success = true, data = (object?)null });

            // Record the check-in
            await _checkInService.RecordCheckInAsync(userId, suggestion.Type, suggestion.EmotionContext);

            return Ok(new { success = true, data = suggestion });
        }
    }

    public class CheckInRespondRequest
    {
        public string Response { get; set; } = string.Empty;
    }
}
