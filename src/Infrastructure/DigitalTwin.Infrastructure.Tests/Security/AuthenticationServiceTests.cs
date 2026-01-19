using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using DigitalTwin.Core.Security;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Models;
using DigitalTwin.Core.Enums;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;

namespace DigitalTwin.Tests.Security
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IOptions<JwtConfiguration>> _jwtConfigMock;
        private readonly Mock<IOptions<PasswordConfiguration>> _passwordConfigMock;
        private readonly Mock<ISecurityEventLogger> _securityEventLoggerMock;
        private readonly AuthenticationService _authService;
        private readonly JwtConfiguration _jwtConfig;
        private readonly PasswordConfiguration _passwordConfig;

        public AuthenticationServiceTests()
        {
            _jwtConfig = new JwtConfiguration
            {
                SecretKey = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789",
                Issuer = "DigitalTwin.Test",
                Audience = "DigitalTwin.Test",
                ExpiryInMinutes = 60,
                RefreshTokenExpiryInDays = 7
            };

            _passwordConfig = new PasswordConfiguration
            {
                MinLength = 8,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireDigits = true,
                RequireSpecialChars = true,
                MaxFailedAttempts = 5,
                LockoutDurationMinutes = 15
            };

            _jwtConfigMock = new Mock<IOptions<JwtConfiguration>>();
            _jwtConfigMock.Setup(x => x.Value).Returns(_jwtConfig);

            _passwordConfigMock = new Mock<IOptions<PasswordConfiguration>>();
            _passwordConfigMock.Setup(x => x.Value).Returns(_passwordConfig);

            _securityEventLoggerMock = new Mock<ISecurityEventLogger>();

            _authService = new AuthenticationService(
                _jwtConfigMock.Object,
                _passwordConfigMock.Object,
                _securityEventLoggerMock.Object);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldCreateUser_WhenValidData()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            var result = await _authService.RegisterUserAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Email, result.Email);
            Assert.Equal(request.FirstName, result.FirstName);
            Assert.Equal(request.LastName, result.LastName);
            Assert.NotNull(result.Id);
            Assert.True(result.IsActive);

            _securityEventLoggerMock.Verify(
                x => x.LogSecurityEventAsync(
                    It.Is<SecurityEvent>(e => 
                        e.EventType == SecurityEventType.UserRegistered &&
                        e.UserId == result.Id)),
                Times.Once);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldThrowException_WhenEmailExists()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            // First registration should succeed
            await _authService.RegisterUserAsync(request);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.RegisterUserAsync(request));
        }

        [Fact]
        public async Task AuthenticateUserAsync_ShouldReturnTokens_WhenValidCredentials()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            var user = await _authService.RegisterUserAsync(request);
            var loginRequest = new LoginRequest
            {
                Email = request.Email,
                Password = request.Password
            };

            // Act
            var result = await _authService.AuthenticateUserAsync(loginRequest);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            Assert.Equal(user.Id, result.User.Id);
            Assert.Equal(request.Email, result.User.Email);
            Assert.True(result.ExpiresOn > DateTime.UtcNow);

            _securityEventLoggerMock.Verify(
                x => x.LogSecurityEventAsync(
                    It.Is<SecurityEvent>(e => 
                        e.EventType == SecurityEventType.UserLoggedIn &&
                        e.UserId == user.Id)),
                Times.Once);
        }

        [Fact]
        public async Task AuthenticateUserAsync_ShouldThrowException_WhenInvalidCredentials()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "wrongpassword"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _authService.AuthenticateUserAsync(loginRequest));
        }

        [Fact]
        public async Task AuthenticateUserAsync_ShouldLockAccount_WhenMaxFailedAttemptsReached()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            var user = await _authService.RegisterUserAsync(request);
            var loginRequest = new LoginRequest
            {
                Email = request.Email,
                Password = "wrongpassword"
            };

            // Act - Attempt failed login multiple times
            for (int i = 0; i < _passwordConfig.MaxFailedAttempts; i++)
            {
                try
                {
                    await _authService.AuthenticateUserAsync(loginRequest);
                }
                catch (UnauthorizedAccessException)
                {
                    // Expected exception
                }
            }

            // Assert - Try login with correct password should fail due to lockout
            var correctLoginRequest = new LoginRequest
            {
                Email = request.Email,
                Password = request.Password
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.AuthenticateUserAsync(correctLoginRequest));

            _securityEventLoggerMock.Verify(
                x => x.LogSecurityEventAsync(
                    It.Is<SecurityEvent>(e => 
                        e.EventType == SecurityEventType.AccountLocked &&
                        e.UserId == user.Id)),
                Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnNewTokens_WhenValidRefreshToken()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            var user = await _authService.RegisterUserAsync(request);
            var loginRequest = new LoginRequest
            {
                Email = request.Email,
                Password = request.Password
            };

            var loginResult = await _authService.AuthenticateUserAsync(loginRequest);
            var refreshRequest = new RefreshTokenRequest
            {
                RefreshToken = loginResult.RefreshToken
            };

            // Act
            var result = await _authService.RefreshTokenAsync(refreshRequest);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            Assert.NotEqual(loginResult.AccessToken, result.AccessToken);
            Assert.NotEqual(loginResult.RefreshToken, result.RefreshToken);
            Assert.Equal(user.Id, result.User.Id);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldThrowException_WhenInvalidRefreshToken()
        {
            // Arrange
            var refreshRequest = new RefreshTokenRequest
            {
                RefreshToken = "invalid_refresh_token"
            };

            // Act & Assert
            await Assert.ThrowsAsync<SecurityTokenException>(
                () => _authService.RefreshTokenAsync(refreshRequest));
        }

        [Fact]
        public async Task LogoutAsync_ShouldInvalidateRefreshToken_WhenValidToken()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            var user = await _authService.RegisterUserAsync(request);
            var loginRequest = new LoginRequest
            {
                Email = request.Email,
                Password = request.Password
            };

            var loginResult = await _authService.AuthenticateUserAsync(loginRequest);
            var logoutRequest = new LogoutRequest
            {
                RefreshToken = loginResult.RefreshToken
            };

            // Act
            await _authService.LogoutAsync(logoutRequest);

            // Assert - Try to refresh token should fail
            var refreshRequest = new RefreshTokenRequest
            {
                RefreshToken = loginResult.RefreshToken
            };

            await Assert.ThrowsAsync<SecurityTokenException>(
                () => _authService.RefreshTokenAsync(refreshRequest));

            _securityEventLoggerMock.Verify(
                x => x.LogSecurityEventAsync(
                    It.Is<SecurityEvent>(e => 
                        e.EventType == SecurityEventType.UserLoggedOut &&
                        e.UserId == user.Id)),
                Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldUpdatePassword_WhenValidCurrentPassword()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            var user = await _authService.RegisterUserAsync(request);
            var changePasswordRequest = new ChangePasswordRequest
            {
                CurrentPassword = request.Password,
                NewPassword = "NewTest123!@#"
            };

            // Act
            await _authService.ChangePasswordAsync(user.Id, changePasswordRequest);

            // Assert - Try login with old password should fail
            var oldLoginRequest = new LoginRequest
            {
                Email = request.Email,
                Password = request.Password
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _authService.AuthenticateUserAsync(oldLoginRequest));

            // Try login with new password should succeed
            var newLoginRequest = new LoginRequest
            {
                Email = request.Email,
                Password = changePasswordRequest.NewPassword
            };

            var result = await _authService.AuthenticateUserAsync(newLoginRequest);
            Assert.NotNull(result);

            _securityEventLoggerMock.Verify(
                x => x.LogSecurityEventAsync(
                    It.Is<SecurityEvent>(e => 
                        e.EventType == SecurityEventType.PasswordChanged &&
                        e.UserId == user.Id)),
                Times.Once);
        }

        [Fact]
        public async Task ValidateTokenAsync_ShouldReturnTrue_WhenValidToken()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            var user = await _authService.RegisterUserAsync(request);
            var loginRequest = new LoginRequest
            {
                Email = request.Email,
                Password = request.Password
            };

            var loginResult = await _authService.AuthenticateUserAsync(loginRequest);

            // Act
            var result = await _authService.ValidateTokenAsync(loginResult.AccessToken);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateTokenAsync_ShouldReturnFalse_WhenInvalidToken()
        {
            // Act
            var result = await _authService.ValidateTokenAsync("invalid_token");

            // Assert
            Assert.False(result);
        }
    }

    public class SecurityEventLoggerTests
    {
        [Fact]
        public async Task LogSecurityEventAsync_ShouldCreateEvent_WhenValidData()
        {
            // Arrange
            var @event = new SecurityEvent
            {
                EventType = SecurityEventType.UserLoggedIn,
                UserId = "user123",
                Description = "User logged in successfully",
                IpAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0..."
            };

            var logger = new SecurityEventLogger();

            // Act
            await logger.LogSecurityEventAsync(@event);

            // Assert
            var events = await logger.GetSecurityEventsAsync();
            Assert.Single(events);
            Assert.Equal(@event.EventType, events[0].EventType);
            Assert.Equal(@event.UserId, events[0].UserId);
            Assert.Equal(@event.Description, events[0].Description);
        }

        [Fact]
        public async Task GetSecurityEventsAsync_ShouldReturnPagedResults_WhenMultipleEvents()
        {
            // Arrange
            var logger = new SecurityEventLogger();

            for (int i = 0; i < 25; i++)
            {
                await logger.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.UserLoggedIn,
                    UserId = $"user{i}",
                    Description = $"Event {i}"
                });
            }

            // Act
            var page1 = await logger.GetSecurityEventsAsync(1, 10);
            var page2 = await logger.GetSecurityEventsAsync(2, 10);
            var page3 = await logger.GetSecurityEventsAsync(3, 10);

            // Assert
            Assert.Equal(10, page1.Count);
            Assert.Equal(10, page2.Count);
            Assert.Equal(5, page3.Count);
        }
    }
}