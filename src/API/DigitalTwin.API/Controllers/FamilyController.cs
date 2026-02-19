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
    [Route("api/family")]
    [Authorize]
    public class FamilyController : ControllerBase
    {
        private readonly IFamilyService _familyService;
        private readonly ILogger<FamilyController> _logger;

        public FamilyController(IFamilyService familyService, ILogger<FamilyController> logger)
        {
            _familyService = familyService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }

        /// <summary>
        /// Create a new family household.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateFamily([FromBody] CreateFamilyRequest request)
        {
            try
            {
                var family = await _familyService.CreateFamilyAsync(GetUserId(), request.Name);
                return Ok(ApiResponse<Family>.Ok(family));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating family");
                return StatusCode(500, ApiResponse.Fail("Failed to create family"));
            }
        }

        /// <summary>
        /// Get the current user's family and its members.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFamily()
        {
            try
            {
                var family = await _familyService.GetFamilyAsync(GetUserId());
                if (family == null)
                    return Ok(ApiResponse<object?>.Ok(null, "User does not belong to a family"));

                var members = await _familyService.GetFamilyMembersAsync(family.Id);

                return Ok(ApiResponse<object>.Ok(new
                {
                    Family = family,
                    Members = members
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching family");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch family"));
            }
        }

        /// <summary>
        /// Invite a member to the family.
        /// </summary>
        [HttpPost("{id}/invite")]
        public async Task<IActionResult> InviteMember(Guid id, [FromBody] InviteMemberRequest request)
        {
            try
            {
                var invite = await _familyService.InviteMemberAsync(GetUserId(), id, request.Email, request.Role);
                return Ok(ApiResponse<FamilyInvite>.Ok(invite));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting family member");
                return StatusCode(500, ApiResponse.Fail("Failed to invite member"));
            }
        }

        /// <summary>
        /// Accept a family invite using an invite code.
        /// </summary>
        [HttpPost("join")]
        public async Task<IActionResult> JoinFamily([FromBody] JoinFamilyRequest request)
        {
            try
            {
                var member = await _familyService.AcceptInviteAsync(GetUserId(), request.InviteCode);
                return Ok(ApiResponse<FamilyMember>.Ok(member));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining family");
                return StatusCode(500, ApiResponse.Fail("Failed to join family"));
            }
        }

        /// <summary>
        /// Remove a member from the family (owner only).
        /// </summary>
        [HttpDelete("{familyId}/members/{memberUserId}")]
        public async Task<IActionResult> RemoveMember(Guid familyId, Guid memberUserId)
        {
            try
            {
                await _familyService.RemoveMemberAsync(GetUserId(), familyId, memberUserId);
                return Ok(ApiResponse.Ok("Member removed successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing family member");
                return StatusCode(500, ApiResponse.Fail("Failed to remove member"));
            }
        }

        /// <summary>
        /// Get shared emotional insights for the family.
        /// </summary>
        [HttpGet("{id}/insights")]
        public async Task<IActionResult> GetInsights(Guid id)
        {
            try
            {
                var insights = await _familyService.GetSharedInsightsAsync(id);
                return Ok(ApiResponse<FamilyInsights>.Ok(insights));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching family insights");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch family insights"));
            }
        }
    }

    public class CreateFamilyRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class InviteMemberRequest
    {
        public string Email { get; set; } = string.Empty;
        public FamilyRole Role { get; set; } = FamilyRole.Adult;
    }

    public class JoinFamilyRequest
    {
        public string InviteCode { get; set; } = string.Empty;
    }
}
