using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.ValueObjects
{
    /// <summary>
    /// Sensor Reading Value Object
    /// 
    /// Architectural Intent:
    /// - Represents immutable sensor data with quality indicators
    /// - Provides validation and quality assessment for sensor readings
    /// - Supports multiple data types and units
    /// - Enables timestamped data tracking for time series analysis
    /// 
    /// Invariants:
    /// - Reading timestamp cannot be in the future
    /// - Quality score must be between 0 and 1
    /// - Value must be finite (not NaN or Infinity)
    /// </summary>
    public readonly struct SensorReading : IEquatable<SensorReading>
    {
        public Guid SensorId { get; }
        public DateTime Timestamp { get; }
        public object Value { get; }
        public string Unit { get; }
        public DataQuality Quality { get; }
        public Dictionary<string, object> Metadata { get; }

        public SensorReading(Guid sensorId, DateTime timestamp, object value, string unit, DataQuality quality = default, Dictionary<string, object> metadata = null)
        {
            if (sensorId == Guid.Empty)
                throw new ArgumentException("Sensor ID cannot be empty", nameof(sensorId));
            
            if (timestamp > DateTime.UtcNow.AddMinutes(5)) // Allow 5 minutes for clock sync
                throw new ArgumentException("Reading timestamp cannot be significantly in the future", nameof(timestamp));
            
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            
            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("Unit cannot be null or empty", nameof(unit));

            SensorId = sensorId;
            Timestamp = timestamp;
            Value = value;
            Unit = unit;
            Quality = quality.IsValid ? quality : DataQuality.Unknown;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        public static SensorReading CreateNumeric(Guid sensorId, DateTime timestamp, decimal value, string unit, DataQuality quality = default)
            => new SensorReading(sensorId, timestamp, value, unit, quality);

        public static SensorReading CreateTemperature(Guid sensorId, DateTime timestamp, Temperature temperature, DataQuality quality = default)
            => new SensorReading(sensorId, timestamp, temperature, temperature.Unit.ToString(), quality);

        public static SensorReading CreateBoolean(Guid sensorId, DateTime timestamp, bool value, DataQuality quality = default)
            => new SensorReading(sensorId, timestamp, value, "boolean", quality);

        public static SensorReading CreateString(Guid sensorId, DateTime timestamp, string value, DataQuality quality = default)
            => new SensorReading(sensorId, timestamp, value, "text", quality);

        public bool TryGetNumericValue(out decimal numericValue)
        {
            if (Value is decimal decimalValue)
            {
                numericValue = decimalValue;
                return true;
            }
            
            if (Value is int intValue)
            {
                numericValue = intValue;
                return true;
            }
            
            if (Value is double doubleValue)
            {
                numericValue = (decimal)doubleValue;
                return true;
            }
            
            if (Value is float floatValue)
            {
                numericValue = (decimal)floatValue;
                return true;
            }

            numericValue = 0;
            return false;
        }

        public bool TryGetTemperatureValue(out Temperature temperature)
        {
            if (Value is Temperature temp)
            {
                temperature = temp;
                return true;
            }

            temperature = default;
            return false;
        }

        public bool TryGetBooleanValue(out bool boolValue)
        {
            if (Value is bool b)
            {
                boolValue = b;
                return true;
            }

            boolValue = false;
            return false;
        }

        public bool TryGetStringValue(out string stringValue)
        {
            if (Value is string s)
            {
                stringValue = s;
                return true;
            }

            stringValue = Value?.ToString();
            return stringValue != null;
        }

        public bool IsWithinQualityThreshold(DataQuality minimumQuality)
            => Quality.Score >= minimumQuality.Score;

        public SensorReading WithQuality(DataQuality newQuality)
            => new SensorReading(SensorId, Timestamp, Value, Unit, newQuality, Metadata);

        public SensorReading WithMetadata(string key, object value)
        {
            var newMetadata = new Dictionary<string, object>(Metadata) { [key] = value };
            return new SensorReading(SensorId, Timestamp, Value, Unit, Quality, newMetadata);
        }

        public bool Equals(SensorReading other)
            => SensorId == other.SensorId && 
               Timestamp == other.Timestamp && 
               EqualityComparer<object>.Default.Equals(Value, other.Value) && 
               Unit == other.Unit && 
               Quality.Equals(other.Quality);

        public override bool Equals(object obj) 
            => obj is SensorReading other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(SensorId, Timestamp, Value, Unit, Quality);

        public override string ToString() 
            => $"Sensor: {SensorId}, Value: {Value} {Unit}, Quality: {Quality}, Time: {Timestamp:yyyy-MM-dd HH:mm:ss}";
    }

    /// <summary>
    /// Data Quality Value Object
    /// 
    /// Architectural Intent:
    /// - Represents the quality and reliability of sensor data
    /// - Provides standardized quality assessment across different sensor types
    /// - Enables filtering of data based on quality thresholds
    /// - Supports confidence scoring for data validation
    /// 
    /// Invariants:
    /// - Quality score must be between 0 and 1 inclusive
    /// - Higher scores indicate better data quality
    /// - Quality levels must be consistent with score ranges
    /// </summary>
    public readonly struct DataQuality : IEquatable<DataQuality>, IComparable<DataQuality>
    {
        public decimal Score { get; }
        public QualityLevel Level { get; }
        public string Reason { get; }
        public bool IsValid => Score >= 0 && Score <= 1;

        private static readonly Dictionary<QualityLevel, (decimal Min, decimal Max)> QualityRanges = new()
        {
            [QualityLevel.Excellent] = (0.9m, 1.0m),
            [QualityLevel.Good] = (0.7m, 0.9m),
            [QualityLevel.Fair] = (0.5m, 0.7m),
            [QualityLevel.Poor] = (0.3m, 0.5m),
            [QualityLevel.Bad] = (0.1m, 0.3m),
            [QualityLevel.Unknown] = (0.0m, 0.1m)
        };

        public DataQuality(decimal score, QualityLevel level = QualityLevel.Unknown, string reason = null)
        {
            if (score < 0 || score > 1)
                throw new ArgumentException("Quality score must be between 0 and 1", nameof(score));

            Score = score;
            Level = level != QualityLevel.Unknown ? level : DetermineLevel(score);
            Reason = reason;
        }

        public static DataQuality Excellent(string reason = null) => new DataQuality(0.95m, QualityLevel.Excellent, reason);
        public static DataQuality Good(string reason = null) => new DataQuality(0.8m, QualityLevel.Good, reason);
        public static DataQuality Fair(string reason = null) => new DataQuality(0.6m, QualityLevel.Fair, reason);
        public static DataQuality Poor(string reason = null) => new DataQuality(0.4m, QualityLevel.Poor, reason);
        public static DataQuality Bad(string reason = null) => new DataQuality(0.2m, QualityLevel.Bad, reason);
        public static DataQuality Unknown(string reason = null) => new DataQuality(0.05m, QualityLevel.Unknown, reason);

        private static QualityLevel DetermineLevel(decimal score)
        {
            foreach (var (level, range) in QualityRanges)
            {
                if (score >= range.Min && score <= range.Max)
                {
                    return level;
                }
            }
            return QualityLevel.Unknown;
        }

        public DataQuality WithReason(string newReason)
            => new DataQuality(Score, Level, newReason);

        public DataQuality WithScore(decimal newScore)
            => new DataQuality(newScore, DetermineLevel(newScore), Reason);

        public int CompareTo(DataQuality other)
            => Score.CompareTo(other.Score);

        public bool IsAtLeast(QualityLevel minimumLevel)
            => Level >= minimumLevel;

        public bool Equals(DataQuality other)
            => Score == other.Score && Level == other.Level;

        public override bool Equals(object obj) 
            => obj is DataQuality other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Score, Level);

        public override string ToString() 
            => $"{Level} (Score: {Score:F2}" + (string.IsNullOrEmpty(Reason) ? ")" : $", Reason: {Reason})");
    }

    public enum QualityLevel
    {
        Unknown = 0,
        Bad = 1,
        Poor = 2,
        Fair = 3,
        Good = 4,
        Excellent = 5
    }

    /// <summary>
    /// Operational Metrics Value Object
    /// 
    /// Architectural Intent:
    /// - Represents comprehensive equipment performance metrics
    /// - Provides normalized metrics for different equipment types
    /// - Enables performance monitoring and trend analysis
    /// - Supports efficiency calculations and KPI tracking
    /// 
    /// Invariants:
    /// - Efficiency must be between 0 and 100 percent
    /// - Operating hours cannot be negative
    /// - Power consumption must be non-negative
    /// </summary>
    public readonly struct OperationalMetrics : IEquatable<OperationalMetrics>
    {
        public decimal Efficiency { get; }
        public decimal PowerConsumption { get; }
        public decimal OperatingHours { get; }
        public int CycleCount { get; }
        public Temperature OperatingTemperature { get; }
        public DateTime LastMaintenance { get; }
        public Dictionary<string, object> CustomMetrics { get; }

        public OperationalMetrics(decimal efficiency, decimal powerConsumption, decimal operatingHours, 
            int cycleCount, Temperature operatingTemperature, DateTime lastMaintenance, 
            Dictionary<string, object> customMetrics = null)
        {
            if (efficiency < 0 || efficiency > 100)
                throw new ArgumentException("Efficiency must be between 0 and 100", nameof(efficiency));
            
            if (powerConsumption < 0)
                throw new ArgumentException("Power consumption cannot be negative", nameof(powerConsumption));
            
            if (operatingHours < 0)
                throw new ArgumentException("Operating hours cannot be negative", nameof(operatingHours));
            
            if (cycleCount < 0)
                throw new ArgumentException("Cycle count cannot be negative", nameof(cycleCount));

            Efficiency = efficiency;
            PowerConsumption = powerConsumption;
            OperatingHours = operatingHours;
            CycleCount = cycleCount;
            OperatingTemperature = operatingTemperature;
            LastMaintenance = lastMaintenance;
            CustomMetrics = customMetrics ?? new Dictionary<string, object>();
        }

        public bool IsEfficient => Efficiency >= 75;
        public bool RequiresMaintenance => (DateTime.UtcNow - LastMaintenance).TotalDays > 30;
        public bool IsOperatingNormally => OperatingTemperature.IsComfortable() && Efficiency >= 50;

        public OperationalMetrics WithEfficiency(decimal newEfficiency)
            => new OperationalMetrics(newEfficiency, PowerConsumption, OperatingHours, CycleCount, OperatingTemperature, LastMaintenance, CustomMetrics);

        public OperationalMetrics WithPowerConsumption(decimal newPowerConsumption)
            => new OperationalMetrics(Efficiency, newPowerConsumption, OperatingHours, CycleCount, OperatingTemperature, LastMaintenance, CustomMetrics);

        public OperationalMetrics WithOperatingHours(decimal additionalHours)
            => new OperationalMetrics(Efficiency, PowerConsumption, OperatingHours + additionalHours, CycleCount, OperatingTemperature, LastMaintenance, CustomMetrics);

        public OperationalMetrics WithCycleIncrement()
            => new OperationalMetrics(Efficiency, PowerConsumption, OperatingHours, CycleCount + 1, OperatingTemperature, LastMaintenance, CustomMetrics);

        public OperationalMetrics WithMaintenance(DateTime maintenanceDate)
            => new OperationalMetrics(Efficiency, PowerConsumption, OperatingHours, CycleCount, OperatingTemperature, maintenanceDate, CustomMetrics);

        public bool Equals(OperationalMetrics other)
            => Efficiency == other.Efficiency && 
               PowerConsumption == other.PowerConsumption && 
               OperatingHours == other.OperatingHours && 
               CycleCount == other.CycleCount && 
               OperatingTemperature.Equals(other.OperatingTemperature) && 
               LastMaintenance == other.LastMaintenance;

        public override bool Equals(object obj) 
            => obj is OperationalMetrics other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Efficiency, PowerConsumption, OperatingHours, CycleCount, OperatingTemperature, LastMaintenance);

        public override string ToString() 
            => $"Efficiency: {Efficiency}%, Power: {PowerConsumption}kW, Hours: {OperatingHours}, Cycles: {CycleCount}";
    }
}