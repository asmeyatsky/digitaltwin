using System;

namespace DigitalTwin.Core.ValueObjects
{
    /// <summary>
    /// Operational Metrics Value Object
    /// 
    /// Architectural Intent:
    /// - Represents equipment performance and operational data
    /// - Provides immutable metrics for monitoring and analysis
    /// - Encapsulates efficiency calculations and performance tracking
    /// - Supports predictive maintenance indicators
    /// </summary>
    public readonly struct OperationalMetrics : IEquatable<OperationalMetrics>
    {
        public decimal Efficiency { get; } // Percentage
        public decimal Utilization { get; } // Percentage
        public decimal Uptime { get; } // Percentage
        public TimeSpan TotalOperatingTime { get; }
        public int ErrorCount { get; }
        public DateTime LastUpdated { get; }

        public OperationalMetrics(decimal efficiency, decimal utilization, decimal uptime, 
                                 TimeSpan totalOperatingTime, int errorCount, DateTime lastUpdated)
        {
            if (efficiency < 0 || efficiency > 100)
                throw new ArgumentException("Efficiency must be between 0 and 100%", nameof(efficiency));
            if (utilization < 0 || utilization > 100)
                throw new ArgumentException("Utilization must be between 0 and 100%", nameof(utilization));
            if (uptime < 0 || uptime > 100)
                throw new ArgumentException("Uptime must be between 0 and 100%", nameof(uptime));
            if (errorCount < 0)
                throw new ArgumentException("Error count cannot be negative", nameof(errorCount));

            Efficiency = efficiency;
            Utilization = utilization;
            Uptime = uptime;
            TotalOperatingTime = totalOperatingTime;
            ErrorCount = errorCount;
            LastUpdated = lastUpdated;
        }

        public static OperationalMetrics Default 
            => new OperationalMetrics(95, 80, 99, TimeSpan.Zero, 0, DateTime.UtcNow);

        public PerformanceRating GetPerformanceRating()
        {
            var weightedScore = (Efficiency * 0.4m) + (Utilization * 0.3m) + (Uptime * 0.3m);

            return weightedScore switch
            {
                >= 95 => PerformanceRating.Excellent,
                >= 85 => PerformanceRating.Good,
                >= 75 => PerformanceRating.Fair,
                >= 65 => PerformanceRating.Poor,
                _ => PerformanceRating.Critical
            };
        }

        public bool RequiresMaintenance()
        {
            return Efficiency < 70 || Uptime < 90 || ErrorCount > 5;
        }

        public bool IsOptimal()
        {
            return Efficiency >= 90 && Utilization >= 70 && Uptime >= 95;
        }

        public OperationalMetrics WithEfficiency(decimal newEfficiency)
            => new OperationalMetrics(newEfficiency, Utilization, Uptime, TotalOperatingTime, ErrorCount, DateTime.UtcNow);

        public OperationalMetrics WithUtilization(decimal newUtilization)
            => new OperationalMetrics(Efficiency, newUtilization, Uptime, TotalOperatingTime, ErrorCount, DateTime.UtcNow);

        public OperationalMetrics IncrementErrorCount()
            => new OperationalMetrics(Efficiency, Utilization, Uptime, TotalOperatingTime, ErrorCount + 1, DateTime.UtcNow);

        public OperationalMetrics UpdateOperatingTime(TimeSpan additionalTime)
            => new OperationalMetrics(Efficiency, Utilization, Uptime, TotalOperatingTime + additionalTime, ErrorCount, DateTime.UtcNow);

        public bool Equals(OperationalMetrics other) 
            => Efficiency == other.Efficiency && Utilization == other.Utilization && 
               Uptime == other.Uptime && TotalOperatingTime == other.TotalOperatingTime && 
               ErrorCount == other.ErrorCount && LastUpdated == other.LastUpdated;

        public override bool Equals(object obj) 
            => obj is OperationalMetrics other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Efficiency, Utilization, Uptime, TotalOperatingTime, ErrorCount, LastUpdated);

        public override string ToString() 
            => $"Efficiency: {Efficiency}%, Utilization: {Utilization}%, Uptime: {Uptime}%, Errors: {ErrorCount}";
    }

    public enum PerformanceRating
    {
        Critical,
        Poor,
        Fair,
        Good,
        Excellent
    }
}