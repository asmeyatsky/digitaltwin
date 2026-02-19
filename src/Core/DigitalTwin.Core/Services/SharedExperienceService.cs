using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class SharedExperienceService : ISharedExperienceService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<SharedExperienceService> _logger;

        public SharedExperienceService(DigitalTwinDbContext context, ILogger<SharedExperienceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SharedRoom> CreateRoomAsync(string name, string creatorUserId)
        {
            var room = new SharedRoom
            {
                Name = name,
                CreatorUserId = creatorUserId,
                Participants = new List<string> { creatorUserId }
            };

            _context.SharedRooms.Add(room);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Room {RoomId} created by {UserId}", room.Id, creatorUserId);
            return room;
        }

        public async Task<SharedRoom?> JoinRoomAsync(Guid roomId, string userId)
        {
            var room = await _context.SharedRooms.FindAsync(roomId);
            if (room == null || !room.IsActive) return null;

            if (!room.Participants.Contains(userId))
            {
                room.Participants.Add(userId);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
            return room;
        }

        public async Task<bool> LeaveRoomAsync(Guid roomId, string userId)
        {
            var room = await _context.SharedRooms.FindAsync(roomId);
            if (room == null) return false;

            room.Participants.Remove(userId);

            if (room.Participants.Count == 0)
                room.IsActive = false;

            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} left room {RoomId}", userId, roomId);
            return true;
        }

        public async Task<List<SharedRoom>> GetActiveRoomsAsync(string userId)
        {
            return await _context.SharedRooms
                .Where(r => r.IsActive && r.Participants.Contains(userId))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
