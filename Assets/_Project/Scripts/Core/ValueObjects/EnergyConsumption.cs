using System;

namespace DigitalTwin.Core.ValueObjects
{
    /// <summary>
    /// Energy Consumption Value Object
    /// 
    /// Architectural Intent:
    /// - Represents energy consumption measurements with unit conversion
    /// - Provides immutable energy calculations and cost estimations
    /// - Encapsulates energy validation and efficiency metrics
    /// - Supports multiple energy units and time-based calculations
    /// 
    /// Invariants:
    /// - Energy consumption cannot be negative
    /// - Power values must be finite
    /// - Cost calculations must use positive rates
    /// - Time periods must be positive
    /// </summary>
    public readonly struct EnergyConsumption : IEquatable<EnergyConsumption>, IComparable<EnergyConsumption>
    {
        public decimal Value { get; }
        public EnergyUnit Unit { get; }
        public TimeSpan TimePeriod { get; }

        public EnergyConsumption(decimal value, EnergyUnit unit, TimeSpan timePeriod)
        {
            if (value < 0)
                throw new ArgumentException("Energy consumption cannot be negative", nameof(value));
            if (timePeriod <= TimeSpan.Zero)
                throw new ArgumentException("Time period must be positive", nameof(timePeriod));

            Value = value;
            Unit = unit;
            TimePeriod = timePeriod;
        }

        public static EnergyConsumption FromWattHours(decimal wattHours, TimeSpan period) 
            => new EnergyConsumption(wattHours, EnergyUnit.WattHour, period);

        public static EnergyConsumption FromKilowattHours(decimal kWh, TimeSpan period) 
            => new EnergyConsumption(kWh, EnergyUnit.KilowattHour, period);

        public static EnergyConsumption FromJoules(decimal joules, TimeSpan period) 
            => new EnergyConsumption(joules, EnergyUnit.Joule, period);

        public EnergyConsumption WattHours => Unit == EnergyUnit.WattHour ? this : ConvertTo(EnergyUnit.WattHour);
        public EnergyConsumption KilowattHours => Unit == EnergyUnit.KilowattHour ? this : ConvertTo(EnergyUnit.KilowattHour);
        public EnergyConsumption Joules => Unit == EnergyUnit.Joule ? this : ConvertTo(EnergyUnit.Joule);

        public decimal Power => Value / (decimal)TimePeriod.TotalHours;

        public decimal ToWattHours()
        {
            return Unit switch
            {
                EnergyUnit.WattHour => Value,
                EnergyUnit.KilowattHour => Value * 1000m,
                EnergyUnit.Joule => Value / 3600m,
                _ => throw new NotSupportedException($"Energy unit {Unit} not supported")
            };
        }

        public decimal ToKilowattHours()
        {
            return Unit switch
            {
                EnergyUnit.WattHour => Value / 1000m,
                EnergyUnit.KilowattHour => Value,
                EnergyUnit.Joule => Value / 3600000m,
                _ => throw new NotSupportedException($"Energy unit {Unit} not supported")
            };
        }

        public decimal ToJoules()
        {
            return Unit switch
            {
                EnergyUnit.WattHour => Value * 3600m,
                EnergyUnit.KilowattHour => Value * 3600000m,
                EnergyUnit.Joule => Value,
                _ => throw new NotSupportedException($"Energy unit {Unit} not supported")
            };
        }

        public EnergyConsumption Add(EnergyConsumption other)
        {
            var normalizedOther = other.ConvertTo(Unit);
            return new EnergyConsumption(Value + normalizedOther.Value, Unit, TimePeriod);
        }

        public EnergyConsumption Subtract(EnergyConsumption other)
        {
            var normalizedOther = other.ConvertTo(Unit);
            return new EnergyConsumption(Value - normalizedOther.Value, Unit, TimePeriod);
        }

        public EnergyConsumption Multiply(decimal factor)
            => new EnergyConsumption(Value * factor, Unit, TimePeriod);

        public EnergyConsumption Divide(decimal divisor)
            => new EnergyConsumption(Value / divisor, Unit, TimePeriod);

        public EnergyConsumption ScaleTo(TimeSpan newTimePeriod)
        {
            var scaleFactor = (decimal)newTimePeriod.TotalHours / (decimal)TimePeriod.TotalHours;
            return new EnergyConsumption(Value * scaleFactor, Unit, newTimePeriod);
        }

        public decimal CalculateCost(decimal ratePerKWh)
        {
            var kWh = ToKilowattHours();
            return kWh * ratePerKWh;
        }

        public decimal CalculateCarbonFootprint(decimal kgCO2PerKWh = 0.23m)
        {
            var kWh = ToKilowattHours();
            return kWh * kgCO2PerKWh;
        }

        public EfficiencyRating GetEfficiencyRating(decimal baselineConsumption)
        {
            var efficiency = baselineConsumption > 0 ? Value / baselineConsumption : 0;
            
            return efficiency switch
            {
                <= 0.5m => EfficiencyRating.Excellent,
                <= 0.7m => EfficiencyRating.Good,
                <= 0.9m => EfficiencyRating.Fair,
                <= 1.1m => EfficiencyRating.Average,
                <= 1.3m => EfficiencyRating.Poor,
                _ => EfficiencyRating.VeryPoor
            };
        }

        public bool IsWithinEfficientRange(decimal minConsumption, decimal maxConsumption)
        {
            return Value >= minConsumption && Value <= maxConsumption;
        }

        private EnergyConsumption ConvertTo(EnergyUnit targetUnit)
        {
            var convertedValue = targetUnit switch
            {
                EnergyUnit.WattHour => ToWattHours(),
                EnergyUnit.KilowattHour => ToKilowattHours(),
                EnergyUnit.Joule => ToJoules(),
                _ => throw new NotSupportedException($"Energy unit {targetUnit} not supported")
            };

            return new EnergyConsumption(convertedValue, targetUnit, TimePeriod);
        }

        public int CompareTo(EnergyConsumption other)
        {
            var normalizedOther = other.ConvertTo(Unit);
            return Value.CompareTo(normalizedOther.Value);
        }

        public bool Equals(EnergyConsumption other) 
            => Value == other.Value && Unit == other.Unit && TimePeriod == other.TimePeriod;

        public override bool Equals(object obj) 
            => obj is EnergyConsumption other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Value, Unit, TimePeriod);

        public override string ToString() 
            => $"{Value} {GetUnitSymbol()} over {TimePeriod.TotalHours:F1}h";

        private string GetUnitSymbol()
        {
            return Unit switch
            {
                EnergyUnit.WattHour => "Wh",
                EnergyUnit.KilowattHour => "kWh",
                EnergyUnit.Joule => "J",
                _ => "?"
            };
        }
    }

    public enum EnergyUnit
    {
        WattHour,
        KilowattHour,
        Joule
    }

    public enum EfficiencyRating
    {
        Excellent,
        Good,
        Fair,
        Average,
        Poor,
        VeryPoor
    }
}