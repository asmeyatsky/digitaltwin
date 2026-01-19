using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.Models;
using DigitalTwin.Core.Enums;

namespace DigitalTwin.Core.Security
{
    /// <summary>
    /// Role-based access control (RBAC) service
    /// </summary>
    public class RoleBasedAccessControlService
    {
        private readonly Dictionary<UserRole, List<Permission>> _rolePermissions;
        private readonly Dictionary<string, UserRole> _userRoles;
        private readonly Dictionary<string, List<Permission>> _userSpecificPermissions;

        public RoleBasedAccessControlService()
        {
            _rolePermissions = new Dictionary<UserRole, List<Permission>>();
            _userRoles = new Dictionary<string, UserRole>();
            _userSpecificPermissions = new Dictionary<string, List<Permission>>();

            InitializeDefaultRolesAndPermissions();
        }

        private void InitializeDefaultRolesAndPermissions()
        {
            // Define permissions for each role
            _rolePermissions[UserRole.SuperAdmin] = Enum.GetValues(typeof(Permission)).Cast<Permission>().ToList();
            
            _rolePermissions[UserRole.Admin] = new List<Permission>
            {
                Permission.ViewDashboard,
                Permission.ViewBuildings,
                Permission.ManageBuildings,
                Permission.ViewSensors,
                Permission.ManageSensors,
                Permission.ViewAnalytics,
                Permission.ExportData,
                Permission.ViewAuditLogs,
                Permission.ManageUsers
            };

            _rolePermissions[UserRole.Manager] = new List<Permission>
            {
                Permission.ViewDashboard,
                Permission.ViewBuildings,
                Permission.ManageBuildings,
                Permission.ViewSensors,
                Permission.ManageSensors,
                Permission.ViewAnalytics,
                Permission.ExportData
            };

            _rolePermissions[UserRole.Operator] = new List<Permission>
            {
                Permission.ViewDashboard,
                Permission.ViewBuildings,
                Permission.ViewSensors,
                Permission.ManageSensors,
                Permission.ViewAnalytics
            };

            _rolePermissions[UserRole.Viewer] = new List<Permission>
            {
                Permission.ViewDashboard,
                Permission.ViewBuildings,
                Permission.ViewSensors
            };

            _rolePermissions[UserRole.Guest] = new List<Permission>
            {
                Permission.ViewDashboard
            };
        }

