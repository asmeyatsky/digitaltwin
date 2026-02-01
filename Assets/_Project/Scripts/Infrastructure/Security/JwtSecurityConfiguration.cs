using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalTwin.Infrastructure.Security
{
    /// <summary>
    /// JWT Security Configuration
    /// 
    /// Architectural Intent:
    /// - Configures JWT settings for authentication
    /// - Provides secure default configurations
    /// - Supports both development and production environments
    /// - Enables easy configuration management
    /// 
    /// Key Settings:
    /// 1. Token lifecycle management
    /// 2. Secret key management
    /// 3. Encryption and signing
    /// 4. Token validation policies
    /// 5. Session security settings
    /// </summary>
    [System.Serializable]
    public class JwtSecurityConfiguration
    {
        [Header("JWT Settings")]
        [SerializeField] public string SecretKey = ""; // Must be set from environment variable
        [SerializeField] public string Issuer = "DigitalTwin";
        [SerializeField] public string Audience = "DigitalTwin-API";
        [SerializeField] public string HashAlgorithm = "HS256";
        [SerializeField] public int TokenExpirationMinutes = 60;
        [SerializeField] public int RefreshTokenExpirationMinutes = 5;
        [SerializeField] public int ClockSkewToleranceSeconds = 30;
        [SerializeField] public bool RequireHttps = true;
        [SerializeField] public bool ValidateAudience = true;
        [SerializeField] public bool ValidateIssuer = true;
        public bool RequireHttps => _requireHttps;
        [SerializeField] private bool _requireHttps = true;

        [Header("Session Settings")]
        [SerializeField] public int MaxConcurrentSessions = 100;
        [SerializeField] public TimeSpan AbsoluteSessionTimeout = TimeSpan.FromHours(8);
        [SerializeField] public TimeSpan SessionRenewalThreshold = TimeSpan.FromMinutes(30);
        [SerializeField] public TimeSpan IdleTimeout = TimeSpan.FromMinutes(15);

        [Header("Security Policies")]
        [SerializeField] public bool RequireStrongPasswords = true;
        [SerializeField] public int MinPasswordLength = 8;
        [SerializeField] public int MaxFailedAttempts = 5;
        [SerializeField] public TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
        [SerializeField] public bool RequirePasswordHistory = true;
        [SerializeField] public bool RequireMFA = false;
        [SerializeField] public bool RequirePasswordChange = true;
        [SerializeField] public TimeSpan PasswordExpirationDays = 90;
        [SerializeField] public int PasswordHistoryCount = 5;
        [SerializeField] public string AllowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%&*()_+-=[];./~";

        [Header("Rate Limiting")]
        [SerializeField] public int MaxLoginAttemptsPerMinute = 5;
        [SerializeField] public int MaxTokensPerUserPerHour = 100;
        [SerializeField] public int ConcurrentTokensPerUser = 3;
        [SerializeField] public int TokensPerMinute = 1000;

        [Header("API Integration")]
        [SerializeField] public bool EnableBMSIntegration = false;
        [SerializeField] public string BMSApiKey = "";
        [SerializeField] public string SCADAComplianceLevel = "Level 2";

        // Validation helpers
        public bool IsProduction => Application.platform == RuntimePlatform.WindowsPlayer || 
                               Application.platform == RuntimePlatform.LinuxPlayer ||
                               Application.platform == RuntimePlatform.OSXPlayer ||
                               (Application.isEditor == false && Debug.isDebugBuild == false);
        
        public bool RequireHttps => _requireHttps;
        
        // Validate secret key at runtime
        public bool ValidateSecretKey()
        {
            if (string.IsNullOrEmpty(SecretKey))
            {
                Debug.LogError("JWT SecretKey is not configured. Set JWT_SECRET_KEY environment variable.");
                return false;
            }
            
            if (SecretKey.Length < 32)
            {
                Debug.LogError("JWT SecretKey must be at least 32 characters long for HS256.");
                return false;
            }
            
            return true;
        }
        
        // Password strength checking - improved implementation
        public bool IsStrongPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            
            // Require minimum length
            if (password.Length < MinPasswordLength) return false;
            
            var hasUpper = password.Any(char.IsUpper);
            var hasLower = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));
            
            // Enhanced scoring system
            var score = 0;
            if (password.Length >= 12) score += 2;
            else if (password.Length >= 8) score += 1;
            if (hasUpper) score += 1;
            if (hasLower) score += 1;
            if (hasDigit) score += 1;
            if (hasSpecial) score += 2;
            
            // Additional checks
            if (!password.Contains(" ")) score += 1; // No spaces
            if (password.Distinct().Count() >= password.Length / 2) score += 1; // Character diversity
            
            return score >= 4; // Higher strength requirement
        }
    }
}