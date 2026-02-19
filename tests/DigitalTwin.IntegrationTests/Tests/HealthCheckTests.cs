using Xunit;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using DigitalTwin.IntegrationTests.Fixtures;

namespace DigitalTwin.IntegrationTests.Tests;

public class HealthCheckTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("status").GetString().Should().Be("healthy");
        root.GetProperty("service").GetString().Should().Be("Digital Twin Emotional Companion API");
        root.GetProperty("version").GetString().Should().Be("1.0.0");
        root.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task RootEndpoint_ReturnsApiInfo()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("name").GetString().Should().Be("Digital Twin Emotional Companion API");
        root.GetProperty("version").GetString().Should().Be("1.0.0");
        root.GetProperty("description").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("endpoints").GetProperty("health").GetString().Should().Be("/health");
        root.GetProperty("endpoints").GetProperty("conversation").GetString().Should().Be("/api/conversation");
    }

    [Fact]
    public async Task HealthEndpoint_DoesNotRequireAuthentication()
    {
        // The client has no Authorization header at all.
        // Health should still return 200.
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RootEndpoint_DoesNotRequireAuthentication()
    {
        var response = await _client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
