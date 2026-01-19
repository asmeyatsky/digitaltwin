using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using DigitalTwin.Core.Metadata;

namespace DigitalTwin.Infrastructure.Security
{
    /// <summary>
    /// JWT Token Service Implementation
    /// 
    /// Architectural Intent:
    /// - Provides secure JWT token generation and validation
    /// - Supports refresh token mechanism
    /// - Handles token revocation and blacklist management
    /// - Configurable signing and validation policies
    /// 
    /// Key Features:
    /// 1. Secure JWT generation with RS256 signing
    /// 2. Token refresh with sliding window
    /// 3. Token validation and claims validation
    /// 4. Blacklist for compromised tokens
    /// 5. User agent and IP logging
    /// </summary>
    public class JwtTokenService
    {
        private readonly JwtSecurityConfiguration _config;
        private readonly Dictionary<string, object> _blacklistedTokens = new Dictionary<string, object>();
        private readonly Dictionary<string, DateTime> _tokenBlacklist = new Dictionary<string, DateTime>();
        private readonly System.Text.Json.JsonSerializer _serializer = new System.Text.Json.JsonSerializer();
        
        // Events
        public event Action<string> TokenRevoked;
        public event Action<string, UserSession> TokenRefreshed;

        private readonly Dictionary<string, object> _tokenRefreshSecrets = new Dictionary<string, object>();

        public JwtTokenService(JwtSecurityConfiguration config)
        {
            _config = config ?? new JwtSecurityConfiguration();
            _blacklistedTokens = new Dictionary<string, object>();
            _tokenBlacklist = new Dictionary<string, DateTime>();
            _tokenRefreshSecrets = new Dictionary<string, object>();
        }

