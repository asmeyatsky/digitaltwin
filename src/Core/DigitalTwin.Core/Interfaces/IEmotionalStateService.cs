using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface IEmotionalStateService
    {
        Task<bool> StoreEmotionalMemoryAsync(EmotionalMemory memory);
        Task<List<EmotionalMemory>> GetUserMemoriesAsync(Guid userId, int limit = 50);
        Task<List<EmotionalMemory>> GetRelevantMemoriesAsync(Guid userId, string context, int limit = 10);
        Task<EmotionalTrend> AnalyzeEmotionalTrendsAsync(Guid userId, TimeSpan period);
        Task<bool> UpdateMemoryImportanceAsync(Guid memoryId, Guid userId, int importance);
    }
}
