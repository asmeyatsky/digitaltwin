using System;
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
    [Route("api/creative")]
    [Authorize]
    public class CreativeController : ControllerBase
    {
        private readonly ICreativeService _creativeService;
        private readonly ILogger<CreativeController> _logger;

        public CreativeController(ICreativeService creativeService, ILogger<CreativeController> logger)
        {
            _creativeService = creativeService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }

        /// <summary>
        /// Create a new creative work.
        /// </summary>
        [HttpPost("works")]
        public async Task<IActionResult> CreateWork([FromBody] CreateWorkRequest request)
        {
            try
            {
                var work = await _creativeService.CreateWorkAsync(
                    GetUserId(), request.Type, request.Title, request.Content, request.Mood);
                return Ok(ApiResponse<CreativeWork>.Ok(work));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating creative work");
                return StatusCode(500, ApiResponse.Fail("Failed to create creative work"));
            }
        }

        /// <summary>
        /// List the current user's creative works with optional type filter and pagination.
        /// </summary>
        [HttpGet("works")]
        public async Task<IActionResult> GetWorks(
            [FromQuery] CreativeWorkType? type,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var (works, totalCount) = await _creativeService.GetWorksAsync(GetUserId(), type, page, pageSize);
                return Ok(ApiResponse<object>.Ok(new
                {
                    Works = works,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching creative works");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch creative works"));
            }
        }

        /// <summary>
        /// Get a specific creative work by ID.
        /// </summary>
        [HttpGet("works/{id}")]
        public async Task<IActionResult> GetWork(Guid id)
        {
            try
            {
                var work = await _creativeService.GetWorkByIdAsync(GetUserId(), id);
                if (work == null)
                    return NotFound(ApiResponse.Fail("Creative work not found"));

                return Ok(ApiResponse<CreativeWork>.Ok(work));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching creative work");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch creative work"));
            }
        }

        /// <summary>
        /// Update an existing creative work.
        /// </summary>
        [HttpPut("works/{id}")]
        public async Task<IActionResult> UpdateWork(Guid id, [FromBody] UpdateWorkRequest request)
        {
            try
            {
                var work = await _creativeService.UpdateWorkAsync(
                    GetUserId(), id, request.Title, request.Content, request.Mood);
                return Ok(ApiResponse<CreativeWork>.Ok(work));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating creative work");
                return StatusCode(500, ApiResponse.Fail("Failed to update creative work"));
            }
        }

        /// <summary>
        /// Delete a creative work.
        /// </summary>
        [HttpDelete("works/{id}")]
        public async Task<IActionResult> DeleteWork(Guid id)
        {
            try
            {
                await _creativeService.DeleteWorkAsync(GetUserId(), id);
                return Ok(ApiResponse.Ok("Creative work deleted"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting creative work");
                return StatusCode(500, ApiResponse.Fail("Failed to delete creative work"));
            }
        }

        /// <summary>
        /// Share a creative work publicly or to a specific group.
        /// </summary>
        [HttpPost("works/{id}/share")]
        public async Task<IActionResult> ShareWork(Guid id, [FromBody] ShareWorkRequest request)
        {
            try
            {
                var work = await _creativeService.ShareWorkAsync(GetUserId(), id, request.GroupId);
                return Ok(ApiResponse<CreativeWork>.Ok(work));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing creative work");
                return StatusCode(500, ApiResponse.Fail("Failed to share creative work"));
            }
        }

        /// <summary>
        /// Browse shared creative works, optionally filtered by group.
        /// </summary>
        [HttpGet("shared")]
        public async Task<IActionResult> GetSharedWorks(
            [FromQuery] Guid? groupId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var (works, totalCount) = await _creativeService.GetSharedWorksAsync(groupId, page, pageSize);
                return Ok(ApiResponse<object>.Ok(new
                {
                    Works = works,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shared creative works");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch shared works"));
            }
        }

        /// <summary>
        /// Generate an AI creative writing prompt based on type.
        /// </summary>
        [HttpPost("prompt")]
        public async Task<IActionResult> GeneratePrompt([FromBody] GeneratePromptRequest request)
        {
            try
            {
                var prompt = await _creativeService.GeneratePromptAsync(GetUserId(), request.Type);
                return Ok(ApiResponse<object>.Ok(new { Prompt = prompt, Type = request.Type }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating creative prompt");
                return StatusCode(500, ApiResponse.Fail("Failed to generate prompt"));
            }
        }

        /// <summary>
        /// Start a new collaborative story in a shared room.
        /// </summary>
        [HttpPost("stories")]
        public async Task<IActionResult> StartCollaborativeStory([FromBody] StartStoryRequest request)
        {
            try
            {
                var story = await _creativeService.StartCollaborativeStoryAsync(
                    GetUserId(), request.RoomId, request.Title);
                return Ok(ApiResponse<CollaborativeStory>.Ok(story));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting collaborative story");
                return StatusCode(500, ApiResponse.Fail("Failed to start collaborative story"));
            }
        }

        /// <summary>
        /// Add a chapter to a collaborative story.
        /// </summary>
        [HttpPost("stories/{id}/chapters")]
        public async Task<IActionResult> AddChapter(Guid id, [FromBody] AddChapterRequest request)
        {
            try
            {
                var chapter = await _creativeService.AddChapterAsync(GetUserId(), id, request.Content);
                return Ok(ApiResponse<StoryChapter>.Ok(chapter));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding chapter to collaborative story");
                return StatusCode(500, ApiResponse.Fail("Failed to add chapter"));
            }
        }

        /// <summary>
        /// Get a collaborative story with all its chapters.
        /// </summary>
        [HttpGet("stories/{id}")]
        public async Task<IActionResult> GetCollaborativeStory(Guid id)
        {
            try
            {
                var (story, chapters) = await _creativeService.GetCollaborativeStoryAsync(id);
                return Ok(ApiResponse<object>.Ok(new
                {
                    Story = story,
                    Chapters = chapters
                }));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching collaborative story");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch collaborative story"));
            }
        }
    }

    public class CreateWorkRequest
    {
        public CreativeWorkType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Emotion Mood { get; set; }
    }

    public class UpdateWorkRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Emotion Mood { get; set; }
    }

    public class ShareWorkRequest
    {
        public Guid? GroupId { get; set; }
    }

    public class GeneratePromptRequest
    {
        public CreativeWorkType Type { get; set; }
    }

    public class StartStoryRequest
    {
        public Guid RoomId { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class AddChapterRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}
