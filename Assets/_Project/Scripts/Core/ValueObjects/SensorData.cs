using System;

namespace DigitalTwin.Core.ValueObjects
{
    /// <summary>
    /// Sensor Reading Value Object
    /// 
    /// Architectural Intent:
    /// - Represents a single sensor data reading with metadata
    /// - Provides immutable sensor data for analysis
    /// - Encapsulates reading validation and quality assessment
    /// - Supports sensor data aggregation and trending
    /// </summary>
    public readonly struct SensorReading : IEquatable<SensorReading>
    {
        public Guid SensorId { get; }
        public SensorType SensorType { get; }
        public decimal Value { get; }
        public DateTime Timestamp { get; }
        public decimal Accuracy { get; } // Percentage
        public ReadingQuality Quality { get; }

        public SensorReading(Guid sensorId, SensorType sensorType, decimal value, 
                            DateTime timestamp, decimal accuracy)
        {
            if (sensorId == Guid.Empty)
                throw new ArgumentException("Sensor ID cannot be empty", nameof(sensorId));
            if (accuracy < 0 || accuracy > 100)
                throw new ArgumentException("Accuracy must be between 0 and 100%", nameof(accuracy));

            SensorId = sensorId;
            SensorType = sensorType;
            Value = value;
            Timestamp = timestamp;
            Accuracy = accuracy;
            Quality = CalculateQuality(accuracy);
        }

        public bool IsAccurate(decimal threshold = 80) 
            => Accuracy >= threshold;

        public bool IsRecent(TimeSpan threshold) 
            => DateTime.UtcNow - Timestamp <= threshold;

        public bool IsWithinRange(decimal minValue, decimal maxValue) 
            => Value >= minValue && Value <= maxValue;

        public SensorReading WithValue(decimal newValue) 
            => new SensorReading(SensorId, SensorType, newValue, DateTime.UtcNow, Accuracy);

        private static ReadingQuality CalculateQuality(decimal accuracy)
        {
            return accuracy switch
            {
                >= 95 => ReadingQuality.Excellent,
                >= 85 => ReadingQuality.Good,
                >= 70 => ReadingQuality.Fair,
                >= 50 => ReadingQuality.Poor,
                _ => ReadingQuality.Unreliable
            };
        }

        public bool Equals(SensorReading other) 
            => SensorId == other.SensorId && SensorType == other.SensorType && 
               Value == other.Value && Timestamp == other.Timestamp && Accuracy == other.Accuracy;

        public override bool Equals(object obj) 
            => obj is SensorReading other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(SensorId, SensorType, Value, Timestamp, Accuracy);

        public override string ToString() 
            => $"{SensorType}: {Value} (Accuracy: {Accuracy}%, Time: {Timestamp:HH:mm:ss})";
    }

    public enum ReadingQuality
    {
        Unreliable,
        Poor,
        Fair,
        Good,
        Excellent
    }

    /// <summary>
    /// Sensor Calibration Value Object
    /// 
    /// Architectural Intent:
    /// - Represents sensor calibration parameters and schedule
    /// - Provides immutable calibration data for accuracy management
    /// - Encapsulates calibration validation and tracking
    /// </summary>
    public readonly struct SensorCalibration : IEquatable<SensorCalibration>
    {
        public decimal Value { get; }
        public DateTime LastCalibrated { get; }
        public TimeSpan Interval { get; }
        public decimal Accuracy { get; } // Percentage

        public SensorCalibration(decimal value, DateTime lastCalibrated, TimeSpan interval, decimal accuracy = 100)
        {
            if (accuracy < 0 || accuracy > 100)
                throw new ArgumentException("Accuracy must be between 0 and 100%", nameof(accuracy));
            if (interval <= TimeSpan.Zero)
                throw new ArgumentException("Calibration interval must be positive", nameof(interval));

            Value = value;
            LastCalibrated = lastCalibrated;
            Interval = interval;
            Accuracy = accuracy;
        }

        public static SensorCalibration Default 
            => new SensorCalibration(1.0m, DateTime.UtcNow, TimeSpan.FromDays(30));

        public bool IsOverdue() 
            => DateTime.UtcNow - LastCalibrated > Interval;

        public TimeSpan GetTimeUntilNextCalibration() 
        => Interval - (DateTime.UtcNow - LastCalibrated);

        public SensorCalibration WithNewCalibration(decimal newValue, DateTime newDate) 
            => new SensorCalibration(newValue, newDate, Interval, 100);

        public bool Equals(SensorCalibration other) 
            => Value == other.Value && LastCalibrated == other.LastCalibrated && 
               Interval == other.Interval && Accuracy == other.Accuracy;

        public override bool Equals(object obj) 
            => obj is SensorCalibration other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Value, LastCalibrated, Interval, Accuracy);

