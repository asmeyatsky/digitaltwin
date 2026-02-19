using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface ISharedExperienceService
    {
        Task<SharedRoom> CreateRoomAsync(string name, string creatorUserId);
        Task<SharedRoom?> JoinRoomAsync(Guid roomId, string userId);
        Task<bool> LeaveRoomAsync(Guid roomId, string userId);
        Task<List<SharedRoom>> GetActiveRoomsAsync(string userId);
    }
}
