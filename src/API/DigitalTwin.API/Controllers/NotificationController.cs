using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly IPushNotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            IPushNotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpPost("register-device")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Fail("User not authenticated"));

            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest(ApiResponse.Fail("Token is required"));

            if (string.IsNullOrWhiteSpace(request.Platform) ||
                !new[] { "ios", "android", "web" }.Contains(request.Platform.ToLowerInvariant()))
                return BadRequest(ApiResponse.Fail("Platform must be 'ios', 'android', or 'web'"));

            await _notificationService.RegisterDeviceAsync(
                Guid.Parse(userId), request.Token, request.Platform.ToLowerInvariant());

            return Ok(ApiResponse.Ok("Device registered successfully"));
        }

        [HttpDelete("unregister-device")]
        public async Task<IActionResult> UnregisterDevice([FromBody] UnregisterDeviceRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Fail("User not authenticated"));

            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest(ApiResponse.Fail("Token is required"));

            await _notificationService.UnregisterDeviceAsync(Guid.Parse(userId), request.Token);

            return Ok(ApiResponse.Ok("Device unregistered successfully"));
        }
    }

    public class RegisterDeviceRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
    }

    public class UnregisterDeviceRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
