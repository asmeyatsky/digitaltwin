using System;

namespace DigitalTwin.Core.ValueObjects
{
    /// <summary>
    /// Temperature Value Object
    /// 
    /// Architectural Intent:
    /// - Represents temperature measurements with unit conversion
    /// - Provides immutable temperature calculations and comparisons
    /// - Encapsulates temperature validation and range checking
    /// - Supports multiple temperature scales and conversions
    /// 
    /// Invariants:
    /// - Temperature must be above absolute zero (-273.15°C)
    /// - Temperature must be finite (not NaN or Infinity)
    /// - Unit conversions must preserve physical meaning
    /// </summary>
    public readonly struct Temperature : IEquatable<Temperature>, IComparable<Temperature>
    {
        public decimal Value { get; }
        public TemperatureUnit Unit { get; }

        public Temperature(decimal value, TemperatureUnit unit)
        {
            if (!IsValidTemperature(value, unit))
                throw new ArgumentException($"Temperature {value} {unit} is below absolute zero");

            Value = value;
            Unit = unit;
        }

        public static Temperature FromCelsius(decimal celsius) => new Temperature(celsius, TemperatureUnit.Celsius);
        public static Temperature FromFahrenheit(decimal fahrenheit) => new Temperature(fahrenheit, TemperatureUnit.Fahrenheit);
        public static Temperature FromKelvin(decimal kelvin) => new Temperature(kelvin, TemperatureUnit.Kelvin);

        public Temperature Celsius => Unit == TemperatureUnit.Celsius ? this : new Temperature(ToCelsius(), TemperatureUnit.Celsius);
        public Temperature Fahrenheit => Unit == TemperatureUnit.Fahrenheit ? this : new Temperature(ToFahrenheit(), TemperatureUnit.Fahrenheit);
        public Temperature Kelvin => Unit == TemperatureUnit.Kelvin ? this : new Temperature(ToKelvin(), TemperatureUnit.Kelvin);

        public decimal ToCelsius()
        {
            return Unit switch
            {
                TemperatureUnit.Celsius => Value,
                TemperatureUnit.Fahrenheit => (Value - 32m) * 5m / 9m,
                TemperatureUnit.Kelvin => Value - 273.15m,
                _ => throw new NotSupportedException($"Temperature unit {Unit} not supported")
            };
        }

        public decimal ToFahrenheit()
        {
            return Unit switch
            {
                TemperatureUnit.Celsius => Value * 9m / 5m + 32m,
                TemperatureUnit.Fahrenheit => Value,
                TemperatureUnit.Kelvin => (Value - 273.15m) * 9m / 5m + 32m,
                _ => throw new NotSupportedException($"Temperature unit {Unit} not supported")
            };
        }

        public decimal ToKelvin()
        {
            return Unit switch
            {
                TemperatureUnit.Celsius => Value + 273.15m,
                TemperatureUnit.Fahrenheit => (Value - 32m) * 5m / 9m + 273.15m,
                TemperatureUnit.Kelvin => Value,
                _ => throw new NotSupportedException($"Temperature unit {Unit} not supported")
            };
        }

        public Temperature Add(decimal delta) => new Temperature(Value + delta, Unit);
        public Temperature Subtract(decimal delta) => new Temperature(Value - delta, Unit);
        public Temperature Multiply(decimal factor) => new Temperature(Value * factor, Unit);
        public Temperature Divide(decimal divisor) => new Temperature(Value / divisor, Unit);

        public static Temperature operator +(Temperature left, Temperature right)
        {
            if (left.Unit != right.Unit)
                right = right.ConvertTo(left.Unit);
            return new Temperature(left.Value + right.Value, left.Unit);
        }

        public static Temperature operator -(Temperature left, Temperature right)
        {
            if (left.Unit != right.Unit)
                right = right.ConvertTo(left.Unit);
            return new Temperature(left.Value - right.Value, left.Unit);
        }

        public static Temperature operator *(Temperature temperature, decimal factor)
            => new Temperature(temperature.Value * factor, temperature.Unit);

        public static Temperature operator /(Temperature temperature, decimal divisor)
            => new Temperature(temperature.Value / divisor, temperature.Unit);

        public static bool operator <(Temperature left, Temperature right)
            => left.CompareTo(right) < 0;

        public static bool operator <=(Temperature left, Temperature right)
            => left.CompareTo(right) <= 0;

        public static bool operator >(Temperature left, Temperature right)
            => left.CompareTo(right) > 0;

        public static bool operator >=(Temperature left, Temperature right)
            => left.CompareTo(right) >= 0;

        public int CompareTo(Temperature other)
        {
            if (Unit != other.Unit)
                other = other.ConvertTo(Unit);
            return Value.CompareTo(other.Value);
        }

        public bool IsWithinRange(Temperature min, Temperature max)
            => this >= min && this <= max;

        public bool IsComfortable()
        {
            var celsius = Celsius.Value;
            return celsius >= 20 && celsius <= 26;
        }

        public bool IsFreezing()
        {
            var celsius = Celsius.Value;
            return celsius <= 0;
        }

        public bool IsBoiling()
        {
            var celsius = Celsius.Value;
            return celsius >= 100;
        }

        private Temperature ConvertTo(TemperatureUnit targetUnit)
        {
            return targetUnit switch
            {
                TemperatureUnit.Celsius => FromCelsius(ToCelsius()),
                TemperatureUnit.Fahrenheit => FromFahrenheit(ToFahrenheit()),
                TemperatureUnit.Kelvin => FromKelvin(ToKelvin()),
                _ => throw new NotSupportedException($"Temperature unit {targetUnit} not supported")
            };
        }

        private static bool IsValidTemperature(decimal value, TemperatureUnit unit)
        {
            var celsius = unit switch
            {
                TemperatureUnit.Celsius => value,
                TemperatureUnit.Fahrenheit => (value - 32m) * 5m / 9m,
                TemperatureUnit.Kelvin => value - 273.15m,
                _ => throw new NotSupportedException($"Temperature unit {unit} not supported")
            };

            return celsius >= -273.15m && !decimal.IsNaN(celsius) && !decimal.IsInfinity(celsius);
        }

        public bool Equals(Temperature other) 
            => Value == other.Value && Unit == other.Unit;

        public override bool Equals(object obj) 
            => obj is Temperature other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Value, Unit);

        public override string ToString() 
            => $"{Value}°{GetUnitSymbol()}";

        private string GetUnitSymbol()
        {
            return Unit switch
            {
                TemperatureUnit.Celsius => "C",
                TemperatureUnit.Fahrenheit => "F",
                TemperatureUnit.Kelvin => "K",
                _ => "?"
            };
        }
    }

    public enum TemperatureUnit
    {
        Celsius,
        Fahrenheit,
        Kelvin
    }
}