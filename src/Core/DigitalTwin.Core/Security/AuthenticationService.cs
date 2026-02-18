using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Enums;
using DigitalTwin.Core.Models;

namespace DigitalTwin.Core.Security
{
    public class AuthenticationService
    {
        private readonly PasswordHasher _passwordHasher;
        private readonly SecurityEventLogger _eventLogger;

        // In production, replace with proper user repository backed by database
        private readonly Dictionary<string, StoredUser> _users = new();

        public AuthenticationService(PasswordHasher passwordHasher, SecurityEventLogger eventLogger)
        {
            _passwordHasher = passwordHasher;
            _eventLogger = eventLogger;
        }

        public async Task<UserDTO> RegisterUserAsync(RegisterUserRequest request)
        {
            if (_users.ContainsKey(request.Username.ToLower()))
            {
                throw new InvalidOperationException("Username already exists");
            }

            var userId = Guid.NewGuid().ToString();
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            _users[request.Username.ToLower()] = new StoredUser
            {
                Id = userId,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = UserRole.Viewer,
                CreatedAt = DateTime.UtcNow
            };

            await _eventLogger.LogEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.UserRegistered,
                UserId = userId,
                Description = $"User {request.Username} registered"
            });

            return new UserDTO
            {
                Id = userId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = UserRole.Viewer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<LoginResponse> AuthenticateUserAsync(LoginRequest request)
        {
            if (!_users.TryGetValue(request.Username.ToLower(), out var user))
            {
                await _eventLogger.LogEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.AccessDenied,
                    Description = $"Login failed for unknown user: {request.Username}"
                });
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                await _eventLogger.LogEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.AccessDenied,
                    UserId = user.Id,
                    Description = "Invalid password attempt"
                });
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            await _eventLogger.LogEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.UserLoggedIn,
                UserId = user.Id,
                Description = $"User {user.Username} logged in",
                IsSuccess = true
            });

            return new LoginResponse
            {
                Success = true,
                Token = $"placeholder-jwt-{user.Id}",
                Message = "Login successful"
            };
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            // Validate and refresh token
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                throw new InvalidOperationException("Invalid refresh token");
            }

            return new LoginResponse
            {
                Success = true,
                Token = $"refreshed-jwt-{Guid.NewGuid()}",
                Message = "Token refreshed"
            };
        }

        public async Task LogoutAsync(LogoutRequest request)
        {
            await _eventLogger.LogEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.UserLoggedOut,
                Description = "User logged out"
            });
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            var user = FindUserById(userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid current password");
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);

            await _eventLogger.LogEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.PasswordChanged,
                UserId = userId,
                Description = "Password changed successfully",
                IsSuccess = true
            });
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            return !string.IsNullOrEmpty(token) && token.Length > 10;
        }

        private StoredUser? FindUserById(string userId)
        {
            foreach (var user in _users.Values)
            {
                if (user.Id == userId) return user;
            }
            return null;
        }

        private class StoredUser
        {
            public string Id { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PasswordHash { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public UserRole Role { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class UserDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
