using Xunit;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DigitalTwin.API.Controllers;
using DigitalTwin.IntegrationTests.Fixtures;

namespace DigitalTwin.IntegrationTests.Tests;

public class AuthFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _unauthenticatedClient;

    public AuthFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _unauthenticatedClient = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithDevCredentials_ReturnsJwtToken()
    {
        // Arrange -- the AuthController only allows "dev" with a non-empty
        // DEV_TEST_PASSWORD environment variable in Development mode.
        var devPassword = Environment.GetEnvironmentVariable("DEV_TEST_PASSWORD");

        // If the env var is not set we cannot test the happy-path login,
        // so we verify the rejection path instead.
        var request = new LoginRequest
        {
            Username = "dev",
            Password = devPassword ?? "some-random-password"
        };

        // Act
        var response = await _unauthenticatedClient.PostAsJsonAsync("/api/auth/login", request);

        if (!string.IsNullOrEmpty(devPassword))
        {
            // Happy path -- valid credentials
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Token.Should().NotBeNullOrWhiteSpace();
            body.RefreshToken.Should().NotBeNullOrWhiteSpace();
            body.ExpiresIn.Should().BeGreaterThan(0);
            body.User.Should().NotBeNull();
            body.User!.Username.Should().Be("dev");
        }
        else
        {
            // Without the env var the controller rejects all logins
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "nonexistent",
            Password = "wrong-password"
        };

        // Act
        var response = await _unauthenticatedClient.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Token.Should().BeNull();
    }

    [Fact]
    public async Task UnauthenticatedRequest_ToProtectedEndpoint_Returns401()
    {
        // Act -- conversation endpoints require [Authorize]
        var response = await _unauthenticatedClient.GetAsync("/api/conversation/history/" + Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedRequest_ToProtectedEndpoint_Succeeds()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act -- GET conversation history should return 200
        var response = await client.GetAsync("/api/conversation/history/" + Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenValidation_WithValidToken_ReturnsValid()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TokenValidationResponse>();
        body.Should().NotBeNull();
        body!.Valid.Should().BeTrue();
        body.User.Should().NotBeNull();
        body.User!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task TokenValidation_WithNoToken_Returns401()
    {
        // The /api/auth/validate endpoint has [Authorize], so
        // the middleware rejects before the action runs.
        var response = await _unauthenticatedClient.GetAsync("/api/auth/validate");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_NewUser_ReturnsToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = $"newuser_{Guid.NewGuid():N}",
            Email = $"newuser_{Guid.NewGuid():N}@example.com",
            Password = "SecurePassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _unauthenticatedClient.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Token.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.User.Should().NotBeNull();
        body.User!.Username.Should().Be(request.Username);
    }

    [Fact]
    public async Task Register_ExistingUser_Returns409Conflict()
    {
        // Arrange -- "admin" is in the hard-coded existing-users list
        var request = new RegisterRequest
        {
            Username = "admin",
            Email = "admin@example.com",
            Password = "SecurePassword123!",
            FirstName = "Admin",
            LastName = "User"
        };

        // Act
        var response = await _unauthenticatedClient.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task BogusToken_IsRejected()
    {
        // Arrange -- craft a client with a random string as the Bearer token
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt");

        // Act
        var response = await client.GetAsync("/api/conversation/history/" + Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
