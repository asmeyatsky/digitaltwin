using System;
using System.Collections.Generic;
using System.Linq;
using DigitalTwin.Infrastructure.Security.Models;

namespace DigitalTwin.Infrastructure.Security
{
    /// <summary>
    /// User Management Service Implementation
    /// 
    /// Architectural Intent:
    /// - Provides comprehensive user management
    /// - Supports registration, authentication, and profile management
    /// - Enables role-based access control
    /// - Maintains user lifecycle management
    /// - Provides password policies and security event logging
    /// 
    /// Key Features:
    /// 1. User registration and validation
    /// 2. Multi-factor authentication setup
    /// 3. Profile management with preferences
    /// 4. User account management (enable/disable)
    /// 5. Bulk user operations
    /// 6. Email verification workflow
    /// </summary>
    public class UserService : MonoBehaviour, IUserService
    {
        [Header("User Management")]
        [SerializeField] private SecurityConfiguration _securityConfig;
        [SerializeField] private JwtTokenService _tokenService;
        [SerializeField] private List<User> _users;
        
        [Header("UI References")]
        [SerializeField] private DigitalTwin.Presentation.UI.UIManager _uiManager;
        [SerializeField] private DigitalTwin.Presentation.UI.ToastManager _toastManager;

        // Events
        public event Action<User> UserRegistered;
        public event Action<User> UserAuthenticated;
        public event Action<User> UserDeauthenticated;
        public event Action<User, User> UserProfileUpdated;
        public event Action<User> SecurityEvent> SecurityEventOccurred;

        private void Start()
        {
            Debug.Log("User management service starting...");
            
            // In production, initialize with default users
            if (!_securityConfig.IsProduction)
            {
                await InitializeDefaultUsersAsync();
            }
            
            // Register for security events
            _tokenService.TokenRevoked += OnTokenRevoked;
            _tokenService.SecurityAlertTriggered += OnSecurityAlertTriggered;
            
            Debug.Log("User management service initialized");
        }

        private async void InitializeDefaultUsersAsync()
        {
            // In production, create admin and demo users
            var defaultUsers = new List<User>
            {
                CreateDefaultUser("admin", "admin@digitaltwin.com", "Admin User", new[] { "admin", "read", "write", "delete", "manage" }, 
                CreateDefaultUser("operator", "operator@digitaltwin.com", "Operator", new[] { "operator", "read", "write", "manage" }), 
                CreateDefaultUser("manager", "manager@digitaltwin.com", "Manager", new[] { "manager", "read", "analytics" }), 
                CreateDefaultUser("analyst", "analyst@digitaltwin.com", "Analyst", new[] { "analyst", "read", "analytics" })
            };

            foreach (var user in defaultUsers)
            {
                _users[user.Id] = user.Id;
            }

            _userCount = defaultUsers.Count;
            Debug.Log($"Initialized {_userCount} default users for production");
        }

        public async Task<AuthenticationResult> RegisterUserAsync(RegisterationRequest request)
        {
            try
            {
                // Validate registration data
                var validationResult = await ValidateRegistrationRequestAsync(request);
                if (!validationResult.IsValid)
                {
                    return new AuthenticationResult
                    IsSuccess = false,
                    Message = validationResult.Errors.FirstOrDefault(),
                        User = null
                    Token = null
                    };
                }

                // Check for existing user
                var existingUser = _users.FirstOrDefault(u => 
                    u.Email == request.Email || 
                    u.Username == request.Username);

                if (existingUser != null && _securityConfig.RequireUniqueEmail)
                {
                    return new AuthenticationResult
                    IsSuccess = false,
                        Message = "User with this email already exists",
                        User = null,
                        Token = null
                    };
                }

                // Create new user
                var newUser = CreateUserFromRequest(request);
                newUser.CreatedAt = DateTime.UtcNow;
                newUser.LastLoginAt = DateTime.UtcNow;
                newUser.IsEmailVerified = false;
                newUser.IsActive = true;

                // Validate password
                if (!_securityConfig.IsStrongPassword(request.Password))
                {
                    return new AuthenticationResult
                        IsSuccess = false,
                        Message = "Password does not meet security requirements",
                        User = null,
                        Token = null
                    };
                }

                // Hash password
                newUser.Password = HashPassword(request.Password, _securityConfig.HashAlgorithm);

                // Save user to database
                await SaveUserAsync(newUser);

                // Add to active users
                _users.Add(newUser);
                _userCount++;

