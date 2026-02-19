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
        Task<ConversationResponse> ProcessMessageAsync(Guid userId, Guid conversationId, string message);
        Task<bool> EndConversationAsync(Guid userId);
        Task<ConversationSession> GetActiveSessionAsync(Guid userId);
        Task<List<EmotionalMemory>> GetConversationMemoriesAsync(Guid userId, int limit = 50);
        Task<ConversationHistoryResult> GetConversationHistoryAsync(Guid userId, Guid conversationId, int page = 1, int pageSize = 50);
    }

    public class ConversationHistoryResult
    {
        public List<ConversationHistoryItem> Messages { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class ConversationHistoryItem
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Response { get; set; }
        public string UserEmotion { get; set; } = string.Empty;
        public string? AIEmotion { get; set; }
        public DateTime Timestamp { get; set; }
        public string MessageType { get; set; } = string.Empty;
    }
}
