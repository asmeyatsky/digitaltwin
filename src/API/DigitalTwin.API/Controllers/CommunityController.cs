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
    [Route("api/community")]
    [Authorize]
    public class CommunityController : ControllerBase
    {
        private readonly ICommunityService _communityService;
        private readonly ILogger<CommunityController> _logger;

        public CommunityController(ICommunityService communityService, ILogger<CommunityController> logger)
        {
            _communityService = communityService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }

        /// <summary>
        /// Create a new community group.
        /// </summary>
        [HttpPost("groups")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            try
            {
                var group = await _communityService.CreateGroupAsync(
                    GetUserId(), request.Name, request.Description, request.Category);
                return Ok(ApiResponse<CommunityGroup>.Ok(group));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating community group");
                return StatusCode(500, ApiResponse.Fail("Failed to create group"));
            }
        }

        /// <summary>
        /// List/search community groups with optional category filter and search.
        /// </summary>
        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups(
            [FromQuery] GroupCategory? category,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var (groups, totalCount) = await _communityService.GetGroupsAsync(category, search, page, pageSize);
                return Ok(ApiResponse<object>.Ok(new
                {
                    Groups = groups,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching community groups");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch groups"));
            }
        }

        /// <summary>
        /// Get a specific community group by ID.
        /// </summary>
        [HttpGet("groups/{id}")]
        public async Task<IActionResult> GetGroup(Guid id)
        {
            try
            {
                var group = await _communityService.GetGroupByIdAsync(id);
                if (group == null)
                    return NotFound(ApiResponse.Fail("Group not found"));

                return Ok(ApiResponse<CommunityGroup>.Ok(group));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching community group");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch group"));
            }
        }

        /// <summary>
        /// Join a community group.
        /// </summary>
        [HttpPost("groups/{id}/join")]
        public async Task<IActionResult> JoinGroup(Guid id)
        {
            try
            {
                var membership = await _communityService.JoinGroupAsync(GetUserId(), id);
                return Ok(ApiResponse<CommunityMembership>.Ok(membership));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining community group");
                return StatusCode(500, ApiResponse.Fail("Failed to join group"));
            }
        }

        /// <summary>
        /// Leave a community group.
        /// </summary>
        [HttpPost("groups/{id}/leave")]
        public async Task<IActionResult> LeaveGroup(Guid id)
        {
            try
            {
                await _communityService.LeaveGroupAsync(GetUserId(), id);
                return Ok(ApiResponse.Ok("Left group successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving community group");
                return StatusCode(500, ApiResponse.Fail("Failed to leave group"));
            }
        }

        /// <summary>
        /// Create a post in a community group.
        /// </summary>
        [HttpPost("groups/{groupId}/posts")]
        public async Task<IActionResult> CreatePost(Guid groupId, [FromBody] CreatePostRequest request)
        {
            try
            {
                var post = await _communityService.CreatePostAsync(
                    GetUserId(), groupId, request.Title, request.Content, request.IsAnonymous);
                return Ok(ApiResponse<CommunityPost>.Ok(post));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating community post");
                return StatusCode(500, ApiResponse.Fail("Failed to create post"));
            }
        }

        /// <summary>
        /// List posts in a community group.
        /// </summary>
        [HttpGet("groups/{groupId}/posts")]
        public async Task<IActionResult> GetPosts(Guid groupId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var (posts, totalCount) = await _communityService.GetPostsAsync(groupId, page, pageSize);
                return Ok(ApiResponse<object>.Ok(new
                {
                    Posts = posts,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching community posts");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch posts"));
            }
        }

        /// <summary>
        /// Get a specific post with its details.
        /// </summary>
        [HttpGet("posts/{postId}")]
        public async Task<IActionResult> GetPost(Guid postId)
        {
            try
            {
                var post = await _communityService.GetPostByIdAsync(postId);
                if (post == null)
                    return NotFound(ApiResponse.Fail("Post not found"));

                var (replies, replyCount) = await _communityService.GetRepliesAsync(postId, 1, 50);
                return Ok(ApiResponse<object>.Ok(new
                {
                    Post = post,
                    Replies = replies,
                    ReplyCount = replyCount
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching community post");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch post"));
            }
        }

        /// <summary>
        /// Reply to a community post.
        /// </summary>
        [HttpPost("posts/{postId}/replies")]
        public async Task<IActionResult> ReplyToPost(Guid postId, [FromBody] CreateReplyRequest request)
        {
            try
            {
                var reply = await _communityService.ReplyToPostAsync(
                    GetUserId(), postId, request.Content, request.IsAnonymous);
                return Ok(ApiResponse<CommunityReply>.Ok(reply));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to community post");
                return StatusCode(500, ApiResponse.Fail("Failed to reply to post"));
            }
        }

        /// <summary>
        /// Like a community post.
        /// </summary>
        [HttpPost("posts/{postId}/like")]
        public async Task<IActionResult> LikePost(Guid postId)
        {
            try
            {
                await _communityService.LikePostAsync(GetUserId(), postId);
                return Ok(ApiResponse.Ok("Post liked"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking community post");
                return StatusCode(500, ApiResponse.Fail("Failed to like post"));
            }
        }

        /// <summary>
        /// Like a community reply.
        /// </summary>
        [HttpPost("replies/{replyId}/like")]
        public async Task<IActionResult> LikeReply(Guid replyId)
        {
            try
            {
                await _communityService.LikeReplyAsync(GetUserId(), replyId);
                return Ok(ApiResponse.Ok("Reply liked"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking community reply");
                return StatusCode(500, ApiResponse.Fail("Failed to like reply"));
            }
        }

        /// <summary>
        /// Get the current user's joined groups.
        /// </summary>
        [HttpGet("my-groups")]
        public async Task<IActionResult> GetMyGroups()
        {
            try
            {
                var groups = await _communityService.GetUserGroupsAsync(GetUserId());
                return Ok(ApiResponse<object>.Ok(new { Groups = groups }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user's community groups");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch your groups"));
            }
        }

        /// <summary>
        /// Get suggested groups the user hasn't joined yet.
        /// </summary>
        [HttpGet("suggested")]
        public async Task<IActionResult> GetSuggestedGroups()
        {
            try
            {
                var groups = await _communityService.GetSuggestedGroupsAsync(GetUserId());
                return Ok(ApiResponse<object>.Ok(new { Groups = groups }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching suggested community groups");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch suggested groups"));
            }
        }
    }

    public class CreateGroupRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GroupCategory Category { get; set; }
    }

    public class CreatePostRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
    }

    public class CreateReplyRequest
    {
        public string Content { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
    }
}