        public override string ToString() 
            => $"Value: {Value}, Last: {LastCalibrated:yyyy-MM-dd}, Interval: {Interval.Days} days, Accuracy: {Accuracy}%";
    }

    /// <summary>
    /// Sensor Health Value Object
    /// 
    /// Architectural Intent:
    /// - Represents sensor health indicators and performance metrics
    /// - Provides immutable health data for sensor monitoring
    /// - Encapsulates health assessment and diagnostic information
    /// </summary>
    public readonly struct SensorHealth : IEquatable<SensorHealth>
    {
        public decimal Accuracy { get; } // Percentage
        public decimal SignalStrength { get; } // Percentage
        public int ErrorCount { get; }
        public TimeSpan Uptime { get; }
        public DateTime LastCheck { get; }

        public SensorHealth(decimal accuracy, decimal signalStrength, int errorCount, TimeSpan uptime, DateTime lastCheck)
        {
            if (accuracy < 0 || accuracy > 100)
                throw new ArgumentException("Accuracy must be between 0 and 100%", nameof(accuracy));
            if (signalStrength < 0 || signalStrength > 100)
                throw new ArgumentException("Signal strength must be between 0 and 100%", nameof(signalStrength));
            if (errorCount < 0)
                throw new ArgumentException("Error count cannot be negative", nameof(errorCount));

            Accuracy = accuracy;
            SignalStrength = signalStrength;
            ErrorCount = errorCount;
            Uptime = uptime;
            LastCheck = lastCheck;
        }

        public static SensorHealth Default 
            => new SensorHealth(95, 85, 0, TimeSpan.FromDays(30), DateTime.UtcNow);

        public HealthStatus GetStatus()
        {
            var healthScore = (Accuracy * 0.4m) + (SignalStrength * 0.3m) + ((100 - Math.Min(ErrorCount * 10, 100)) * 0.3m);

            return healthScore switch
            {
                >= 85 => HealthStatus.Excellent,
                >= 70 => HealthStatus.Good,
                >= 50 => HealthStatus.Fair,
                >= 30 => HealthStatus.Poor,
                _ => HealthStatus.Critical
            };
        }

        public bool IsHealthy() 
            => GetStatus() >= HealthStatus.Fair;

        public SensorHealth WithAccuracy(decimal newAccuracy) 
            => new SensorHealth(newAccuracy, SignalStrength, ErrorCount, Uptime, DateTime.UtcNow);

        public SensorHealth IncrementErrorCount() 
            => new SensorHealth(Accuracy, SignalStrength, ErrorCount + 1, Uptime, DateTime.UtcNow);

        public bool Equals(SensorHealth other) 
            => Accuracy == other.Accuracy && SignalStrength == other.SignalStrength && 
               ErrorCount == other.ErrorCount && Uptime == other.Uptime && LastCheck == other.LastCheck;

        public override bool Equals(object obj) 
            => obj is SensorHealth other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Accuracy, SignalStrength, ErrorCount, Uptime, LastCheck);

        public override string ToString() 
            => $"Accuracy: {Accuracy}%, Signal: {SignalStrength}%, Errors: {ErrorCount}, Status: {GetStatus()}";
    }

    public enum HealthStatus
    {
        Critical,
        Poor,
        Fair,
        Good,
        Excellent
    }
}