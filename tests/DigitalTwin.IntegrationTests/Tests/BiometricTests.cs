using Xunit;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DigitalTwin.API.Controllers;
using DigitalTwin.IntegrationTests.Fixtures;

namespace DigitalTwin.IntegrationTests.Tests;

public class BiometricTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BiometricTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task StoreBiometricReading_ReturnsCreatedReading()
    {
        // Arrange
        var request = new BiometricReadingRequest
        {
            Type = "heart_rate",
            Value = 72.0,
            Unit = "bpm",
            Source = "apple_health"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/biometric", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = root.GetProperty("data");
        data.GetProperty("type").GetString().Should().Be("heart_rate");
        data.GetProperty("value").GetDouble().Should().Be(72.0);
        data.GetProperty("unit").GetString().Should().Be("bpm");
        data.GetProperty("source").GetString().Should().Be("apple_health");
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("userId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetReadings_ByUser_ReturnsStoredReadings()
    {
        // Arrange -- store a reading first
        var storeRequest = new BiometricReadingRequest
        {
            Type = "hrv",
            Value = 55.0,
            Unit = "ms",
            Source = "apple_health"
        };
        var storeResponse = await _client.PostAsJsonAsync("/api/biometric", storeRequest);
        storeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await _client.GetAsync("/api/biometric");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = root.GetProperty("data");
        data.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetReadings_FilteredByType_ReturnsMatchingReadings()
    {
        // Arrange -- store two different types
        await _client.PostAsJsonAsync("/api/biometric", new BiometricReadingRequest
        {
            Type = "steps",
            Value = 8500,
            Unit = "count",
            Source = "google_fit"
        });
        await _client.PostAsJsonAsync("/api/biometric", new BiometricReadingRequest
        {
            Type = "sleep_quality",
            Value = 85.0,
            Unit = "percent",
            Source = "manual"
        });

        // Act -- filter by steps
        var response = await _client.GetAsync("/api/biometric?type=steps");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = root.GetProperty("data");
        data.GetArrayLength().Should().BeGreaterThan(0);

        // All returned readings should be of type "steps"
        foreach (var reading in data.EnumerateArray())
        {
            reading.GetProperty("type").GetString().Should().Be("steps");
        }
    }

    [Fact]
    public async Task GetLatestReading_ReturnsLatestOfType()
    {
        // Arrange -- store two heart_rate readings
        await _client.PostAsJsonAsync("/api/biometric", new BiometricReadingRequest
        {
            Type = "heart_rate",
            Value = 68.0,
            Unit = "bpm",
            Source = "manual"
        });
        await _client.PostAsJsonAsync("/api/biometric", new BiometricReadingRequest
        {
            Type = "heart_rate",
            Value = 75.0,
            Unit = "bpm",
            Source = "manual"
        });

        // Act
        var response = await _client.GetAsync("/api/biometric/latest/heart_rate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SyncReadings_StoresMultipleReadings()
    {
        // Arrange
        var readings = new List<BiometricReadingRequest>
        {
            new() { Type = "heart_rate", Value = 70.0, Unit = "bpm", Source = "apple_health" },
            new() { Type = "steps", Value = 5000, Unit = "count", Source = "apple_health" },
            new() { Type = "hrv", Value = 45.0, Unit = "ms", Source = "apple_health" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/biometric/sync", readings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("data").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task Biometric_RequiresAuthentication()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var getResponse = await unauthenticatedClient.GetAsync("/api/biometric");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var postResponse = await unauthenticatedClient.PostAsJsonAsync("/api/biometric",
            new BiometricReadingRequest { Type = "heart_rate", Value = 72, Unit = "bpm" });
        postResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
