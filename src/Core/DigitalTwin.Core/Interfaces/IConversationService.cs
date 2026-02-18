using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface IConversationService
    {
        Task<ConversationResponse> StartConversationAsync(Guid userId, string initialMessage = null);
        Task<ConversationResponse> ProcessMessageAsync(Guid userId, string message);
        Task<bool> EndConversationAsync(Guid userId);
        Task<ConversationSession> GetActiveSessionAsync(Guid userId);
        Task<List<EmotionalMemory>> GetConversationMemoriesAsync(Guid userId, int limit = 50);
    }
}
