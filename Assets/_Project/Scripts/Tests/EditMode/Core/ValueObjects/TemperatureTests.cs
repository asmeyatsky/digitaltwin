using System;
using NUnit.Framework;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Tests.EditMode.Core.ValueObjects
{
    /// <summary>
    /// Temperature Value Object Tests
    /// 
    /// Architectural Intent:
    /// - Tests temperature value object functionality
    /// - Validates unit conversions and calculations
    /// - Ensures immutability and value equality
    /// - Tests edge cases and boundary conditions
    /// 
    /// Key Testing Decisions:
    /// 1. Pure value object testing (no dependencies)
    /// 2. Comprehensive unit conversion coverage
    /// 3. Temperature comparison and arithmetic operations
    /// 4. Boundary condition testing
    /// </summary>
    [TestFixture]
    public class TemperatureTests
    {
        [Test]
        public void Constructor_WithValidCelsius_CreatesTemperature()
        {
            // Arrange
            var celsiusValue = 25m;

            // Act
            var temperature = Temperature.FromCelsius(celsiusValue);

            // Assert
            Assert.That(temperature.Value, Is.EqualTo(celsiusValue));
            Assert.That(temperature.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void Constructor_WithValidFahrenheit_CreatesTemperature()
        {
            // Arrange
            var fahrenheitValue = 77m;

            // Act
            var temperature = Temperature.FromFahrenheit(fahrenheitValue);

            // Assert
            Assert.That(temperature.Value, Is.EqualTo(fahrenheitValue));
            Assert.That(temperature.Unit, Is.EqualTo(TemperatureUnit.Fahrenheit));
        }

        [Test]
        public void Constructor_WithValidKelvin_CreatesTemperature()
        {
            // Arrange
            var kelvinValue = 298.15m;

            // Act
            var temperature = Temperature.FromKelvin(kelvinValue);

            // Assert
            Assert.That(temperature.Value, Is.EqualTo(kelvinValue));
            Assert.That(temperature.Unit, Is.EqualTo(TemperatureUnit.Kelvin));
        }

        [Test]
        public void Constructor_WithBelowAbsoluteZero_ThrowsArgumentException()
        {
            // Arrange
            var belowAbsoluteZero = -300m;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Temperature.FromCelsius(belowAbsoluteZero));
            Assert.Throws<ArgumentException>(() => Temperature.FromFahrenheit(belowAbsoluteZero));
            Assert.Throws<ArgumentException>(() => Temperature.FromKelvin(-1m));
        }

        [Test]
        public void ToCelsius_FromCelsius_ReturnsSameValue()
        {
            // Arrange
            var originalTemp = Temperature.FromCelsius(25m);

            // Act
            var converted = originalTemp.Celsius;

            // Assert
            Assert.That(converted.Value, Is.EqualTo(25m));
            Assert.That(converted.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void ToCelsius_FromFahrenheit_ConvertsCorrectly()
        {
            // Arrange
            var fahrenheitTemp = Temperature.FromFahrenheit(77m);

            // Act
            var celsiusTemp = fahrenheitTemp.Celsius;

            // Assert
            Assert.That(celsiusTemp.Value, Is.EqualTo(25m).Within(0.01m));
            Assert.That(celsiusTemp.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void ToCelsius_FromKelvin_ConvertsCorrectly()
        {
            // Arrange
            var kelvinTemp = Temperature.FromKelvin(298.15m);

            // Act
            var celsiusTemp = kelvinTemp.Celsius;

            // Assert
            Assert.That(celsiusTemp.Value, Is.EqualTo(25m).Within(0.01m));
            Assert.That(celsiusTemp.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void ToFahrenheit_FromCelsius_ConvertsCorrectly()
        {
            // Arrange
            var celsiusTemp = Temperature.FromCelsius(25m);

            // Act
            var fahrenheitTemp = celsiusTemp.Fahrenheit;

            // Assert
            Assert.That(fahrenheitTemp.Value, Is.EqualTo(77m).Within(0.01m));
            Assert.That(fahrenheitTemp.Unit, Is.EqualTo(TemperatureUnit.Fahrenheit));
        }

        [Test]
        public void ToFahrenheit_FromFahrenheit_ReturnsSameValue()
        {
            // Arrange
            var originalTemp = Temperature.FromFahrenheit(77m);

            // Act
            var converted = originalTemp.Fahrenheit;

            // Assert
            Assert.That(converted.Value, Is.EqualTo(77m));
            Assert.That(converted.Unit, Is.EqualTo(TemperatureUnit.Fahrenheit));
        }

        [Test]
        public void ToFahrenheit_FromKelvin_ConvertsCorrectly()
        {
            // Arrange
            var kelvinTemp = Temperature.FromKelvin(298.15m);

            // Act
            var fahrenheitTemp = kelvinTemp.Fahrenheit;

            // Assert
            Assert.That(fahrenheitTemp.Value, Is.EqualTo(77m).Within(0.01m));
            Assert.That(fahrenheitTemp.Unit, Is.EqualTo(TemperatureUnit.Fahrenheit));
        }

        [Test]
        public void ToKelvin_FromCelsius_ConvertsCorrectly()
        {
            // Arrange
            var celsiusTemp = Temperature.FromCelsius(25m);

            // Act
            var kelvinTemp = celsiusTemp.Kelvin;

            // Assert
            Assert.That(kelvinTemp.Value, Is.EqualTo(298.15m).Within(0.01m));
            Assert.That(kelvinTemp.Unit, Is.EqualTo(TemperatureUnit.Kelvin));
        }

        [Test]
        public void ToKelvin_FromFahrenheit_ConvertsCorrectly()
        {
            // Arrange
            var fahrenheitTemp = Temperature.FromFahrenheit(77m);

            // Act
            var kelvinTemp = fahrenheitTemp.Kelvin;

            // Assert
            Assert.That(kelvinTemp.Value, Is.EqualTo(298.15m).Within(0.01m));
            Assert.That(kelvinTemp.Unit, Is.EqualTo(TemperatureUnit.Kelvin));
        }

        [Test]
        public void ToKelvin_FromKelvin_ReturnsSameValue()
        {
            // Arrange
            var originalTemp = Temperature.FromKelvin(298.15m);

            // Act
            var converted = originalTemp.Kelvin;

            // Assert
            Assert.That(converted.Value, Is.EqualTo(298.15m));
            Assert.That(converted.Unit, Is.EqualTo(TemperatureUnit.Kelvin));
        }

        [Test]
        public void Add_WithSameUnit_ReturnsCorrectSum()
        {
            // Arrange
            var temp1 = Temperature.FromCelsius(20m);
            var temp2 = Temperature.FromCelsius(10m);

            // Act
            var result = temp1 + temp2;

            // Assert
            Assert.That(result.Value, Is.EqualTo(30m));
            Assert.That(result.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void Add_WithDifferentUnits_ConvertsAndReturnsCorrectSum()
        {
            // Arrange
            var celsiusTemp = Temperature.FromCelsius(20m);
            var fahrenheitTemp = Temperature.FromFahrenheit(50m); // ~10°C

            // Act
            var result = celsiusTemp + fahrenheitTemp;

            // Assert
            Assert.That(result.Value, Is.EqualTo(30m).Within(0.01m));
            Assert.That(result.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void Subtract_WithSameUnit_ReturnsCorrectDifference()
        {
            // Arrange
            var temp1 = Temperature.FromCelsius(30m);
            var temp2 = Temperature.FromCelsius(10m);

            // Act
            var result = temp1 - temp2;

            // Assert
            Assert.That(result.Value, Is.EqualTo(20m));
            Assert.That(result.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void Subtract_WithDifferentUnits_ConvertsAndReturnsCorrectDifference()
        {
            // Arrange
            var celsiusTemp = Temperature.FromCelsius(30m);
            var fahrenheitTemp = Temperature.FromFahrenheit(86m); // ~30°C

            // Act
            var result = celsiusTemp - fahrenheitTemp;

            // Assert
            Assert.That(result.Value, Is.EqualTo(0m).Within(0.01m));
            Assert.That(result.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void Multiply_WithFactor_ReturnsCorrectProduct()
        {
            // Arrange
            var temp = Temperature.FromCelsius(20m);
            var factor = 2m;

            // Act
            var result = temp * factor;

            // Assert
            Assert.That(result.Value, Is.EqualTo(40m));
            Assert.That(result.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void Divide_WithDivisor_ReturnsCorrectQuotient()
        {
            // Arrange
            var temp = Temperature.FromCelsius(40m);
            var divisor = 2m;

            // Act
            var result = temp / divisor;

            // Assert
            Assert.That(result.Value, Is.EqualTo(20m));
            Assert.That(result.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void ComparisonOperators_CompareCorrectly()
        {
            // Arrange
            var temp1 = Temperature.FromCelsius(20m);
            var temp2 = Temperature.FromCelsius(25m);
            var temp3 = Temperature.FromCelsius(25m);

            // Act & Assert
            Assert.That(temp1 < temp2, Is.True);
            Assert.That(temp2 > temp1, Is.True);
            Assert.That(temp1 <= temp2, Is.True);
            Assert.That(temp2 >= temp1, Is.True);
            Assert.That(temp2 == temp3, Is.True);
            Assert.That(temp1 != temp2, Is.True);
        }

        [Test]
        public void CompareTo_ComparesCorrectly()
        {
            // Arrange
            var temp1 = Temperature.FromCelsius(20m);
            var temp2 = Temperature.FromCelsius(25m);

            // Act
            var result = temp1.CompareTo(temp2);

            // Assert
            Assert.That(result, Is.LessThan(0));
        }

        [Test]
        public void IsWithinRange_WithValidRange_ReturnsTrue()
        {
            // Arrange
            var temp = Temperature.FromCelsius(22m);
            var min = Temperature.FromCelsius(20m);
            var max = Temperature.FromCelsius(25m);

            // Act
            var result = temp.IsWithinRange(min, max);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsWithinRange_WithInvalidRange_ReturnsFalse()
        {
            // Arrange
            var temp = Temperature.FromCelsius(26m);
            var min = Temperature.FromCelsius(20m);
            var max = Temperature.FromCelsius(25m);

            // Act
            var result = temp.IsWithinRange(min, max);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsComfortable_WithComfortableRange_ReturnsTrue()
        {
            // Arrange
            var temp = Temperature.FromCelsius(22m);

            // Act
            var result = temp.IsComfortable();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsComfortable_WithUncomfortableRange_ReturnsFalse()
        {
            // Arrange
            var temp = Temperature.FromCelsius(30m);

            // Act
            var result = temp.IsComfortable();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsFreezing_WithFreezingTemperature_ReturnsTrue()
        {
            // Arrange
            var temp = Temperature.FromCelsius(-5m);

            // Act
            var result = temp.IsFreezing();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsFreezing_WithNonFreezingTemperature_ReturnsFalse()
        {
            // Arrange
            var temp = Temperature.FromCelsius(5m);

            // Act
            var result = temp.IsFreezing();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsBoiling_WithBoilingTemperature_ReturnsTrue()
        {
            // Arrange
            var temp = Temperature.FromCelsius(100m);

            // Act
            var result = temp.IsBoiling();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsBoiling_WithNonBoilingTemperature_ReturnsFalse()
        {
            // Arrange
            var temp = Temperature.FromCelsius(95m);

            // Act
            var result = temp.IsBoiling();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void Equality_WithSameValueAndUnit_ReturnsTrue()
        {
            // Arrange
            var temp1 = Temperature.FromCelsius(25m);
            var temp2 = Temperature.FromCelsius(25m);

            // Act & Assert
            Assert.That(temp1.Equals(temp2), Is.True);
            Assert.That(temp1 == temp2, Is.True);
            Assert.That(temp1.GetHashCode(), Is.EqualTo(temp2.GetHashCode()));
        }

        [Test]
        public void Equality_WithDifferentValue_ReturnsFalse()
        {
            // Arrange
            var temp1 = Temperature.FromCelsius(25m);
            var temp2 = Temperature.FromCelsius(30m);

            // Act & Assert
            Assert.That(temp1.Equals(temp2), Is.False);
            Assert.That(temp1 != temp2, Is.True);
        }

        [Test]
        public void Equality_WithDifferentUnitButSameTemperature_ReturnsTrue()
        {
            // Arrange
            var celsiusTemp = Temperature.FromCelsius(25m);
            var fahrenheitTemp = Temperature.FromFahrenheit(77m); // 25°C

            // Act & Assert
            Assert.That(celsiusTemp.Equals(fahrenheitTemp), Is.False); // Different units
            Assert.That(celsiusTemp.Value != fahrenheitTemp.Value, Is.True); // Different numerical values
        }

        [Test]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var temp = Temperature.FromCelsius(25.5m);

            // Act
            var result = temp.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("25.5°C"));
        }

        // Edge Cases and Boundary Tests
        [Test]
        public void Constructor_WithAbsoluteZero_CreatesTemperature()
        {
            // Arrange & Act
            var celsiusTemp = Temperature.FromCelsius(-273.15m);
            var kelvinTemp = Temperature.FromKelvin(0m);

            // Assert
            Assert.That(celsiusTemp.Value, Is.EqualTo(-273.15m));
            Assert.That(kelvinTemp.Value, Is.EqualTo(0m));
        }

        [Test]
        public void Conversion_RoundTrip_ConsistencyTest()
        {
            // Arrange
            var originalTemp = Temperature.FromCelsius(25.7m);

            // Act
            var fahrenheitTemp = originalTemp.Fahrenheit;
            var backToCelsius = fahrenheitTemp.Celsius;
            var kelvinTemp = originalTemp.Kelvin;
            var backToCelsius2 = kelvinTemp.Celsius;

            // Assert
            Assert.That(backToCelsius.Value, Is.EqualTo(25.7m).Within(0.01m));
            Assert.That(backToCelsius2.Value, Is.EqualTo(25.7m).Within(0.01m));
        }

        [Test]
        public void ExtremeTemperatures_ConversionsWorkCorrectly()
        {
            // Arrange
            var extremeCelsius = Temperature.FromCelsius(-50m);
            var boilingCelsius = Temperature.FromCelsius(100m);

            // Act
            var extremeKelvin = extremeCelsius.Kelvin;
            var boilingKelvin = boilingCelsius.Kelvin;

            // Assert
            Assert.That(extremeKelvin.Value, Is.EqualTo(223.15m).Within(0.01m));
            Assert.That(boilingKelvin.Value, Is.EqualTo(373.15m).Within(0.01m));
        }
    }
}