using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace DigitalTwin.API.Services
{
    public class JwtAuthenticationService
    {
        private readonly JwtSigningCredentials _jwtConfig;
        private readonly ILogger<JwtAuthenticationService> _logger;

        public JwtAuthenticationService(
            JwtSigningCredentials jwtConfig,
            ILogger<JwtAuthenticationService> logger)
        {
            _jwtConfig = jwtConfig;
            _logger = logger;
        }

        public string GenerateToken(string userId, string username, IList<string> roles)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(
                        double.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS") ?? "24")),
                    Issuer = _jwtConfig.Issuer,
                    Audience = _jwtConfig.Audience,
                    SigningCredentials = _jwtConfig.Credentials
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

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _jwtConfig.Credentials.Key,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtConfig.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtConfig.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed");
                return null;
            }
        }

        public UserInfo? ExtractUserInfoFromToken(string token)
        {
            try
            {
                var principal = ValidateToken(token);
                if (principal?.Identity is not ClaimsIdentity identity)
                    return null;

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

        public bool IsTokenExpired(string token)
        {
            try
            {
                var userInfo = ExtractUserInfoFromToken(token);
                if (userInfo?.Expiration == null) return true;
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(userInfo.Expiration));
                return expirationTime < DateTimeOffset.UtcNow;
            }
            catch
            {
                return true;
            }
        }
    }

    public class UserInfo
    {
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public List<string> Roles { get; set; } = new();
        public string? Expiration { get; set; }
    }
}
