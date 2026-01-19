using System;
using System.Collections.Generic;
using DigitalTwin.Core.Metadata;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Security.Models
{
    /// <summary>
    /// User Model
    /// 
    /// Architectural Intent:
    /// - Represents user account in the digital twin system
    /// - Supports multiple authentication methods
    /// - Maintains security audit trail
    /// - Enables role-based permissions
    /// 
    /// Key Features:
    /// 1. Multi-factor authentication support
    /// 2. Password policies and history
    /// 3. Session management across devices
    /// 4. Security event logging and audit trail
    /// </summary>
    [System.Serializable]
    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public List<string> Roles { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? PasswordChangedAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutUntil { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public User()
        {
            Roles = new List<string>();
            Metadata = new Dictionary<string, object>();
            IsActive = true;
            IsEmailVerified = false;
            IsPhoneVerified = false;
            CreatedAt = DateTime.UtcNow;
            FailedLoginAttempts = 0;
            LastLoginAt = DateTime.UtcNow;
            LockoutUntil = null;
        }
    }

    /// <summary>
    /// User Session Model
    /// 
    /// Architectural Intent:
    /// - Represents user session information
    /// - Supports multiple concurrent sessions
    /// - Tracks activity and security events
    /// - Handles device fingerprinting
    /// 
    /// Key Features:
    /// 1. Session timeout management
    /// 2. Device tracking
    /// 3. Activity logging
    /// 4. Geographic location tracking
    /// </summary>
    [System.Serializable]
    public class UserSession
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public List<string> Roles { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime LastActivity { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string RefreshTokenId { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public List<SecurityEvent> SecurityEvents { get; set; }

        public UserSession()
        {
            Id = Guid.NewGuid().ToString();
            UserId = string.Empty;
            Username = string.Empty;
            Roles = new List<string>();
            IPAddress = "127.0.0.1";
            UserAgent = "Unknown";
            LoginTime = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow.AddHours(8);
            IsActive = true;
            RefreshTokenId = Guid.NewGuid().ToString();
            SecurityEvents = new List<SecurityEvent>();
            Metadata = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Security Event Model
    /// 
    /// Architectural Intent:
    /// - Records security-related events for audit trail
    /// - Enables comprehensive logging and alerting
    /// - Supports filtering and analysis
    /// 
    /// Key Features:
    /// 1. Multiple event types (Auth, Security, Admin, System)
    /// 2. Detailed event context
    /// 3. User tracking correlation
    /// 4. IP and device fingerprinting
    /// 5. Automated alert generation
    /// </summary>
    [System.Serializable]
    public class SecurityEvent
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string IPAddress { get; set; }
        public string Resource { get; set; }
        public string Action { get; set; }
        public object Details { get; set; }
        public string Risk { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Context { get; set; }
    }

        public SecurityEvent()
        {
            Id = Guid.NewGuid().ToString();
            Type = "INFO";
            UserId = string.Empty;
            Username = string.Empty;
            IPAddress = "127.0.0.1";
            Resource = string.Empty;
            Action = string.Empty;
            Details = new Dictionary<string, object>();
            Risk = "Low";
            Timestamp = DateTime.UtcNow;
            Context = new Dictionary<string, object>();
        }
    }
}