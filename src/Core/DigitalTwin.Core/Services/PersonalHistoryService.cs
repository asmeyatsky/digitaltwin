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
    public class PersonalHistoryService : IPersonalHistoryService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<PersonalHistoryService> _logger;

        public PersonalHistoryService(
            DigitalTwinDbContext context,
            ILogger<PersonalHistoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<LifeEvent> AddLifeEventAsync(Guid userId, LifeEvent evt)
        {
            evt.UserId = userId;
            evt.CreatedAt = DateTime.UtcNow;

            _context.LifeEvents.Add(evt);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added life event {EventId} for user {UserId}", evt.Id, userId);
            return evt;
        }

        public async Task<LifeEvent?> UpdateLifeEventAsync(Guid userId, Guid eventId, LifeEvent evt)
        {
            var existing = await _context.LifeEvents
                .FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId);

            if (existing == null)
                return null;

            existing.Title = evt.Title;
            existing.Description = evt.Description;
            existing.EventDate = evt.EventDate;
            existing.Category = evt.Category;
            existing.EmotionalImpact = evt.EmotionalImpact;
            existing.IsRecurring = evt.IsRecurring;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated life event {EventId} for user {UserId}", eventId, userId);
            return existing;
        }

        public async Task<bool> DeleteLifeEventAsync(Guid userId, Guid eventId)
        {
            var existing = await _context.LifeEvents
                .FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId);

            if (existing == null)
                return false;

            _context.LifeEvents.Remove(existing);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted life event {EventId} for user {UserId}", eventId, userId);
            return true;
        }

        public async Task<List<LifeEvent>> GetTimelineAsync(Guid userId, DateTime? start, DateTime? end)
        {
            var query = _context.LifeEvents.Where(e => e.UserId == userId);

            if (start.HasValue)
                query = query.Where(e => e.EventDate >= start.Value);
            if (end.HasValue)
                query = query.Where(e => e.EventDate <= end.Value);

            return await query
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<List<LifeEvent>> GetUpcomingEventsAsync(Guid userId, int daysAhead = 30)
        {
            // Find recurring events whose month/day falls within the next N days
            var today = DateTime.UtcNow.Date;
            var endDate = today.AddDays(daysAhead);

            var recurringEvents = await _context.LifeEvents
                .Where(e => e.UserId == userId && e.IsRecurring)
                .ToListAsync();

            var upcoming = new List<LifeEvent>();

            foreach (var evt in recurringEvents)
            {
                // Check if the event's month/day falls within the window
                // Handle year boundary (e.g., checking Dec 20 - Jan 20)
                for (var date = today; date <= endDate; date = date.AddDays(1))
                {
                    if (evt.EventDate.Month == date.Month && evt.EventDate.Day == date.Day)
                    {
                        upcoming.Add(evt);
                        break;
                    }
                }
            }

            return upcoming.OrderBy(e => GetNextOccurrence(e.EventDate, today)).ToList();
        }

        public async Task<ConversationLifeContext> GetContextForConversationAsync(Guid userId)
        {
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);

            var recentEvents = await _context.LifeEvents
                .Where(e => e.UserId == userId && e.EventDate >= thirtyDaysAgo && e.EventDate <= now)
                .OrderByDescending(e => e.EventDate)
                .Take(10)
                .ToListAsync();

            var upcomingEvents = await GetUpcomingEventsAsync(userId, 30);
            var personalContext = await GetPersonalContextAsync(userId);

            return new ConversationLifeContext
            {
                RecentEvents = recentEvents,
                UpcomingEvents = upcomingEvents,
                PersonalContext = personalContext
            };
        }

        public async Task<PersonalContext> UpdatePersonalContextAsync(Guid userId, PersonalContext context)
        {
            var existing = await _context.PersonalContexts
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (existing == null)
            {
                context.UserId = userId;
                context.UpdatedAt = DateTime.UtcNow;
                _context.PersonalContexts.Add(context);
                await _context.SaveChangesAsync();
                return context;
            }

            existing.CulturalBackground = context.CulturalBackground;
            existing.CommunicationPreferences = context.CommunicationPreferences;
            existing.ImportantPeople = context.ImportantPeople;
            existing.Values = context.Values;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated personal context for user {UserId}", userId);
            return existing;
        }

        public async Task<PersonalContext?> GetPersonalContextAsync(Guid userId)
        {
            return await _context.PersonalContexts
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        private static DateTime GetNextOccurrence(DateTime eventDate, DateTime fromDate)
        {
            var thisYear = new DateTime(fromDate.Year, eventDate.Month, eventDate.Day);
            if (thisYear >= fromDate)
                return thisYear;
            return thisYear.AddYears(1);
        }
    }
}
