using System;

namespace DigitalTwin.Core.Entities
{
    public class DeviceToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty; // "ios", "android", "web"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