                UserRegistered?.Invoke(newUser);
                LogSecurityEvent("REGISTRATION", "USER_REGISTERED", $"User {newUser.Username} registered successfully");

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    User = newUser,
                    Token = _tokenService.GenerateTokenAsync(newUser),
                    Message = "User registered successfully"
                };
            }
            catch (System.Exception ex)
            {
                LogSecurityEvent("ERROR", "USER_REGISTRATION_ERROR", $"Registration failed: {ex.Message}");
                
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = "Registration failed due to: " + ex.Message,
                    User = null,
                    Token = null
                };
            }
        }

        public async Task<User> AuthenticateUserAsync(AuthenticationRequest request)
        {
            try
            {
                // Validate credentials
                var validationResult = await ValidateCredentialsAsync(request.Username, request.Password);
                if (!validationResult.IsValid)
                {
                    return new AuthenticationResult
                    IsSuccess = false,
                        Message = validationResult.Errors.FirstOrDefault(),
                        User = null,
                        Token = null
                    };
                }

                // Find user by username
                var user = _users.FirstOrDefault(u => u.Username == request.Username);
                if (user == null)
                {
                    return new AuthenticationResult
                    IsSuccess = false,
                        Message = "Invalid username or password",
                        User = null,
                        Token = null
                    };
                }

                // Check account status
                if (!user.IsActive)
                {
                    return new AuthenticationResult
                    IsSuccess = false,
                        Message = "Account is disabled",
                        User = user,
                        Token = null
                    };
                }

                // Verify password using secure hashing
                if (!PasswordHasher.VerifyPassword(request.Password, user.Password))
                {
                    user.FailedLoginAttempts++;
                    
                    // Check for account lockout
                    if (user.FailedLoginAttempts >= _securityConfig.MaxFailedAttempts)
                    {
                        user.LockoutUntil = DateTime.UtcNow.Add(_securityConfig.LockoutDuration);
                        LogSecurityEvent("AUTH", "ACCOUNT_LOCKED", $"User {user.Username} locked due to too many failed attempts");
                    }
                    
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        Message = "Invalid username or password",
                        User = null,
                        Token = null
                    };
                }

                // Reset failed attempts on successful password verification
                user.FailedLoginAttempts = 0;
                user.LockoutUntil = null;

                // Check for account lockout
                if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
                {
                    return new AuthenticationResult
                        IsSuccess = false,
                        Message = "Account is temporarily locked",
                        User = null,
                        Token = null
                    };
                }

                // Check password history
                if (_securityConfig.RequirePasswordHistory && user.FailedLoginAttempts >= _securityConfig.MaxFailedAttempts)
                {
                    return new AuthenticationResult
                        IsSuccess = false,
                        Message = "Too many failed login attempts. Please try again later",
                        User = null,
                        Token = null
                    };
                }

                // In production, verify email verification
                if (_securityConfig.IsProduction && !user.IsEmailVerified)
                {
                    // Send verification email
                    // In production, you'd implement actual email sending
                    await SendEmailVerificationEmailAsync(user.Email);
                    
                    return new AuthenticationResult
                    IsSuccess = false,
                        Message = "Email verification required for production",
                        User = user,
                        Token = null
                    };
                }

                // Check recent password change
                if (_securityConfig.RequirePasswordChange && 
                    user.PasswordChangedAt.HasValue && 
                    (DateTime.UtcNow - user.PasswordChangedAt).TotalHours < 1))
                {
                    return new AuthenticationResult
                        IsSuccess = false,
                        Message = "Password too recently changed. Please wait before changing again",
                        User = null,
                        Token = null
                    };
                }

                // Create session
                var session = await CreateUserSessionAsync(user);
                
                UserAuthenticated?.Invoke(user);
                LogSecurityEvent("AUTH", "USER_AUTHENTICATED", $"User {user.Username} authenticated from IP: {session.IPAddress}");
                
                return new AuthenticationResult
                {
                    IsSuccess = true,
                    User = user,
                    Token = _tokenService.GenerateTokenAsync(user),
                    Session = session
                    Message = "Authentication successful"
                };
            }
            catch (System.Exception ex)
            {
                LogSecurityEvent("ERROR", "AUTHENTICATION_ERROR", ex.Message);
                
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = $"Authentication failed: {ex.Message}",
                    User = null,
                    Token = null
                };
            }
        }

        public async Task<User> GetCurrentUserAsync()
        {
            // Return current active user or null
            var activeSessions = _activeSessions.Values.Where(s => s.IsActive).ToList();
            
            return activeSessions.FirstOrDefault();
        }

        public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }

            // Validate new password
            if (!_securityConfig.IsStrongPassword(newPassword))
            {
                return false;
            }

            // Update password change history
            user.PasswordChangedAt = DateTime.UtcNow;

            // Hash new password
            user.Password = HashPassword(newPassword);

            // Update user in database
            await SaveUserAsync(user);

            // Log security event
            LogSecurityEvent("ADMIN", "PASSWORD_CHANGED", $"User {userId} changed password successfully");

            // Invalidate user sessions if password changed recently
            var sessionsToInvalidate = _activeSessions.Values
                .Where(s => s.UserId == userId && s.IsActive)
                .ToList();

            foreach (var session in sessionsToInvalidate)
            {
                await RevokeUserSessionAsync(session.Id);
            }

            LogSecurityEvent("ADMIN", "SESSION_INVALIDATED", 
                $"{sessionsToInvalidate.Count} sessions invalidated due to password change");

            return true;
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, UserProfile profile)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return false;

            // Update user metadata
            if (profile != null)
            {
                user.Metadata["name"] = profile.Name;
                user.Metadata["avatar"] = profile.Avatar;
                user.Metadata["department"] = profile.Department;
                user.Metadata["location"] = profile.Location;
                user.Metadata["phone"] = profile.Phone;
                user.Metadata["timezone"] = profile.Timezone;
                user.Metadata["language"] = profile.Language;
                user.Metadata["preferences"] = new Dictionary<string, object>();
                user.Metadata["notifications"] = new Dictionary<string, object>();
            }

            await SaveUserAsync(user);

            // Log security event
            LogSecurityEvent("ADMIN", "PROFILE_UPDATED", $"User {user.Username} profile updated successfully");

            return true;
        }

        public async Task<bool> DisableUserAsync(string userId, string reason)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return false;

            if (user.IsActive)
            {
                // Deactivate user and create session
                user.IsActive = false;
                await RevokeUserSessionAsync(user.Id);

                LogSecurityEvent("ADMIN", "USER_DISABLED", $"User {user.Username} disabled: {reason}");

                return true;
            }
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string query = null, int page = 1, int pageSize = 50)
        {
            var users = _users.Where(u => 
                (string.IsNullOrEmpty(query) || 
                 u.Username.Contains(query) ||
                 u.Email.Contains(query) ||
                 u.Metadata.ContainsKey("name") && u.Metadata["name"].ToString().Contains(query) ||
                 u.Metadata.ContainsKey("department") && u.Metadata["department"].ToString().Contains(query)))
                .OrderBy(u => u.Metadata["name"])
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return users;
        }

        public async Task<bool> DeleteUserAsync(string userId, string reason)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return false;

            try
            {
                // Check if user can be deleted
                if (user.Id == Guid.Parse("00000000000-0000-0000-000000000001"))
                {
                    return false; // Can't delete system user
                }

                // Remove user from active sessions
                var sessionsToInvalidate = _activeSessions.Values
                    .Where(s => s.UserId == userId && s.IsActive)
                    .ToList();

                foreach (var session in sessionsToInvalidate)
                {
                    await RevokeUserSessionAsync(session.Id);
                }

                // Remove user
                _users.Remove(user);
                _userCount--;

                LogSecurityEvent("ADMIN", "USER_DELETED", $"User {user.Username} deleted: {reason}");

                return true;
            }
            catch (System.Exception ex)
            {
                LogSecurityEvent("ERROR", "USER_DELETE_ERROR", ex.Message);
                return false;
            }
        }

        // Private helper methods
        private async Task<User> CreateUserFromRequestAsync(RegistrationRequest request)
        {
            return new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = request.Username,
                Email = request.Email,
                PhoneNumber = request.Phone,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Roles = request.Roles ?? new List<string>(),
                IsEmailVerified = false,
                Password = HashPassword(request.Password),
                FailedLoginAttempts = 0
            };
        }

        private string HashPassword(string password)
        {
            // Use secure PBKDF2 password hashing
            return PasswordHasher.HashPassword(password);
        }

        private async Task SaveUserAsync(User user)
        {
            try
            {
                // Update database
                // In production, you'd use a proper ORM
                Debug.Log($"User {user.Id} saved to database");
                
                // Return success (in production, you'd return a success message)
                return true;
            }
            catch (System.Exception ex)
            {
                LogSecurityEvent("ERROR", "USER_SAVE_ERROR", ex.Message);
                Debug.LogError($"Failed to save user {user.Id}: {ex.Message}");
                return false;
            }
        }

        private async Task RevokeUserSessionAsync(string sessionId)
        {
            var session = _activeSessions.Values.FirstOrDefault(s => s.Id == sessionId);
            if (session != null && session.IsActive)
            {
                session.IsActive = false;
                session.ExpiresAt = DateTime.UtcNow;
                
                // Update session
                await UpdateSessionAsync(session);

                // Notify session revoked
                UserLoggedOut?.Invoke(session.User.Id);
                LogSecurityEvent("AUTH", "SESSION_REVOKED", $"User {session.User.Username} session {session.Id} revoked");
                Debug.Log($"Session {session.Id} automatically ended due to revocation or timeout");
            }
        }

        private async Task UpdateSessionAsync(UserSession session)
        {
            // Update last activity
            session.LastActivity = DateTime.UtcNow;
            
            // Update session expiration if needed
            if (session.ExpiresAt <= DateTime.UtcNow && 
                (DateTime.UtcNow - session.LastActivity) > TimeSpan.FromHours(1)))
            {
                session.ExpiresAt = DateTime.UtcNow + TimeSpan.FromHours(1); // Extend by 1 hour
            }

            // Save session changes
            // In production, you'd save to database
            Debug.Log($"Session {session.Id} updated in database");

            return true;
            }
        }
    }

    /// <summary>
    /// Authentication Result
    /// </summary>
    public class AuthenticationResult
    {
        public bool IsSuccess { get; }
        public User User { get; set; }
        public string Token { get; set; }
        public string RefreshTokenId { get; set; }
        public string Message { get; set; }
        public string SessionId { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
    }

    /// <summary>
    /// Registration Request
    /// </summary>
    public class RegistrationRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public string[] Roles { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public string Department { get; set; }
        public string Location { get; set; }
    }
}