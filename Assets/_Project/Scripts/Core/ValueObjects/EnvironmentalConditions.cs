using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.ValueObjects
{
    /// <summary>
    /// Environmental Conditions Value Object
    /// 
    /// Architectural Intent:
    /// - Represents comprehensive environmental state of a room or zone
    /// - Provides standardized environmental measurements
    /// - Enables comfort assessment and environmental monitoring
    /// - Supports air quality and climate control systems integration
    /// 
    /// Invariants:
    /// - Temperature must be within physically possible range
    /// - Humidity must be between 0 and 100 percent
    /// - Air quality index must be between 0 and 500
    /// - CO2 levels must be non-negative
    /// </summary>
    public readonly struct EnvironmentalConditions : IEquatable<EnvironmentalConditions>
    {
        public Temperature Temperature { get; }
        public decimal Humidity { get; }
        public decimal AirQualityIndex { get; }
        public decimal CO2Level { get; }
        public decimal LightLevel { get; }
        public decimal NoiseLevel { get; }
        public DateTime Timestamp { get; }
        public Dictionary<string, object> AdditionalMetrics { get; }

        public EnvironmentalConditions(Temperature temperature, decimal humidity, decimal airQualityIndex,
            decimal co2Level, decimal lightLevel, decimal noiseLevel, DateTime timestamp,
            Dictionary<string, object> additionalMetrics = null)
        {
            if (humidity < 0 || humidity > 100)
                throw new ArgumentException("Humidity must be between 0 and 100 percent", nameof(humidity));
            
            if (airQualityIndex < 0 || airQualityIndex > 500)
                throw new ArgumentException("Air quality index must be between 0 and 500", nameof(airQualityIndex));
            
            if (co2Level < 0)
                throw new ArgumentException("CO2 level cannot be negative", nameof(co2Level));
            
            if (lightLevel < 0)
                throw new ArgumentException("Light level cannot be negative", nameof(lightLevel));
            
            if (noiseLevel < 0)
                throw new ArgumentException("Noise level cannot be negative", nameof(noiseLevel));

            Temperature = temperature;
            Humidity = humidity;
            AirQualityIndex = airQualityIndex;
            CO2Level = co2Level;
            LightLevel = lightLevel;
            NoiseLevel = noiseLevel;
            Timestamp = timestamp;
            AdditionalMetrics = additionalMetrics ?? new Dictionary<string, object>();
        }

        public static EnvironmentalConditions Default 
            => new EnvironmentalConditions(Temperature.FromCelsius(22), 45, 50, 400, 500, 40, DateTime.UtcNow);

        public bool IsComfortable
        {
            get
            {
                return Temperature.IsComfortable() &&
                       Humidity >= 30 && Humidity <= 60 &&
                       AirQualityIndex <= 50 &&
                       CO2Level <= 1000;
            }
        }

        public ComfortLevel GetComfortLevel()
        {
            var score = 0;
            if (Temperature.IsComfortable()) score++;
            if (Humidity >= 30 && Humidity <= 60) score++;
            if (AirQualityIndex <= 50) score++;
            if (CO2Level <= 1000) score++;
            if (LightLevel >= 300 && LightLevel <= 500) score++;
            if (NoiseLevel <= 45) score++;

            return score switch
            {
                6 => ComfortLevel.Excellent,
                5 => ComfortLevel.Good,
                4 => ComfortLevel.Fair,
                3 => ComfortLevel.Poor,
                _ => ComfortLevel.Unacceptable
            };
        }

        public EnvironmentalQuality GetAirQuality()
        {
            return AirQualityIndex switch
            {
                <= 50 => EnvironmentalQuality.Good,
                <= 100 => EnvironmentalQuality.Moderate,
                <= 150 => EnvironmentalQuality.UnhealthyForSensitive,
                <= 200 => EnvironmentalQuality.Unhealthy,
                <= 300 => EnvironmentalQuality.VeryUnhealthy,
                _ => EnvironmentalQuality.Hazardous
            };
        }

        public bool IsWithinComfortRange()
        {
            return GetComfortLevel() >= ComfortLevel.Fair;
        }

        public EnvironmentalConditions WithTemperature(Temperature newTemperature)
            => new EnvironmentalConditions(newTemperature, Humidity, AirQualityIndex, CO2Level, LightLevel, NoiseLevel, Timestamp, AdditionalMetrics);

        public EnvironmentalConditions WithHumidity(decimal newHumidity)
            => new EnvironmentalConditions(Temperature, newHumidity, AirQualityIndex, CO2Level, LightLevel, NoiseLevel, Timestamp, AdditionalMetrics);

        public EnvironmentalConditions WithUpdatedTimestamp()
            => new EnvironmentalConditions(Temperature, Humidity, AirQualityIndex, CO2Level, LightLevel, NoiseLevel, DateTime.UtcNow, AdditionalMetrics);

        public bool Equals(EnvironmentalConditions other)
            => Temperature.Equals(other.Temperature) &&
               Humidity == other.Humidity &&
               AirQualityIndex == other.AirQualityIndex &&
               CO2Level == other.CO2Level &&
               LightLevel == other.LightLevel &&
               NoiseLevel == other.NoiseLevel &&
               Timestamp == other.Timestamp;

        public override bool Equals(object obj) 
            => obj is EnvironmentalConditions other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Temperature, Humidity, AirQualityIndex, CO2Level, LightLevel, NoiseLevel, Timestamp);

        public override string ToString() 
            => $"Temp: {Temperature}, Humidity: {Humidity}%, AQI: {AirQualityIndex}, CO2: {CO2Level}ppm";
    }

    public enum ComfortLevel
    {
        Unacceptable,
        Poor,
        Fair,
        Good,
        Excellent
    }

    public enum EnvironmentalQuality
    {
        Good,
        Moderate,
        UnhealthyForSensitive,
        Unhealthy,
        VeryUnhealthy,
        Hazardous
    }

    /// <summary>
    /// Energy Consumption Value Object
    /// 
    /// Architectural Intent:
    /// - Represents energy usage patterns and metrics
    /// - Provides normalized energy measurements across different units
    /// - Enables cost calculation and carbon footprint analysis
    /// - Supports energy efficiency monitoring and optimization
    /// 
    /// Invariants:
    /// - Energy values must be non-negative
    /// - Cost must be non-negative
    /// - Power factor must be between 0 and 1
    /// </summary>
    public readonly struct EnergyConsumption : IEquatable<EnergyConsumption>
    {
        public decimal Amount { get; }
        public EnergyUnit Unit { get; }
        public decimal Cost { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
        public decimal PowerFactor { get; }
        public decimal CarbonFootprint { get; }
        public Dictionary<string, object> Metadata { get; }

        public EnergyConsumption(decimal amount, EnergyUnit unit, decimal cost, DateTime startTime, DateTime endTime,
            decimal powerFactor = 0.95m, decimal carbonFootprint = 0, Dictionary<string, object> metadata = null)
        {
            if (amount < 0)
                throw new ArgumentException("Energy amount cannot be negative", nameof(amount));
            
            if (cost < 0)
                throw new ArgumentException("Cost cannot be negative", nameof(cost));
            
            if (endTime < startTime)
                throw new ArgumentException("End time must be after start time", nameof(endTime));
            
            if (powerFactor < 0 || powerFactor > 1)
                throw new ArgumentException("Power factor must be between 0 and 1", nameof(powerFactor));

            Amount = amount;
            Unit = unit;
            Cost = cost;
            StartTime = startTime;
            EndTime = endTime;
            PowerFactor = powerFactor;
            CarbonFootprint = carbonFootprint;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        public TimeSpan Duration => EndTime - StartTime;
        public decimal HourlyAverage => Unit == EnergyUnit.kWh ? Amount / (decimal)Duration.TotalHours : Amount;
        public decimal AveragePower => Amount / (decimal)Duration.TotalHours; // kW

        public EnergyConsumption ConvertTo(EnergyUnit targetUnit)
        {
            var convertedAmount = ConvertAmount(Amount, Unit, targetUnit);
            return new EnergyConsumption(convertedAmount, targetUnit, Cost, StartTime, EndTime, PowerFactor, CarbonFootprint, Metadata);
        }

        private static decimal ConvertAmount(decimal amount, EnergyUnit fromUnit, EnergyUnit toUnit)
        {
            // Conversion to kWh as base unit
            var inKWh = fromUnit switch
            {
                EnergyUnit.kWh => amount,
                EnergyUnit.Wh => amount / 1000m,
                EnergyUnit.MWh => amount * 1000m,
                EnergyUnit.J => amount / 3600000m, // Joules to kWh
                EnergyUnit.BTU => amount * 0.000293071m, // BTU to kWh
                _ => throw new NotSupportedException($"Conversion from {fromUnit} not supported")
            };

            // Conversion from kWh to target unit
            return toUnit switch
            {
                EnergyUnit.kWh => inKWh,
                EnergyUnit.Wh => inKWh * 1000m,
                EnergyUnit.MWh => inKWh / 1000m,
                EnergyUnit.J => inKWh * 3600000m,
                EnergyUnit.BTU => inKWh / 0.000293071m,
                _ => throw new NotSupportedException($"Conversion to {toUnit} not supported")
            };
        }

        public bool Equals(EnergyConsumption other)
            => Amount == other.Amount &&
               Unit == other.Unit &&
               Cost == other.Cost &&
               StartTime == other.StartTime &&
               EndTime == other.EndTime &&
               PowerFactor == other.PowerFactor;

        public override bool Equals(object obj) 
            => obj is EnergyConsumption other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Amount, Unit, Cost, StartTime, EndTime, PowerFactor);

        public override string ToString() 
            => $"{Amount} {Unit}, Cost: ${Cost:F2}, Duration: {Duration.TotalHours:F1}h";
    }

    public enum EnergyUnit
    {
        Wh,
        kWh,
        MWh,
        J,
        BTU
    }
}