using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.Models;

namespace DigitalTwin.Core.Security
{
    public class SecurityEventLogger
    {
        private readonly List<SecurityEvent> _events = new();
        private readonly object _lock = new();

        public async Task LogEventAsync(SecurityEvent securityEvent)
        {
            securityEvent.Timestamp = DateTime.UtcNow;

            lock (_lock)
            {
                _events.Add(securityEvent);

                // Keep only last 10000 events in memory
                if (_events.Count > 10000)
                {
                    _events.RemoveRange(0, _events.Count - 10000);
                }
            }

            await Task.CompletedTask;
        }

        public async Task<List<SecurityEvent>> GetSecurityEventsAsync(int page = 1, int pageSize = 50)
        {
            lock (_lock)
            {
                var events = _events
                    .OrderByDescending(e => e.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return events;
            }
        }

        public async Task<List<SecurityEvent>> GetSecurityEventsAsync()
        {
            lock (_lock)
            {
                return _events.OrderByDescending(e => e.Timestamp).ToList();
            }
        }
    }
}
