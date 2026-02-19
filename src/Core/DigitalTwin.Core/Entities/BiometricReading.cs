using System;

namespace DigitalTwin.Core.Entities
{
    public class BiometricReading
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // heart_rate, hrv, steps, sleep_quality
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Source { get; set; } = "manual"; // apple_health, google_fit, manual
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
