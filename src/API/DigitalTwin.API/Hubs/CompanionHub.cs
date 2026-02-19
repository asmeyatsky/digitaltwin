using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.API.Hubs
{
    [Authorize]
    public class CompanionHub : Hub
    {
        private readonly ISharedExperienceService _sharedExperienceService;

        public CompanionHub(ISharedExperienceService sharedExperienceService)
        {
            _sharedExperienceService = sharedExperienceService;
        }

        private string GetUserId() =>
            Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        public async Task JoinRoom(string roomId)
        {
            var userId = GetUserId();
            var room = await _sharedExperienceService.JoinRoomAsync(Guid.Parse(roomId), userId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Room not found or inactive");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserJoined", new { userId, roomId, timestamp = DateTime.UtcNow });
        }

        public async Task LeaveRoom(string roomId)
        {
            var userId = GetUserId();
            await _sharedExperienceService.LeaveRoomAsync(Guid.Parse(roomId), userId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserLeft", new { userId, roomId, timestamp = DateTime.UtcNow });
        }

        public async Task SendMessage(string roomId, string message)
        {
            var userId = GetUserId();
            await Clients.Group(roomId).SendAsync("ReceiveMessage", new
            {
                userId,
                roomId,
                message,
                timestamp = DateTime.UtcNow
            });
        }

        public async Task ShareEmotion(string roomId, string emotion, double confidence)
        {
            var userId = GetUserId();
            await Clients.Group(roomId).SendAsync("EmotionShared", new
            {
                userId,
                roomId,
                emotion,
                confidence,
                timestamp = DateTime.UtcNow
            });
        }

        public async Task SyncAvatar(string roomId, object avatarState)
        {
            var userId = GetUserId();
            await Clients.OthersInGroup(roomId).SendAsync("AvatarSync", new
            {
                userId,
                roomId,
                avatarState,
                timestamp = DateTime.UtcNow
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
