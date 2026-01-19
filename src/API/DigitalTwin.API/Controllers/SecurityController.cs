using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Security;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Enums;
using DigitalTwin.Infrastructure.Attributes;

namespace DigitalTwin.API.Controllers
{
    /// <summary>
    /// Security management API controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SecurityController : ControllerBase
    {
        private readonly AuthenticationService _authService;
        private readonly RoleBasedAccessControlService _rbacService;
        private readonly SecurityEventLogger _securityEventLogger;

        public SecurityController(
            AuthenticationService authService,
            RoleBasedAccessControlService rbacService,
            SecurityEventLogger securityEventLogger)
        {
            _authService = authService;
            _rbacService = rbacService;
            _securityEventLogger = securityEventLogger;
        }

        /// <summary>
        /// User registration endpoint
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDTO>> RegisterUser([FromBody] RegisterUserRequest request)
        {
            try
            {
                var user = await _authService.RegisterUserAsync(request);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// User login endpoint
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.AuthenticateUserAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { error = "Invalid credentials" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Token refresh endpoint
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request);
                return Ok(result);
            }
            catch (SecurityTokenException)
            {
                return Unauthorized(new { error = "Invalid refresh token" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// User logout endpoint
        /// </summary>
        [HttpPost("logout")]
        public async Task<ActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                await _authService.LogoutAsync(request);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Change password endpoint
        /// </summary>
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = User.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                await _authService.ChangePasswordAsync(userId, request);
                return Ok(new { message = "Password changed successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest(new { error = "Invalid current password" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<UserDTO>> GetCurrentUser()
        {
            try
            {
                var userId = User.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // In a real implementation, this would get user from user service
                // For now, return a mock user
                var user = new UserDTO
                {
                    Id = userId,
                    Email = "user@example.com",
                    FirstName = "User",
                    LastName = "Name",
                    Role = UserRole.Viewer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user by ID (Admin only)
        /// </summary>
        [HttpGet("users/{id}")]
        [RequirePermission(Permission.ManageUsers)]
        public async Task<ActionResult<UserDTO>> GetUser(string id)
        {
            try
            {
                // In a real implementation, this would get user from user service
                // For now, return a mock user
                var user = new UserDTO
                {
                    Id = id,
                    Email = "user@example.com",
                    FirstName = "User",
                    LastName = "Name",
                    Role = UserRole.Viewer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        [HttpGet("users")]
        [RequirePermission(Permission.ManageUsers)]
        public async Task<ActionResult<List<UserDTO>>> GetAllUsers()
        {
            try
            {
                // In a real implementation, this would get users from user service
                // For now, return mock users
                var users = new List<UserDTO>
                {
                    new UserDTO
                    {
                        Id = "user1",
                        Email = "admin@example.com",
                        FirstName = "Admin",
                        LastName = "User",
                        Role = UserRole.SuperAdmin,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        LastLoginAt = DateTime.UtcNow.AddHours(-2)
                    },
                    new UserDTO
                    {
                        Id = "user2",
                        Email = "manager@example.com",
                        FirstName = "Manager",
                        LastName = "User",
                        Role = UserRole.Manager,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-20),
                        LastLoginAt = DateTime.UtcNow.AddHours(-4)
                    }
                };

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Assign role to user (Admin only)
        /// </summary>
        [HttpPost("users/{userId}/roles")]
        [RequirePermission(Permission.ManageRoles)]
        public async Task<ActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request)
        {
            try
            {
                await _rbacService.AssignRoleAsync(userId, request.Role);
                return Ok(new { message = "Role assigned successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user role
        /// </summary>
        [HttpGet("users/{userId}/role")]
        public async Task<ActionResult<UserRole>> GetUserRole(string userId)
        {
            try
            {
                var currentUserId = User.GetUserId();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                // Users can only view their own role unless they have manage users permission
                if (currentUserId != userId && !await _rbacService.HasPermissionAsync(currentUserId, Permission.ManageUsers))
                {
                    return Forbid();
                }

                var role = await _rbacService.GetUserRoleAsync(userId);
                if (role == null)
                {
                    return NotFound();
                }

                return Ok(role);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user permissions
        /// </summary>
        [HttpGet("users/{userId}/permissions")]
        public async Task<ActionResult<List<Permission>>> GetUserPermissions(string userId)
        {
            try
            {
                var currentUserId = User.GetUserId();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                // Users can only view their own permissions unless they have manage users permission
                if (currentUserId != userId && !await _rbacService.HasPermissionAsync(currentUserId, Permission.ManageUsers))
                {
                    return Forbid();
                }

                var permissions = await _rbacService.GetUserPermissionsAsync(userId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Grant permission to user (Admin only)
        /// </summary>
        [HttpPost("users/{userId}/permissions")]
        [RequirePermission(Permission.ManageRoles)]
        public async Task<ActionResult> GrantPermission(string userId, [FromBody] GrantPermissionRequest request)
        {
            try
            {
                await _rbacService.GrantPermissionAsync(userId, request.Permission);
                return Ok(new { message = "Permission granted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Revoke permission from user (Admin only)
        /// </summary>
        [HttpDelete("users/{userId}/permissions/{permission}")]
        [RequirePermission(Permission.ManageRoles)]
        public async Task<ActionResult> RevokePermission(string userId, Permission permission)
        {
            try
            {
                await _rbacService.RevokePermissionAsync(userId, permission);
                return Ok(new { message = "Permission revoked successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all roles and their permissions (Admin only)
        /// </summary>
        [HttpGet("roles")]
        [RequirePermission(Permission.ManageRoles)]
        public async Task<ActionResult<Dictionary<UserRole, List<Permission>>>> GetAllRoles()
        {
            try
            {
                var roles = await _rbacService.GetAllRolePermissionsAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get security audit logs (Admin only)
        /// </summary>
        [HttpGet("audit-logs")]
        [RequirePermission(Permission.ViewAuditLogs)]
        public async Task<ActionResult<List<Core.Models.SecurityEvent>>> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] SecurityEventType? eventType = null,
            [FromQuery] string? userId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var logs = await _securityEventLogger.GetSecurityEventsAsync(page, pageSize);
                
                // Apply filters (in a real implementation, this would be done in the repository)
                if (eventType.HasValue)
                {
                    logs = logs.FindAll(l => l.EventType == eventType.Value);
                }
                
                if (!string.IsNullOrEmpty(userId))
                {
                    logs = logs.FindAll(l => l.UserId == userId);
                }
                
                if (startDate.HasValue)
                {
                    logs = logs.FindAll(l => l.Timestamp >= startDate.Value);
                }
                
                if (endDate.HasValue)
                {
                    logs = logs.FindAll(l => l.Timestamp <= endDate.Value);
                }

                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Export audit logs (Admin only)
        /// </summary>
        [HttpGet("audit-logs/export")]
        [RequirePermission(Permission.ExportData)]
        public async Task<ActionResult> ExportAuditLogs(
            [FromQuery] SecurityEventType? eventType = null,
            [FromQuery] string? userId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var logs = await _securityEventLogger.GetSecurityEventsAsync();
                
                // Apply filters
                if (eventType.HasValue)
                {
                    logs = logs.FindAll(l => l.EventType == eventType.Value);
                }
                
                if (!string.IsNullOrEmpty(userId))
                {
                    logs = logs.FindAll(l => l.UserId == userId);
                }
                
                if (startDate.HasValue)
                {
                    logs = logs.FindAll(l => l.Timestamp >= startDate.Value);
                }
                
                if (endDate.HasValue)
                {
                    logs = logs.FindAll(l => l.Timestamp <= endDate.Value);
                }

                var csvData = ConvertLogsToCSV(logs);
                var filename = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                
                return File(System.Text.Encoding.UTF8.GetBytes(csvData), "text/csv", filename);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate token endpoint
        /// </summary>
        [HttpPost("validate-token")]
        public async Task<ActionResult<bool>> ValidateToken([FromBody] ValidateTokenRequest request)
        {
            try
            {
                var isValid = await _authService.ValidateTokenAsync(request.Token);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private string ConvertLogsToCSV(List<Core.Models.SecurityEvent> logs)
        {
            var csv = "Timestamp,Event Type,User ID,Description,IP Address,User Agent,Request Path\n";
            
            foreach (var log in logs.OrderByDescending(l => l.Timestamp))
            {
                csv += $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                       $"{log.EventType}," +
                       $"\"{log.UserId}\"," +
                       $"\"{log.Description}\"," +
                       $"{log.IpAddress}," +
                       $"\"{log.UserAgent}\"," +
                       $"{log.RequestPath}\n";
            }
            
            return csv;
        }
    }

    /// <summary>
    /// Request DTOs for security endpoints
    /// </summary>
    public class AssignRoleRequest
    {
        public UserRole Role { get; set; }
    }

    public class GrantPermissionRequest
    {
        public Permission Permission { get; set; }
    }

    public class ValidateTokenRequest
    {
        public string Token { get; set; }
    }
}