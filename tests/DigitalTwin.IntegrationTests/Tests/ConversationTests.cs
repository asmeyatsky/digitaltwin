using Xunit;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DigitalTwin.API.Controllers;
using DigitalTwin.IntegrationTests.Fixtures;

namespace DigitalTwin.IntegrationTests.Tests;

public class ConversationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ConversationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task StartConversation_ReturnsSessionId()
    {
        // Arrange
        var request = new ConversationStartRequest
        {
            Message = "Hello, I would like to talk."
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversation/start", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = root.GetProperty("data");
        data.GetProperty("sessionId").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("response").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("emotionalTone").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendMessage_ReturnsAIResponseWithEmotion()
    {
        // Arrange -- start a conversation first to get a valid session
        var startRequest = new ConversationStartRequest { Message = "Hi there" };
        var startResponse = await _client.PostAsJsonAsync("/api/conversation/start", startRequest);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startJson = JsonDocument.Parse(startContent);
        var sessionId = startJson.RootElement.GetProperty("data").GetProperty("sessionId").GetGuid();

        var messageRequest = new ConversationMessageRequest
        {
            ConversationId = sessionId,
            Message = "I am feeling happy today!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversation/message", messageRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = root.GetProperty("data");
        data.GetProperty("response").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("detectedEmotion").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("aiEmotionalTone").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("conversationId").GetGuid().Should().Be(sessionId);
    }

    [Fact]
    public async Task EndConversation_ReturnsSuccess()
    {
        // Arrange -- start a conversation
        var startRequest = new ConversationStartRequest { Message = "Quick chat" };
        var startResponse = await _client.PostAsJsonAsync("/api/conversation/start", startRequest);
        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startJson = JsonDocument.Parse(startContent);
        var sessionId = startJson.RootElement.GetProperty("data").GetProperty("sessionId").GetGuid();

        var endRequest = new ConversationEndRequest
        {
            ConversationId = sessionId,
            SessionDuration = TimeSpan.FromMinutes(5)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversation/end", endRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SendMessage_WithEmptyMessage_ReturnsBadRequest()
    {
        // Arrange -- the ConversationMessageRequest.Message has
        // [Required][StringLength(5000, MinimumLength = 1)] validation.
        var request = new ConversationMessageRequest
        {
            ConversationId = Guid.NewGuid(),
            Message = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversation/message", request);

        // Assert -- model validation should reject the empty message
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartConversation_WithEmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var request = new ConversationStartRequest
        {
            Message = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversation/start", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetConversationHistory_ReturnsPagedResults()
    {
        // Arrange -- create a conversation so there is data
        var startRequest = new ConversationStartRequest { Message = "History test message" };
        await _client.PostAsJsonAsync("/api/conversation/start", startRequest);

        // Act
        var response = await _client.GetAsync($"/api/conversation/history/{Guid.NewGuid()}?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.TryGetProperty("messages", out _).Should().BeTrue();
        data.GetProperty("page").GetInt32().Should().Be(1);
        data.GetProperty("pageSize").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task Conversation_RequiresAuthentication()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var startResponse = await unauthenticatedClient.PostAsJsonAsync(
            "/api/conversation/start",
            new ConversationStartRequest { Message = "hello" });
        startResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var messageResponse = await unauthenticatedClient.PostAsJsonAsync(
            "/api/conversation/message",
            new ConversationMessageRequest { ConversationId = Guid.NewGuid(), Message = "hello" });
        messageResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var endResponse = await unauthenticatedClient.PostAsJsonAsync(
            "/api/conversation/end",
            new ConversationEndRequest { ConversationId = Guid.NewGuid() });
        endResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
