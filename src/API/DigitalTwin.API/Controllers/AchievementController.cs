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
    [Route("api/achievements")]
    [Authorize]
    public class AchievementController : ControllerBase
    {
        private readonly IAchievementService _achievementService;
        private readonly ILogger<AchievementController> _logger;

        public AchievementController(IAchievementService achievementService, ILogger<AchievementController> logger)
        {
            _achievementService = achievementService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }

        /// <summary>
        /// Get all achievement definitions.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllDefinitions()
        {
            try
            {
                var achievements = await _achievementService.GetUserAchievementsAsync(GetUserId());
                return Ok(ApiResponse<List<UserAchievementWithDefinition>>.Ok(achievements));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching achievement definitions");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch achievements"));
            }
        }

        /// <summary>
        /// Get the current user's achievements with progress.
        /// </summary>
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyAchievements()
        {
            try
            {
                var achievements = await _achievementService.GetUserAchievementsAsync(GetUserId());
                return Ok(ApiResponse<List<UserAchievementWithDefinition>>.Ok(achievements));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user achievements");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch user achievements"));
            }
        }

        /// <summary>
        /// Get only unlocked achievements for the current user.
        /// </summary>
        [HttpGet("unlocked")]
        public async Task<IActionResult> GetUnlockedAchievements()
        {
            try
            {
                var achievements = await _achievementService.GetUnlockedAchievementsAsync(GetUserId());
                return Ok(ApiResponse<List<UserAchievementWithDefinition>>.Ok(achievements));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unlocked achievements");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch unlocked achievements"));
            }
        }
    }
}
