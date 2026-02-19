using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface IPersonalHistoryService
    {
        Task<LifeEvent> AddLifeEventAsync(Guid userId, LifeEvent evt);
        Task<LifeEvent?> UpdateLifeEventAsync(Guid userId, Guid eventId, LifeEvent evt);
        Task<bool> DeleteLifeEventAsync(Guid userId, Guid eventId);
        Task<List<LifeEvent>> GetTimelineAsync(Guid userId, DateTime? start, DateTime? end);
        Task<List<LifeEvent>> GetUpcomingEventsAsync(Guid userId, int daysAhead = 30);
        Task<ConversationLifeContext> GetContextForConversationAsync(Guid userId);
        Task<PersonalContext> UpdatePersonalContextAsync(Guid userId, PersonalContext context);
        Task<PersonalContext?> GetPersonalContextAsync(Guid userId);
    }
}
