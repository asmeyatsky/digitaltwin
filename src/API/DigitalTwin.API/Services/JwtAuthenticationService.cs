using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace DigitalTwin.API.Services
{
    /// <summary>
    /// JWT Authentication Service
    /// 
    /// Architectural Intent:
    /// - Generates JWT tokens for authenticated users
    /// - Validates tokens for API requests
    /// - Manages token refresh mechanism
    /// - Supports secure authentication flows
    /// 
    /// Key Features:
    /// 1. Token generation with claims
    /// 2. Token validation and verification
    /// 3. Refresh token support
    /// 4. Secure key management
    /// 5. Token expiration handling
    /// </summary>
    public class JwtAuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtAuthenticationService> _logger;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;

        public JwtAuthenticationService(
            IConfiguration configuration,
            ILogger<JwtAuthenticationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            _secretKey = Environment.GetEnvironmentVariable("JWT_KEY") 
                ?? configuration["Jwt:Key"] 
                ?? "ThisIsASecretKeyForDevelopmentUseOnly123456789012345678901234567890";
            
            _issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                ?? configuration["Jwt:Issuer"] 
                ?? "DigitalTwinAPI";
            
            _audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                ?? configuration["Jwt:Audience"] 
                ?? "DigitalTwinClients";
        }

        /// <summary>
        /// Generate JWT token for authenticated user
        /// </summary>
        public string GenerateToken(string userId, string username, IList<string> roles)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, string.Join(",", roles)),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };

                // Add role claims individually
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(
                        double.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS") ?? "24")),
                    Issuer = _issuer,
                    Audience = _audience,
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserId}", userId);
                throw new ApplicationException("Failed to generate authentication token", ex);
            }
        }

        /// <summary>
        /// Generate refresh token for user
        /// </summary>
        public string GenerateRefreshToken()
        {
            try
            {
                var randomNumber = new byte[32];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token");
                throw new ApplicationException("Failed to generate refresh token", ex);
            }
        }

        /// <summary>
        /// Validate JWT token and return claims principal
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token validation failed: Token expired");
                return null;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                _logger.LogWarning("Token validation failed: Invalid signature");
                return null;
            }
            catch (SecurityTokenInvalidIssuerException)
            {
                _logger.LogWarning("Token validation failed: Invalid issuer");
                return null;
            }
            catch (SecurityTokenInvalidAudienceException)
            {
                _logger.LogWarning("Token validation failed: Invalid audience");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed: Unknown error");
                return null;
            }
        }

        /// <summary>
        /// Extract user information from token
        /// </summary>
        public UserInfo? ExtractUserInfoFromToken(string token)
        {
            try
            {
                var principal = ValidateToken(token);
                if (principal == null)
                {
                    return null;
                }

                var identity = principal.Identity as ClaimsIdentity;
                if (identity == null)
                {
                    return null;
                }

                return new UserInfo
                {
                    UserId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    Username = identity.FindFirst(ClaimTypes.Name)?.Value,
                    Roles = identity.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList() ?? new List<string>(),
                    Expiration = identity.FindFirst(JwtRegisteredClaimNames.Exp)?.Value
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user info from token");
                return null;
            }
        }

        /// <summary>
        /// Check if token is expired
        /// </summary>
        public bool IsTokenExpired(string token)
        {
            try
            {
                var userInfo = ExtractUserInfoFromToken(token);
                if (userInfo?.Expiration == null)
                {
                    return true; // Can't validate expiration, assume expired
                }

                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(userInfo.Expiration));
                return expirationTime < DateTimeOffset.UtcNow;
            }
            catch
            {
                return true; // If we can't parse, assume expired
            }
        }
    }

    /// <summary>
    /// User information extracted from JWT token
    /// </summary>
    public class UserInfo
    {
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public List<string> Roles { get; set; } = new();
        public string? Expiration { get; set; }
    }
}