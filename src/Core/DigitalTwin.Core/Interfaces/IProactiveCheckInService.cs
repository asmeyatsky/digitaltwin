using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface IProactiveCheckInService
    {
        Task<CheckInSuggestion?> EvaluateCheckInAsync(string userId);
        Task RecordCheckInAsync(string userId, string type, string? emotionContext);
        Task<List<CheckInRecord>> GetPendingCheckInsAsync(string userId);
        Task RespondToCheckInAsync(Guid checkInId, string response);
    }

    public class CheckInSuggestion
    {
        public string Type { get; set; } = "daily";
        public string Message { get; set; } = string.Empty;
        public string? EmotionContext { get; set; }
        public DateTime SuggestedAt { get; set; } = DateTime.UtcNow;
    }
}
