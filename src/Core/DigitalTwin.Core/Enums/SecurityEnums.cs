using System;

namespace DigitalTwin.Core.Enums
{
    /// <summary>
    /// User roles for role-based access control
    /// </summary>
    public enum UserRole
    {
        Guest = 0,
        Viewer = 1,
        Operator = 2,
        Manager = 3,
        Admin = 4,
        SuperAdmin = 5
    }

    /// <summary>
    /// Permissions for granular access control
    /// </summary>
    public enum Permission
    {
        // Dashboard permissions
        ViewDashboard = 1,
        
        // Building permissions
        ViewBuildings = 2,
        ManageBuildings = 3,
        
        // Sensor permissions
        ViewSensors = 4,
        ManageSensors = 5,
        
        // Analytics permissions
        ViewAnalytics = 6,
        ExportData = 7,
        
        // Security permissions
        ViewAuditLogs = 8,
        ManageUsers = 9,
        ManageRoles = 10,
        
        // System permissions
        ViewSystemLogs = 11,
        ManageSystemSettings = 12,
        
        // API permissions
        AccessAPI = 13,
        ManageAPIKeys = 14,
        
        // Alert permissions
        ViewAlerts = 15,
        ManageAlerts = 16,
        
        // Report permissions
        ViewReports = 17,
        GenerateReports = 18,
        
        // Maintenance permissions
        ViewMaintenance = 19,
        ManageMaintenance = 20
    }

    /// <summary>
    /// Security event types for audit logging
    /// </summary>
    public enum SecurityEventType
    {
        // Authentication events
        UserLoggedIn = 1,
        UserLoggedOut = 2,
        UserRegistered = 3,
        PasswordChanged = 4,
        PasswordResetRequested = 5,
        PasswordResetCompleted = 6,
        
        // Token events
        TokenIssued = 7,
        TokenValidated = 8,
        TokenExpired = 9,
        TokenRevoked = 10,
        InvalidToken = 11,
        
        // Account events
        AccountLocked = 12,
        AccountUnlocked = 13,
        AccountCreated = 14,
        AccountUpdated = 15,
        AccountDeleted = 16,
        AccountDisabled = 17,
        AccountEnabled = 18,
        
        // Authorization events
        AccessGranted = 19,
        AccessDenied = 20,
        PermissionGranted = 21,
        PermissionRevoked = 22,
        RoleAssigned = 23,
        RoleRemoved = 24,
        
        // Security events
        BruteForceAttempt = 25,
        SuspiciousActivity = 26,
        RateLimitExceeded = 27,
        DataAccessAttempt = 28,
        
        // Data events
        DataExported = 29,
        DataImported = 30,
        DataModified = 31,
        DataDeleted = 32,
        
        // System events
        SystemConfigurationChanged = 33,
        SecurityPolicyUpdated = 34,
        BackupInitiated = 35,
        RestoreInitiated = 36
    }
}