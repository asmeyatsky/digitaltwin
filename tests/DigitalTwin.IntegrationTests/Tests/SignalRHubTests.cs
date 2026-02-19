using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
using DigitalTwin.IntegrationTests.Fixtures;

namespace DigitalTwin.IntegrationTests.Tests;

public class SignalRHubTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HubConnection? _connection;

    public SignalRHubTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    /// <summary>
    /// Builds a HubConnection that routes through the test server's in-process handler.
    /// When a JWT token is provided the connection authenticates as that user.
    /// </summary>
    private HubConnection CreateHubConnection(string? token = null)
    {
        var server = _factory.Server;

        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{server.BaseAddress}hubs/companion", options =>
            {
                // Use the test server's message handler so requests stay in-process.
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();

                if (token is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                }
            })
            .Build();

        return hubConnection;
    }

    // ---------------------------------------------------------------
    // Connection / Authentication tests
    // ---------------------------------------------------------------

    [Fact]
    public async Task AuthenticatedUser_CanConnectToHub()
    {
        // Arrange
        var token = _factory.GenerateTestToken(
            _factory.TestUserId, _factory.TestUsername, new[] { "User" });

        _connection = CreateHubConnection(token);

        // Act
        await _connection.StartAsync();

        // Assert
        _connection.State.Should().Be(HubConnectionState.Connected);
    }

    [Fact]
    public async Task UnauthenticatedUser_IsRejectedByHub()
    {
        // Arrange -- no token
        _connection = CreateHubConnection(token: null);

        // Act
        Func<Task> act = () => _connection.StartAsync();

        // Assert -- the [Authorize] attribute should cause a failure
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task InvalidToken_IsRejectedByHub()
    {
        // Arrange -- garbage token that will fail validation
        _connection = CreateHubConnection(token: "this-is-not-a-valid-jwt");

        // Act
        Func<Task> act = () => _connection.StartAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    // ---------------------------------------------------------------
    // SendMessage tests
    // ---------------------------------------------------------------

    [Fact]
    public async Task SendMessage_BroadcastsToGroupMembers()
    {
        // Arrange -- two users connect and join the same room
        var roomId = Guid.NewGuid().ToString();

        var token1 = _factory.GenerateTestToken("user-1", "alice", new[] { "User" });
        var token2 = _factory.GenerateTestToken("user-2", "bob", new[] { "User" });

        var conn1 = CreateHubConnection(token1);
        var conn2 = CreateHubConnection(token2);
        _connection = conn1; // ensure cleanup

        using var receivedMessage = new SemaphoreSlim(0, 1);
        string? receivedContent = null;

        conn2.On<JsonElement>("ReceiveMessage", msg =>
        {
            receivedContent = msg.GetProperty("message").GetString();
            receivedMessage.Release();
        });

        await conn1.StartAsync();
        await conn2.StartAsync();

        // Both join the room (JoinRoom may fail because the in-memory service
        // does not have the room, but SendMessage broadcasts directly to the
        // SignalR group so we manually add them via JoinRoom and accept errors).
        // Instead, we first call SendMessage after joining the group. JoinRoom
        // adds to group even if service returns null (it does return null for
        // unknown rooms and sends Error to caller). We test the SendMessage
        // broadcast path by having conn1 join and send.
        //
        // To keep the test focused on SignalR transport, we invoke SendMessage
        // which unconditionally broadcasts to the group.

        // Join both to the group via the hub method (group add happens regardless)
        try { await conn1.InvokeAsync("JoinRoom", roomId); } catch { /* room not in db */ }
        try { await conn2.InvokeAsync("JoinRoom", roomId); } catch { /* room not in db */ }

        // Act
        await conn1.InvokeAsync("SendMessage", roomId, "Hello from Alice");

        // Assert
        var received = await receivedMessage.WaitAsync(TimeSpan.FromSeconds(5));
        received.Should().BeTrue("conn2 should have received the broadcast message");
        receivedContent.Should().Be("Hello from Alice");

        // Cleanup
        await conn2.DisposeAsync();
    }

    // ---------------------------------------------------------------
    // JoinRoom / LeaveRoom tests
    // ---------------------------------------------------------------

    [Fact]
    public async Task JoinRoom_InvalidRoom_SendsErrorToCaller()
    {
        // Arrange
        var token = _factory.GenerateTestToken(
            _factory.TestUserId, _factory.TestUsername, new[] { "User" });
        _connection = CreateHubConnection(token);

        using var errorReceived = new SemaphoreSlim(0, 1);
        string? errorMessage = null;

        _connection.On<string>("Error", msg =>
        {
            errorMessage = msg;
            errorReceived.Release();
        });

        await _connection.StartAsync();

        // Act -- join a room that does not exist in the database
        await _connection.InvokeAsync("JoinRoom", Guid.NewGuid().ToString());

        // Assert
        var received = await errorReceived.WaitAsync(TimeSpan.FromSeconds(5));
        received.Should().BeTrue("caller should receive an Error callback for nonexistent room");
        errorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task LeaveRoom_SendsUserLeftToGroup()
    {
        // Arrange
        var roomId = Guid.NewGuid().ToString();
        var token1 = _factory.GenerateTestToken("user-a", "user-a", new[] { "User" });
        var token2 = _factory.GenerateTestToken("user-b", "user-b", new[] { "User" });

        var conn1 = CreateHubConnection(token1);
        var conn2 = CreateHubConnection(token2);
        _connection = conn1;

        using var userLeftReceived = new SemaphoreSlim(0, 1);
        string? leftUserId = null;

        conn1.On<JsonElement>("UserLeft", msg =>
        {
            leftUserId = msg.GetProperty("userId").GetString();
            userLeftReceived.Release();
        });

        await conn1.StartAsync();
        await conn2.StartAsync();

        // Join both to the group
        try { await conn1.InvokeAsync("JoinRoom", roomId); } catch { }
        try { await conn2.InvokeAsync("JoinRoom", roomId); } catch { }

        // Act -- conn2 leaves
        await conn2.InvokeAsync("LeaveRoom", roomId);

        // Assert -- conn1 should receive UserLeft
        var received = await userLeftReceived.WaitAsync(TimeSpan.FromSeconds(5));
        received.Should().BeTrue("remaining group member should receive UserLeft notification");
        leftUserId.Should().Be("user-b");

        await conn2.DisposeAsync();
    }

    // ---------------------------------------------------------------
    // ShareEmotion tests
    // ---------------------------------------------------------------

    [Fact]
    public async Task ShareEmotion_BroadcastsEmotionToGroup()
    {
        // Arrange
        var roomId = Guid.NewGuid().ToString();
        var token1 = _factory.GenerateTestToken("emo-user-1", "emo1", new[] { "User" });
        var token2 = _factory.GenerateTestToken("emo-user-2", "emo2", new[] { "User" });

        var conn1 = CreateHubConnection(token1);
        var conn2 = CreateHubConnection(token2);
        _connection = conn1;

        using var emotionReceived = new SemaphoreSlim(0, 1);
        string? receivedEmotion = null;
        double? receivedConfidence = null;

        conn2.On<JsonElement>("EmotionShared", msg =>
        {
            receivedEmotion = msg.GetProperty("emotion").GetString();
            receivedConfidence = msg.GetProperty("confidence").GetDouble();
            emotionReceived.Release();
        });

        await conn1.StartAsync();
        await conn2.StartAsync();

        try { await conn1.InvokeAsync("JoinRoom", roomId); } catch { }
        try { await conn2.InvokeAsync("JoinRoom", roomId); } catch { }

        // Act
        await conn1.InvokeAsync("ShareEmotion", roomId, "joy", 0.95);

        // Assert
        var received = await emotionReceived.WaitAsync(TimeSpan.FromSeconds(5));
        received.Should().BeTrue("group members should receive the shared emotion");
        receivedEmotion.Should().Be("joy");
        receivedConfidence.Should().BeApproximately(0.95, 0.001);

        await conn2.DisposeAsync();
    }

    // ---------------------------------------------------------------
    // SyncAvatar tests
    // ---------------------------------------------------------------

    [Fact]
    public async Task SyncAvatar_BroadcastsToOthersInGroup()
    {
        // Arrange
        var roomId = Guid.NewGuid().ToString();
        var token1 = _factory.GenerateTestToken("avatar-user-1", "av1", new[] { "User" });
        var token2 = _factory.GenerateTestToken("avatar-user-2", "av2", new[] { "User" });

        var conn1 = CreateHubConnection(token1);
        var conn2 = CreateHubConnection(token2);
        _connection = conn1;

        using var avatarReceived = new SemaphoreSlim(0, 1);
        string? syncUserId = null;

        conn2.On<JsonElement>("AvatarSync", msg =>
        {
            syncUserId = msg.GetProperty("userId").GetString();
            avatarReceived.Release();
        });

        await conn1.StartAsync();
        await conn2.StartAsync();

        try { await conn1.InvokeAsync("JoinRoom", roomId); } catch { }
        try { await conn2.InvokeAsync("JoinRoom", roomId); } catch { }

        // Act -- conn1 syncs avatar state; only others (conn2) should receive it
        var avatarState = new { pose = "wave", intensity = 0.8 };
        await conn1.InvokeAsync("SyncAvatar", roomId, avatarState);

        // Assert
        var received = await avatarReceived.WaitAsync(TimeSpan.FromSeconds(5));
        received.Should().BeTrue("other group members should receive the avatar sync");
        syncUserId.Should().Be("avatar-user-1");

        await conn2.DisposeAsync();
    }

    [Fact]
    public async Task SyncAvatar_SenderDoesNotReceiveOwnBroadcast()
    {
        // Arrange
        var roomId = Guid.NewGuid().ToString();
        var token = _factory.GenerateTestToken("solo-user", "solo", new[] { "User" });

        _connection = CreateHubConnection(token);

        var selfReceived = false;
        _connection.On<JsonElement>("AvatarSync", _ =>
        {
            selfReceived = true;
        });

        await _connection.StartAsync();

        try { await _connection.InvokeAsync("JoinRoom", roomId); } catch { }

        // Act
        await _connection.InvokeAsync("SyncAvatar", roomId, new { pose = "idle" });

        // Give a moment for any unexpected callback
        await Task.Delay(500);

        // Assert -- SyncAvatar uses Clients.OthersInGroup, so sender should NOT receive it
        selfReceived.Should().BeFalse("SyncAvatar should only broadcast to others, not the sender");
    }

    // ---------------------------------------------------------------
    // Disconnect test
    // ---------------------------------------------------------------

    [Fact]
    public async Task Disconnect_CompletesCleanly()
    {
        // Arrange
        var token = _factory.GenerateTestToken(
            _factory.TestUserId, _factory.TestUsername, new[] { "User" });
        _connection = CreateHubConnection(token);

        await _connection.StartAsync();
        _connection.State.Should().Be(HubConnectionState.Connected);

        // Act
        await _connection.StopAsync();

        // Assert
        _connection.State.Should().Be(HubConnectionState.Disconnected);
    }
}
