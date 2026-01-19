using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.Security;
using DigitalTwin.Core.Enums;

namespace DigitalTwin.Infrastructure.Attributes
{
    /// <summary>
    /// Custom authorization attribute that checks permissions using RBAC
    /// </summary>
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly Permission[] _permissions;

        public RequirePermissionAttribute(params Permission[] permissions)
        {
            _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            if (_permissions.Length == 0)
            {
                throw new ArgumentException("At least one permission must be specified", nameof(permissions));
            }
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var rbacService = context.HttpContext.RequestServices.GetRequiredService<RoleBasedAccessControlService>();
            var logger = context.HttpContext.RequestServices.GetService<Microsoft.Extensions.Logging.ILogger<RequirePermissionAttribute>>();

            var userId = context.HttpContext.User?.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                await LogSecurityEventAsync(context, SecurityEventType.AccessDenied, "User not authenticated");
                return;
            }

            // Check if user has ANY of the required permissions
            var hasPermission = await rbacService.HasAnyPermissionAsync(userId, _permissions);
            
            if (!hasPermission)
            {
                context.Result = new ForbidResult();
                await LogSecurityEventAsync(context, SecurityEventType.AccessDenied, 
                    $"User lacks required permissions: {string.Join(", ", _permissions)}");
                
                logger?.LogWarning("User {UserId} denied access - missing permissions: {Permissions}", 
                    userId, string.Join(", ", _permissions));
                return;
            }

            await LogSecurityEventAsync(context, SecurityEventType.AccessGranted, 
                $"Access granted with permissions: {string.Join(", ", _permissions)}");
        }

        private async Task LogSecurityEventAsync(AuthorizationFilterContext context, SecurityEventType eventType, string description)
        {
            try
            {
                var securityEventLogger = context.HttpContext.RequestServices.GetRequiredService<ISecurityEventLogger>();
                var userId = context.HttpContext.User?.GetUserId();
                var actionDescriptor = context.ActionDescriptor.DisplayName;
                
                await securityEventLogger.LogSecurityEventAsync(new Core.Models.SecurityEvent
                {
                    EventType = eventType,
                    UserId = userId,
                    Description = $"{description} for action: {actionDescriptor}",
                    IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
                    RequestPath = context.HttpContext.Request.Path,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch
            {
                // Don't let logging failures break the authorization flow
            }
        }
    }

    /// <summary>
    /// Custom authorization attribute that requires all specified permissions
    /// </summary>
    public class RequireAllPermissionsAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly Permission[] _permissions;

        public RequireAllPermissionsAttribute(params Permission[] permissions)
        {
            _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            if (_permissions.Length == 0)
            {
                throw new ArgumentException("At least one permission must be specified", nameof(permissions));
            }
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var rbacService = context.HttpContext.RequestServices.GetRequiredService<RoleBasedAccessControlService>();
            var logger = context.HttpContext.RequestServices.GetService<Microsoft.Extensions.Logging.ILogger<RequireAllPermissionsAttribute>>();

            var userId = context.HttpContext.User?.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                await LogSecurityEventAsync(context, SecurityEventType.AccessDenied, "User not authenticated");
                return;
            }

            // Check if user has ALL of the required permissions
            var hasAllPermissions = await rbacService.HasAllPermissionsAsync(userId, _permissions);
            
            if (!hasAllPermissions)
            {
                context.Result = new ForbidResult();
                await LogSecurityEventAsync(context, SecurityEventType.AccessDenied, 
                    $"User lacks required permissions: {string.Join(", ", _permissions)}");
                
                logger?.LogWarning("User {UserId} denied access - missing permissions: {Permissions}", 
                    userId, string.Join(", ", _permissions));
                return;
            }

            await LogSecurityEventAsync(context, SecurityEventType.AccessGranted, 
                $"Access granted with all permissions: {string.Join(", ", _permissions)}");
        }

        private async Task LogSecurityEventAsync(AuthorizationFilterContext context, SecurityEventType eventType, string description)
        {
            try
            {
                var securityEventLogger = context.HttpContext.RequestServices.GetRequiredService<ISecurityEventLogger>();
                var userId = context.HttpContext.User?.GetUserId();
                var actionDescriptor = context.ActionDescriptor.DisplayName;
                
                await securityEventLogger.LogSecurityEventAsync(new Core.Models.SecurityEvent
                {
                    EventType = eventType,
                    UserId = userId,
                    Description = $"{description} for action: {actionDescriptor}",
                    IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
                    RequestPath = context.HttpContext.Request.Path,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch
            {
                // Don't let logging failures break the authorization flow
            }
        }
    }

    /// <summary>
    /// Custom authorization attribute that requires a specific role
    /// </summary>
    public class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly UserRole[] _roles;

        public RequireRoleAttribute(params UserRole[] roles)
        {
            _roles = roles ?? throw new ArgumentNullException(nameof(roles));
            if (_roles.Length == 0)
            {
                throw new ArgumentException("At least one role must be specified", nameof(roles));
            }
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var rbacService = context.HttpContext.RequestServices.GetRequiredService<RoleBasedAccessControlService>();
            var logger = context.HttpContext.RequestServices.GetService<Microsoft.Extensions.Logging.ILogger<RequireRoleAttribute>>();

            var userId = context.HttpContext.User?.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                await LogSecurityEventAsync(context, SecurityEventType.AccessDenied, "User not authenticated");
                return;
            }

            var userRole = await rbacService.GetUserRoleAsync(userId);
            
            if (userRole == null || !_roles.Contains(userRole.Value))
            {
                context.Result = new ForbidResult();
                await LogSecurityEventAsync(context, SecurityEventType.AccessDenied, 
                    $"User role {userRole} not in required roles: {string.Join(", ", _roles)}");
                
                logger?.LogWarning("User {UserId} denied access - role {UserRole} not in required roles: {Roles}", 
                    userId, userRole, string.Join(", ", _roles));
                return;
            }

            await LogSecurityEventAsync(context, SecurityEventType.AccessGranted, 
                $"Access granted with role: {userRole}");
        }

        private async Task LogSecurityEventAsync(AuthorizationFilterContext context, SecurityEventType eventType, string description)
        {
            try
            {
                var securityEventLogger = context.HttpContext.RequestServices.GetRequiredService<ISecurityEventLogger>();
                var userId = context.HttpContext.User?.GetUserId();
                var actionDescriptor = context.ActionDescriptor.DisplayName;
                
                await securityEventLogger.LogSecurityEventAsync(new Core.Models.SecurityEvent
                {
                    EventType = eventType,
                    UserId = userId,
                    Description = $"{description} for action: {actionDescriptor}",
                    IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
                    RequestPath = context.HttpContext.Request.Path,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch
            {
                // Don't let logging failures break the authorization flow
            }
        }
    }
}