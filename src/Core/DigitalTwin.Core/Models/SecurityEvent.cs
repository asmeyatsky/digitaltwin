using System;
using DigitalTwin.Core.Enums;

namespace DigitalTwin.Core.Models
{
    public class SecurityEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public SecurityEventType EventType { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string RequestPath { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
