# Security System Documentation

## Overview

The Digital Twin platform includes a comprehensive security system designed for enterprise-grade deployment. This document provides an overview of the security features, setup instructions, and best practices.

## 🔐 Security Features

### Authentication
- **JWT-based Authentication**: Secure token-based authentication with configurable expiration
- **Password Policies**: Enforce strong password requirements
- **Account Lockout**: Automatic account lockout after failed attempts
- **Token Refresh**: Secure token refresh mechanism
- **Session Management**: Track and manage user sessions

### Authorization
- **Role-Based Access Control (RBAC)**: Granular permission system
- **Custom Roles**: Create and manage custom roles with specific permissions
- **Permission-Based Authorization**: Fine-grained access control at the API level
- **User-Specific Permissions**: Grant or revoke permissions for individual users

### Security Middleware
- **JWT Authentication Middleware**: Automatic token validation
- **Rate Limiting**: Prevent brute force attacks and API abuse
- **Security Headers**: Add security headers to all responses
- **CORS Configuration**: Cross-origin resource sharing controls

### Audit & Monitoring
- **Security Event Logging**: Comprehensive audit trail
- **Real-time Monitoring**: Track security events in real-time
- **Log Export**: Export audit logs for compliance
- **Security Dashboard**: Visual overview of security metrics

## 🚀 Quick Setup

### 1. Configuration

Add the following to your `appsettings.json`:

```json
{
  "JwtConfiguration": {
    "SecretKey": "YourVeryLongSecretKeyForJWTTokenGeneration123456789",
    "Issuer": "DigitalTwin",
    "Audience": "DigitalTwin",
    "ExpiryInMinutes": 60,
    "RefreshTokenExpiryInDays": 7
  },
  "PasswordConfiguration": {
    "MinLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigits": true,
    "RequireSpecialChars": true,
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 15
  },
  "SecurityConfiguration": {
    "EnableTwoFactorAuth": false,
    "EnableSessionTimeout": true,
    "SessionTimeoutMinutes": 30,
    "EnableRateLimiting": true,
    "RateLimitRequestsPerMinute": 100
  }
}
```

### 2. Service Registration

In your `Startup.cs` or `Program.cs`:

```csharp
// Register security services
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<RoleBasedAccessControlService>();
builder.Services.AddScoped<SecurityEventLogger>();

// Configure JWT
builder.Services.Configure<JwtConfiguration>(
    builder.Configuration.GetSection("JwtConfiguration"));
builder.Services.Configure<PasswordConfiguration>(
    builder.Configuration.GetSection("PasswordConfiguration"));

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtConfig = builder.Configuration.GetSection("JwtConfiguration").Get<JwtConfiguration>();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtConfig.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtConfig.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => 
        policy.RequireRole("SuperAdmin", "Admin"));
    options.AddPolicy("ManageUsers", policy => 
        policy.RequireClaim("permission", "ManageUsers"));
});
```

### 3. Middleware Configuration

Add the security middleware to your pipeline:

```csharp
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<JwtAuthenticationMiddleware>();
```

## 👥 User Roles & Permissions

### Default Roles

| Role | Description | Key Permissions |
|------|-------------|------------------|
| **SuperAdmin** | Full system access | All permissions |
| **Admin** | System administration | Manage users, buildings, sensors |
| **Manager** | Building management | View/manage buildings, export data |
| **Operator** | Daily operations | View buildings, manage sensors |
| **Viewer** | Read-only access | View dashboard, buildings, sensors |
| **Guest** | Limited access | View dashboard only |

### Permission Categories

- **Dashboard**: `ViewDashboard`
- **Buildings**: `ViewBuildings`, `ManageBuildings`
- **Sensors**: `ViewSensors`, `ManageSensors`
- **Analytics**: `ViewAnalytics`, `ExportData`
- **Security**: `ViewAuditLogs`, `ManageUsers`, `ManageRoles`
- **System**: `ViewSystemLogs`, `ManageSystemSettings`
- **API**: `AccessAPI`, `ManageAPIKeys`
- **Alerts**: `ViewAlerts`, `ManageAlerts`
- **Reports**: `ViewReports`, `GenerateReports`
- **Maintenance**: `ViewMaintenance`, `ManageMaintenance`

## 🔧 API Endpoints

### Authentication
- `POST /api/security/register` - User registration
- `POST /api/security/login` - User login
- `POST /api/security/refresh` - Token refresh
- `POST /api/security/logout` - User logout
- `POST /api/security/change-password` - Change password
- `GET /api/security/me` - Get current user

