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
    [Route("api/learning")]
    [Authorize]
    public class LearningController : ControllerBase
    {
        private readonly ILearningService _learningService;
        private readonly ILogger<LearningController> _logger;

        public LearningController(ILearningService learningService, ILogger<LearningController> logger)
        {
            _learningService = learningService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }

        /// <summary>
        /// List available learning paths, optionally filtered by category.
        /// </summary>
        [HttpGet("paths")]
        public async Task<IActionResult> GetPaths([FromQuery] LearningCategory? category)
        {
            try
            {
                var paths = await _learningService.GetPathsAsync(category);
                return Ok(ApiResponse<object>.Ok(new { Paths = paths }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching learning paths");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch learning paths"));
            }
        }

        /// <summary>
        /// Get a specific learning path with all its modules.
        /// </summary>
        [HttpGet("paths/{id}")]
        public async Task<IActionResult> GetPath(Guid id)
        {
            try
            {
                var (path, modules) = await _learningService.GetPathByIdAsync(id);
                if (path == null)
                    return NotFound(ApiResponse.Fail("Learning path not found"));

                return Ok(ApiResponse<object>.Ok(new { Path = path, Modules = modules }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching learning path");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch learning path"));
            }
        }

        /// <summary>
        /// Start a learning path.
        /// </summary>
        [HttpPost("paths/{id}/start")]
        public async Task<IActionResult> StartPath(Guid id)
        {
            try
            {
                var progress = await _learningService.StartPathAsync(GetUserId(), id);
                return Ok(ApiResponse<UserLearningProgress>.Ok(progress));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting learning path");
                return StatusCode(500, ApiResponse.Fail("Failed to start learning path"));
            }
        }

        /// <summary>
        /// Get the current module for a learning path in progress.
        /// </summary>
        [HttpGet("paths/{id}/current")]
        public async Task<IActionResult> GetCurrentModule(Guid id)
        {
            try
            {
                var (module, progress) = await _learningService.GetCurrentModuleAsync(GetUserId(), id);
                if (progress == null)
                    return NotFound(ApiResponse.Fail("You have not started this learning path"));

                return Ok(ApiResponse<object>.Ok(new { Module = module, Progress = progress }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current module");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch current module"));
            }
        }

        /// <summary>
        /// Complete the current module and advance to the next one.
        /// </summary>
        [HttpPost("paths/{id}/complete-module")]
        public async Task<IActionResult> CompleteModule(Guid id, [FromBody] CompleteModuleRequest? request)
        {
            try
            {
                var progress = await _learningService.CompleteModuleAsync(
                    GetUserId(), id, request?.ReflectionNotes);
                return Ok(ApiResponse<UserLearningProgress>.Ok(progress));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing module");
                return StatusCode(500, ApiResponse.Fail("Failed to complete module"));
            }
        }

        /// <summary>
        /// Get all learning progress for the current user.
        /// </summary>
        [HttpGet("progress")]
        public async Task<IActionResult> GetProgress()
        {
            try
            {
                var progress = await _learningService.GetProgressAsync(GetUserId());
                return Ok(ApiResponse<object>.Ok(new { Progress = progress }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching learning progress");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch learning progress"));
            }
        }

        /// <summary>
        /// Get a suggested learning path based on user's least-explored category.
        /// </summary>
        [HttpGet("suggested")]
        public async Task<IActionResult> GetSuggested()
        {
            try
            {
                var path = await _learningService.GetSuggestedPathAsync(GetUserId());
                return Ok(ApiResponse<LearningPath?>.Ok(path));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching suggested learning path");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch suggested path"));
            }
        }
    }

    public class CompleteModuleRequest
    {
        public string? ReflectionNotes { get; set; }
    }
}