        /// <summary>
        /// Assigns a role to a user
        /// </summary>
        public async Task AssignRoleAsync(string userId, UserRole role)
        {
            _userRoles[userId] = role;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets the role assigned to a user
        /// </summary>
        public async Task<UserRole?> GetUserRoleAsync(string userId)
        {
            _userRoles.TryGetValue(userId, out var role);
            return await Task.FromResult(role);
        }

        /// <summary>
        /// Gets all permissions for a user (role-based + user-specific)
        /// </summary>
        public async Task<List<Permission>> GetUserPermissionsAsync(string userId)
        {
            var permissions = new HashSet<Permission>();

            // Add role-based permissions
            if (_userRoles.TryGetValue(userId, out var userRole))
            {
                if (_rolePermissions.TryGetValue(userRole, out var rolePerms))
                {
                    foreach (var perm in rolePerms)
                    {
                        permissions.Add(perm);
                    }
                }
            }

            // Add user-specific permissions
            if (_userSpecificPermissions.TryGetValue(userId, out var userPerms))
            {
                foreach (var perm in userPerms)
                {
                    permissions.Add(perm);
                }
            }

            return permissions.ToList();
        }

        /// <summary>
        /// Checks if a user has a specific permission
        /// </summary>
        public async Task<bool> HasPermissionAsync(string userId, Permission permission)
        {
            var userPermissions = await GetUserPermissionsAsync(userId);
            return userPermissions.Contains(permission);
        }

        /// <summary>
        /// Checks if a user has any of the specified permissions
        /// </summary>
        public async Task<bool> HasAnyPermissionAsync(string userId, params Permission[] permissions)
        {
            var userPermissions = await GetUserPermissionsAsync(userId);
            return permissions.Any(perm => userPermissions.Contains(perm));
        }

        /// <summary>
        /// Checks if a user has all of the specified permissions
        /// </summary>
        public async Task<bool> HasAllPermissionsAsync(string userId, params Permission[] permissions)
        {
            var userPermissions = await GetUserPermissionsAsync(userId);
            return permissions.All(perm => userPermissions.Contains(perm));
        }

        /// <summary>
        /// Grants a specific permission to a user
        /// </summary>
        public async Task GrantPermissionAsync(string userId, Permission permission)
        {
            if (!_userSpecificPermissions.ContainsKey(userId))
            {
                _userSpecificPermissions[userId] = new List<Permission>();
            }

            if (!_userSpecificPermissions[userId].Contains(permission))
            {
                _userSpecificPermissions[userId].Add(permission);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Revokes a specific permission from a user
        /// </summary>
        public async Task RevokePermissionAsync(string userId, Permission permission)
        {
            if (_userSpecificPermissions.ContainsKey(userId))
            {
                _userSpecificPermissions[userId].Remove(permission);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets all users with a specific role
        /// </summary>
        public async Task<List<string>> GetUsersInRoleAsync(UserRole role)
        {
            var users = _userRoles
                .Where(kvp => kvp.Value == role)
                .Select(kvp => kvp.Key)
                .ToList();

            return await Task.FromResult(users);
        }

        /// <summary>
        /// Gets all users with a specific permission
        /// </summary>
        public async Task<List<string>> GetUsersWithPermissionAsync(Permission permission)
        {
            var users = new List<string>();

            foreach (var userId in _userRoles.Keys.Concat(_userSpecificPermissions.Keys).Distinct())
            {
                if (await HasPermissionAsync(userId, permission))
                {
                    users.Add(userId);
                }
            }

            return users;
        }

        /// <summary>
        /// Creates a new custom role with specific permissions
        /// </summary>
        public async Task CreateCustomRoleAsync(UserRole role, List<Permission> permissions)
        {
            if (!_rolePermissions.ContainsKey(role))
            {
                _rolePermissions[role] = permissions;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets permissions for a specific role
        /// </summary>
        public async Task<List<Permission>> GetRolePermissionsAsync(UserRole role)
        {
            _rolePermissions.TryGetValue(role, out var permissions);
            return await Task.FromResult(permissions ?? new List<Permission>());
        }

        /// <summary>
        /// Updates permissions for a role
        /// </summary>
        public async Task UpdateRolePermissionsAsync(UserRole role, List<Permission> permissions)
        {
            _rolePermissions[role] = permissions;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Removes a user from all roles and permissions
        /// </summary>
        public async Task RemoveUserAsync(string userId)
        {
            _userRoles.Remove(userId);
            _userSpecificPermissions.Remove(userId);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets a summary of all roles and their permissions
        /// </summary>
        public async Task<Dictionary<UserRole, List<Permission>>> GetAllRolePermissionsAsync()
        {
            return await Task.FromResult(_rolePermissions.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList()
            ));
        }
    }

    /// <summary>
    /// Extension methods for RBAC
    /// </summary>
    public static class RbacExtensions
    {
        /// <summary>
        /// Gets the user ID from claims principal
        /// </summary>
        public static string? GetUserId(this System.Security.Claims.ClaimsPrincipal principal)
        {
            return principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Gets the user role from claims principal
        /// </summary>
        public static UserRole? GetUserRole(this System.Security.Claims.ClaimsPrincipal principal)
        {
            var roleClaim = principal?.FindFirst("role")?.Value;
            if (Enum.TryParse<UserRole>(roleClaim, out var role))
            {
                return role;
            }
            return null;
        }

        /// <summary>
        /// Checks if the user has a specific permission using the RBAC service
        /// </summary>
        public static async Task<bool> HasPermissionAsync(
            this System.Security.Claims.ClaimsPrincipal principal,
            RoleBasedAccessControlService rbacService,
            Permission permission)
        {
            var userId = principal.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            return await rbacService.HasPermissionAsync(userId, permission);
        }
    }
}