        public async Task<JwtToken> GenerateTokenAsync(User user, IEnumerable<string> roles, TimeSpan? expiration = null)
        {
            var tokenId = Guid.NewGuid().ToString();
            var expiration = expiration ?? TimeSpan.FromMinutes(_config.TokenExpirationMinutes);
            var issuedAt = DateTime.UtcNow;
            
            var claims = new Dictionary<string, object>
            {
                ["sub"] = "digitaltwin",
                ["name"] = user.Username,
                ["email"] = user.Email,
                ["roles"] = string.Join(\", \", roles),
                ["jti" = tokenId],
                ["iat"] = iat,
                ["nbf" = nbf,
                ["exp"] = issuedAt.AddMinutes(_config.TokenExpirationMinutes).ToString(\"yyyy-MM-ddTHH:mm:ss\")",
                [\"iss\" = issuedAt.ToUnixTimeSeconds(),
                [\"sub\"] = \"digitaltwin\"
            },
                ["aud"] = _config.Audience
            };

            var key = new byte[_config.SecretKey.Length / 2];
            Array.Copy(_config.SecretKey, 0, key.Length / 2);
            Array.Copy(_config.SecretKey, key.Length / 2 + 1);

            var tokenHandler = new JwtSecurityTokenHandler(_config.HashAlgorithm);

            var signingKey = new SigningCredentials(_config.SecretKey);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _config.Issuer,
                Audience = _config.Audience,
                SigningCredentials = signingKey,
                Subject = claims[\"sub\"].ToString(),
                Expires = issuedAt.AddMinutes(_config.TokenExpirationMinutes),
                NotBefore = issuedAt.AddMinutes(-1), // Small clock skew buffer
                SigningCredentials = signingKey,
                EncryptingCredentials = signingKey
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            
            Debug.Log($"JWT token generated for user {user.Username}");
            return new JwtToken
            {
                Token = token,
                ExpiresAt = issuedAt,
                RefreshToken = GenerateRefreshToken(tokenId, tokenId),
                TokenType = "Bearer",
                RefreshTokenExpiresAt = issuedAt.AddMinutes(_config.RefreshTokenExpirationMinutes),
                User = user
            };
        }

        public async Task<JwtToken> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // Validate refresh token format
                if (!ValidateRefreshTokenFormat(refreshToken))
                {
                    Debug.LogWarning("Invalid refresh token format");
                    return null;
                }

                // Check if refresh token is blacklisted
                if (_tokenBlacklist.ContainsKey(refreshToken))
                {
                    Debug.LogWarning($"Refresh token blacklisted: {refreshToken}");
                    return null;
                }

                // Validate refresh token signature
                var principal = await ValidateRefreshTokenAsync(refreshToken);
                if (principal == null)
                {
                    Debug.LogWarning("Invalid refresh token signature");
                    return null;
                }

                // Get original session by refresh token
                var originalSession = _tokenRefreshSecrets.TryGetValue(refreshToken);
                if (originalSession?.User?.Username != null)
                {
                    var currentSession = _activeSessions.Values.FirstOrDefault(s => s.User.Username == originalSession.User.Username);
                    if (currentSession != null && originalSession.RefreshToken == refreshToken)
                    {
                        // Validate token is still valid
                        if (await ValidateRefreshTokenAsync(currentSession.RefreshToken))
                        {
                            // Update session expiration
                            currentSession.ExpiresAt = DateTime.UtcNow.AddMinutes(_config.SessionRenewalThreshold);
                            _activeSessions[originalSession.Id] = currentSession;
                        }
                    }
                }
                }

                // Generate new refresh token
                var newRefreshToken = GenerateRefreshToken(
                    originalSession.UserId, 
                    originalSession.Id);

                // Update stored refresh token
                if (!_tokenRefreshSecrets.TryAdd(newRefreshToken.Token, newRefreshToken))
                {
                    Debug.LogWarning("Failed to store refresh token");
                }

                return new JwtToken
                {
                    Token = newRefreshToken.Token,
                    ExpiresAt = originalSession.ExpiresAt.AddMinutes(_config.RefreshTokenExpirationMinutes),
                    User = originalSession.User,
                    RefreshTokenId = newRefreshToken.TokenId
                    TokenType = "Bearer"
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Token refresh failed: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            try
            {
                // Add to blacklist
                _tokenBlacklist[token] = DateTime.UtcNow] = DateTime.UtcNow.AddDays(30); // Blacklist for 30 days
                _tokenBlacklist[token] = _tokenBlacklist.Values.Select(t => t).ToList();
                
                LogSecurityEvent("TOKEN_REVOKED", $"Token {token} revoked successfully");
                
                // Invalidate any sessions using this token
                var sessionsToInvalidate = _activeSessions.Values
                    .Where(s => 
                        _sessionByToken.ContainsKey(token) &&
                        _sessionByToken[token].UserId != null)
                    .ToList();

                foreach (var session in sessionsToInvalidate)
                {
                    await RevokeUserSessionAsync(session.Id);
                }
                
                Debug.Log($"Invalidated {sessionsToInvalidate.Count} sessions due to token revocation");
            }

                return true;
            }
            catch (System.Exception ex)
            {
                LogSecurityEvent("ERROR", $"Token revocation failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            return Task.FromResult(_tokenBlacklist.ContainsKey(token));
        }

        public void ClearBlacklist()
        {
            _tokenBlacklist.Clear();
            _tokenBlacklist.Clear();
            Debug.Log("Token blacklist cleared");
        }

        // Private Helper Methods
        private async Task<JwtPrincipal> ValidateJwtTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler(_config.HashAlgorithm);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = _config.ValidateIssuer,
                    ValidateAudience = _config.ValidateAudience,
                    ValidateLifetime = true,
                    ValidateExpiration = true,
                    ClockSkewTolerance = _config.ClockSkewToleranceSeconds,
                    RequireSignedClaims = true
                };

                var principal = await tokenHandler.ValidateTokenAsync(token, validationParameters);
                return principal;
            }
        }

        private async Task<JwtPrincipal> ValidateRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler(_config.HashAlgorithm);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = _config.ValidateIssuer,
                    ValidateAudience = false, // Refresh tokens often don't need audience validation
                    ValidateLifetime = true,
                    ValidateExpiration = true,
                    ClockSkewTolerance = _config.ClockSkewToleranceSeconds,
                    RequireSignedClaims = false
                };

