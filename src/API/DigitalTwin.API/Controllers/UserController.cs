using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.DTOs;

namespace DigitalTwin.API.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly DigitalTwinDbContext _db;
        private readonly ILogger<UserController> _logger;

        public UserController(DigitalTwinDbContext db, ILogger<UserController> logger)
        {
            _db = db;
            _logger = logger;
        }

        private string GetUserId() =>
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = GetUserId();
                var profile = await _db.AITwinProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                {
                    return NotFound(ApiResponse.Fail("Profile not found"));
                }

                var result = new UserProfileDto
                {
                    Id = profile.Id.ToString(),
                    DisplayName = profile.Name,
                    AvatarUrl = profile.Preferences?.ContainsKey("avatarUrl") == true
                        ? profile.Preferences["avatarUrl"]?.ToString()
                        : null,
                    CreatedAt = profile.CreationDate,
                    UserId = profile.UserId
                };

                return Ok(ApiResponse<UserProfileDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user profile");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch user profile"));
            }
        }

        [HttpPatch("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = GetUserId();
                var profile = await _db.AITwinProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                {
                    return NotFound(ApiResponse.Fail("Profile not found"));
                }

                if (!string.IsNullOrEmpty(request.DisplayName))
                {
                    profile.Name = request.DisplayName;
                }

                if (request.AvatarUrl != null)
                {
                    profile.Preferences ??= new();
                    profile.Preferences["avatarUrl"] = request.AvatarUrl;
                }

                profile.LastInteraction = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var result = new UserProfileDto
                {
                    Id = profile.Id.ToString(),
                    DisplayName = profile.Name,
                    AvatarUrl = profile.Preferences?.ContainsKey("avatarUrl") == true
                        ? profile.Preferences["avatarUrl"]?.ToString()
                        : null,
                    CreatedAt = profile.CreationDate,
                    UserId = profile.UserId
                };

                return Ok(ApiResponse<UserProfileDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, ApiResponse.Fail("Failed to update user profile"));
            }
        }
    }

    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
