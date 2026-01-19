using System;

namespace DigitalTwin.Core.Entities
{
    /// <summary>
    /// Sensor Entity
    /// 
    /// Architectural Intent:
    /// - Represents environmental monitoring sensors within rooms
    /// - Encapsulates sensor data collection, calibration, and health monitoring
    /// - Provides real-time data streaming and historical data access
    /// - Maintains sensor accuracy and reliability metrics
    /// 
    /// Invariants:
    /// - Sensor accuracy must be between 0 and 100 percent
    /// - Calibration interval cannot be negative
    /// - Sensor cannot provide data if in failed state
    /// - Reading frequency must be positive
    /// </summary>
    public class Sensor
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Model { get; }
        public string Manufacturer { get; }
        public SensorType Type { get; }
        public SensorStatus Status { get; private set; }
        public SensorCalibration Calibration { get; private set; }
        public SensorHealth Health { get; private set; }
        public SensorMetadata Metadata { get; }
        public DateTime InstalledDate { get; }
        public DateTime LastCalibrationDate { get; private set; }
        public TimeSpan ReadingFrequency { get; }

        public Sensor(Guid id, string name, string model, string manufacturer, SensorType type,
                     SensorMetadata metadata, DateTime installedDate, TimeSpan readingFrequency)
        {
            if (readingFrequency <= TimeSpan.Zero)
                throw new ArgumentException("Reading frequency must be positive", nameof(readingFrequency));

            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Manufacturer = manufacturer ?? throw new ArgumentNullException(nameof(manufacturer));
            Type = type;
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            InstalledDate = installedDate;
            LastCalibrationDate = installedDate;
            ReadingFrequency = readingFrequency;
            Status = SensorStatus.Operational;
            Calibration = SensorCalibration.Default;
            Health = SensorHealth.Default;
        }

        public Sensor SetStatus(SensorStatus newStatus)
        {
            var newSensor = CloneWithNewStatus(newStatus);
            newSensor.Status = newStatus;
            return newSensor;
        }

        public Sensor Calibrate(decimal calibrationValue, DateTime calibrationDate)
        {
            var newCalibration = new SensorCalibration(calibrationValue, calibrationDate, Calibration.Interval);
            var newSensor = CloneWithNewCalibration(newCalibration);
            newSensor.Calibration = newCalibration;
            newSensor.LastCalibrationDate = calibrationDate;
            return newSensor;
        }

        public Sensor UpdateHealth(SensorHealth health)
        {
            var newSensor = CloneWithNewHealth(health);
            newSensor.Health = health;
            return newSensor;
        }

        public SensorReading TakeReading()
        {
            if (Status != SensorStatus.Operational)
                throw new InvalidOperationException("Cannot take reading from non-operational sensor");

            var timestamp = DateTime.UtcNow;
            var rawValue = GenerateSimulatedReading();
            var calibratedValue = ApplyCalibration(rawValue);
            var accuracy = CalculateCurrentAccuracy();

            return new SensorReading(Id, Type, calibratedValue, timestamp, accuracy);
        }

        public bool RequiresCalibration()
        {
            return DateTime.UtcNow - LastCalibrationDate >= Calibration.Interval;
        }

        public bool IsHealthy()
        {
            return Status == SensorStatus.Operational && 
                   Health.Accuracy >= 50 && 
                   Health.SignalStrength >= 30;
        }

        public TimeSpan GetTimeSinceLastCalibration()
        {
            return DateTime.UtcNow - LastCalibrationDate;
        }

        public decimal GetExpectedAccuracy()
        {
            var timeSinceCalibration = GetTimeSinceLastCalibration();
            var degradationRate = 0.1m; // 0.1% accuracy loss per day
            var daysSinceCalibration = (decimal)timeSinceCalibration.TotalDays;
            
            var accuracy = Math.Max(0, Calibration.Accuracy - (degradationRate * daysSinceCalibration));
            return Math.Min(100, accuracy);
        }

        private decimal GenerateSimulatedReading()
        {
            var random = new Random();
            
            return Type switch
            {
                SensorType.Temperature => 20 + (decimal)(random.NextDouble() * 15), // 20-35°C
                SensorType.Humidity => 30 + (decimal)(random.NextDouble() * 40), // 30-70%
                SensorType.Motion => random.Next(0, 2), // 0 or 1
                SensorType.Light => 100 + (decimal)(random.NextDouble() * 900), // 100-1000 lux
                SensorType.Power => 0 + (decimal)(random.NextDouble() * 5000), // 0-5000W
                SensorType.AirQuality => random.Next(0, 101), // 0-100 AQI
                SensorType.Pressure => 980 + (decimal)(random.NextDouble() * 50), // 980-1030 hPa
                SensorType.Co2 => 400 + (decimal)(random.NextDouble() * 600), // 400-1000 ppm
                _ => 0
            };
        }

        private decimal ApplyCalibration(decimal rawValue)
        {
            return rawValue * Calibration.Value;
        }

        private decimal CalculateCurrentAccuracy()
        {
            var baseAccuracy = Calibration.Accuracy;
            var healthFactor = Health.Accuracy / 100m;
            var signalFactor = Health.SignalStrength / 100m;
            
            return baseAccuracy * healthFactor * signalFactor;
        }

        private Sensor CloneWithNewStatus(SensorStatus newStatus)
        {
            var clone = new Sensor(Id, Name, Model, Manufacturer, Type, Metadata, InstalledDate, ReadingFrequency);
            clone.Status = newStatus;
            clone.Calibration = Calibration;
            clone.Health = Health;
            clone.LastCalibrationDate = LastCalibrationDate;
            return clone;
        }

        private Sensor CloneWithNewCalibration(SensorCalibration calibration)
        {
            var clone = new Sensor(Id, Name, Model, Manufacturer, Type, Metadata, InstalledDate, ReadingFrequency);
            clone.Status = Status;
            clone.Calibration = calibration;
            clone.Health = Health;
            clone.LastCalibrationDate = LastCalibrationDate;
            return clone;
        }

        private Sensor CloneWithNewHealth(SensorHealth health)
        {
            var clone = new Sensor(Id, Name, Model, Manufacturer, Type, Metadata, InstalledDate, ReadingFrequency);
            clone.Status = Status;
            clone.Calibration = Calibration;
            clone.Health = health;
            clone.LastCalibrationDate = LastCalibrationDate;
            return clone;
        }
    }

    public enum SensorType
    {
        Temperature,
        Humidity,
        Motion,
        Light,
        Power,
        AirQuality,
        Pressure,
        Co2,
        Noise,
        Vibration,
        WaterFlow,
        Gas,
        Smoke,
        Custom
    }

    public enum SensorStatus
    {
        Operational,
        Calibration,
        Failed,
        Offline,
        Maintenance
    }
}