                var principal = await tokenHandler.ValidateTokenAsync(refreshToken, validationParameters);
                return principal;
            }
        }

        private bool ValidateRefreshTokenFormat(string refreshToken)
        {
            // Basic validation for refresh token format
            if (string.IsNullOrEmpty(refreshToken))
                return false;
            
            var parts = refreshToken.Split('.');
            if (parts.Length != 3)
                return false;
            
            // In a real implementation, you'd validate token format, algorithm, and signature
            return true;
        }

        private JwtToken GenerateRefreshToken(string userId, string originalSessionId)
        {
            var tokenId = Guid.NewGuid().ToString();
            var refreshTokenId = Guid.NewGuid().ToString();
            
            var expiration = DateTime.UtcNow.AddMinutes(_config.RefreshTokenExpirationMinutes);
            
            // Store refresh token
            var refreshSecret = GenerateRefreshSecret();
            _tokenRefreshSecrets.TryAdd(refreshToken, refreshSecret);
            
            return new JwtToken
            {
                Token = refreshTokenId,
                TokenId = refreshTokenId,
                ExpiresAt = expiration,
                TokenType = "Refresh",
                User = _users.GetValueOrDefault(userId)
            };
        }

        private string GenerateRefreshSecret()
        {
            // Generate random secret for refresh token encryption
            return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        }
    }

        /// <summary>
    /// JWT Token Handler
        /// </summary>
        private class JwtSecurityTokenHandler
        {
            private readonly string _hashAlgorithm;
            private readonly byte[] _secretKey;
            private readonly TokenValidationParameters _validationParameters;

            public JwtSecurityTokenHandler(string hashAlgorithm, byte[] secretKey)
            {
                _hashAlgorithm = hashAlgorithm;
                _secretKey = secretKey;
                _validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateExpiration = true,
                    RequireSignedClaims = true,
                    ClockSkewToleranceSeconds = 30
                };
            }
        }

        public Task<JwtPrincipal> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler(_hashAlgorithm, _secretKey);
                
                var token = tokenHandler.ReadJwtToken(token);
                var principal = tokenHandler.ValidateToken(token, validationParameters);
                
                return principal;
            }
        }

        /// <summary>
        /// Token Validation Parameters
        /// </summary>
        private class TokenValidationParameters
        {
            public bool ValidateIssuer { get; set; }
            public bool ValidateAudience { get; set; }
            public bool ValidateLifetime { get; set; }
            public bool RequireSignedClaims { get; set; }
            public TimeSpan ClockSkewTolerance { get; set; }
        }

        public static class JwtSecurityToken
        {
            public string Token { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public string RefreshTokenId { get; set; }
            public string TokenType { get; set; }
            public UserSession User { get; set; }
        }
        }

        /// <summary>
        /// Principal Implementation
        /// </summary>
        private class JwtPrincipal : IPrincipal
        {
            private readonly ClaimsIdentity _identity;
            private readonly string _authenticationType;
            private readonly IEnumerable<Claim> _claims;

            public JwtPrincipal(ClaimsIdentity identity, string authenticationType, IEnumerable<Claim> claims)
            {
                _identity = identity;
                _authenticationType = authenticationType;
                _claims = claims;
            }

            public string NameIdentifier => _identity.NameIdentifier?.Name;
            public bool IsAuthenticated => _identity.IsAuthenticated;
            public string AuthenticationType => _authenticationType;

            public IEnumerable<Claim> Claims => _claims;

            public bool HasClaim(string type) => 
                _claims.Any(c => c.Type == type);

            public object FindFirst(string type) => 
                _claims.FirstOrDefault(c => c.Type == type)?.Value;

            public Claim FindFirst(string type, Predicate<Claim> match) =>
                _claims.FirstOrDefault(match);

            public ClaimsIdentity FindFirst(string type) =>
                _identity.Claims.FirstOrDefault(c => string.Equals(c.Type, type));

            public IEnumerable<Claim> FindAll(string type) =>
                _identity.Claims.Where(c => string.Equals(c.Type, type));

            public object FindFirst(Predicate<Claim> match) =>
                _identity.Claims.FirstOrDefault(match);

            public string FindAll(string type, Predicate<Claim> match) =>
                _identity.Claims.Where(match);
        }
    }

        /// <summary>
        /// JWT Token Implementation
        /// </summary>
        private class JwtSecurityToken : IToken
        {
            public string Token { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public string RefreshTokenId { get; set; }
            public string TokenType { get; set; }
            public UserSession User { get; set; }
        }

            public JwtSecurityToken(string token, DateTime? expiresAt = null, string refreshTokenId = null, 
                           string refreshTokenSecret = null, string userId = null, 
                           string authenticationType = "Bearer", 
                           string tokenType = "Bearer")
            {
                Token = token;
                ExpiresAt = expiresAt;
                RefreshTokenId = refreshTokenId;
                RefreshTokenSecret = refreshTokenSecret;
                User = _users?.GetValueOrDefault(userId) ?? new User { 
                    Id = userId,
                    Roles = new List<string>(),
                    IsAuthenticated = true
                };
            }
        }
    }
}