### User Management
- `GET /api/security/users` - Get all users (Admin)
- `GET /api/security/users/{id}` - Get user by ID
- `POST /api/security/users/{userId}/roles` - Assign role
- `GET /api/security/users/{userId}/role` - Get user role
- `GET /api/security/users/{userId}/permissions` - Get user permissions
- `POST /api/security/users/{userId}/permissions` - Grant permission
- `DELETE /api/security/users/{userId}/permissions/{permission}` - Revoke permission

### Role Management
- `GET /api/security/roles` - Get all roles (Admin)

### Audit & Security
- `GET /api/security/audit-logs` - Get audit logs
- `GET /api/security/audit-logs/export` - Export audit logs
- `POST /api/security/validate-token` - Validate token

## 🛡️ Security Best Practices

### 1. JWT Configuration
- Use a strong, unique secret key (minimum 256 characters)
- Set appropriate token expiration times
- Use HTTPS in production
- Implement token blacklisting for logout

### 2. Password Security
- Enforce strong password policies
- Implement password hashing with salt
- Consider multi-factor authentication for sensitive accounts
- Regular password expiration policies

### 3. Rate Limiting
- Configure appropriate rate limits per endpoint
- Implement IP-based rate limiting
- Consider user-based rate limiting for authenticated users
- Monitor for abuse patterns

### 4. Audit Logging
- Log all security-relevant events
- Include contextual information (IP, user agent, etc.)
- Implement log retention policies
- Regular log analysis and monitoring

### 5. Authorization
- Follow principle of least privilege
- Use permission-based authorization over role-based
- Regular permission audits
- Implement permission inheritance where appropriate

## 🔍 Security Monitoring

### Key Metrics to Monitor
- Failed login attempts
- Account lockouts
- Token validation failures
- Rate limit violations
- Permission denials
- Suspicious activity patterns

### Alert Configuration
Set up alerts for:
- Multiple failed login attempts from same IP
- Account lockouts
- Unauthorized access attempts
- Unusual API usage patterns
- Security configuration changes

## 🧪 Testing Security

### Unit Tests
Run the comprehensive security test suite:

```bash
dotnet test --filter "Category=Security"
```

### Integration Tests
Test authentication and authorization flows:

```bash
dotnet test --filter "Category=SecurityIntegration"
```

### Security Testing
- Penetration testing
- Vulnerability scanning
- Security code review
- Configuration validation

## 📋 Compliance Checklist

### ✅ Security Requirements
- [ ] JWT authentication implemented
- [ ] Password policies configured
- [ ] Rate limiting enabled
- [ ] Security headers added
- [ ] Audit logging enabled
- [ ] RBAC system configured
- [ ] API authorization implemented
- [ ] Security monitoring configured

### ✅ Configuration
- [ ] JWT secret key configured
- [ ] Token expiration set appropriately
- [ ] Password policies defined
- [ ] Rate limits configured
- [ ] Security headers enabled
- [ ] Audit log retention set

### ✅ Testing
- [ ] Security unit tests passing
- [ ] Integration tests passing
- [ ] Authentication flow tested
- [ ] Authorization flow tested
- [ ] Security monitoring tested

## 🚨 Troubleshooting

### Common Issues

#### JWT Token Not Validating
- Check secret key configuration
- Verify issuer and audience settings
- Ensure token hasn't expired
- Check token format

#### Permission Denied
- Verify user has required role
- Check permission assignments
- Ensure RBAC service is configured
- Check authorization attributes

#### Rate Limiting Issues
- Verify rate limit configuration
- Check client identification
- Monitor rate limit violations
- Adjust limits as needed

#### Audit Logging Issues
- Verify security event logger is registered
- Check log storage configuration
- Ensure proper error handling
- Monitor log performance

## 📚 Additional Resources

### Documentation
- [JWT Specification](https://tools.ietf.org/html/rfc7519)
- [OWASP Authentication Guidelines](https://owasp.org/www-project-application-security-verification-standard/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)

### Tools
- [JWT Debugger](https://jwt.io/)
- [OWASP ZAP](https://www.zaproxy.org/)
- [Security Headers Test](https://securityheaders.com/)

### Support
For security-related issues:
1. Check the audit logs
2. Review the troubleshooting section
3. Contact the security team
4. Create a security ticket

---

**Last Updated**: January 19, 2026
**Version**: 1.0.0
**Security Level**: Enterprise