using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.Entities
{
    public class SharedRoom
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string CreatorUserId { get; set; } = string.Empty;
        public List<string> Participants { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
