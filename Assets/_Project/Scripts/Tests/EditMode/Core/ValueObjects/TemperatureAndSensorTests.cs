using NUnit.Framework;
using System;
using System.Linq;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Tests.EditMode.Core.ValueObjects
{
    /// <summary>
    /// Temperature Value Object Tests
    /// 
    /// Architectural Intent:
    /// - Tests all temperature conversion and comparison operations
    /// - Validates immutability and mathematical operations
    /// - Ensures proper unit conversion accuracy
    /// - Tests edge cases and boundary conditions
    /// </summary>
    [TestFixture]
    public class TemperatureTests
    {
        [Test]
        public void Constructor_WithValidCelsius_CreatesTemperature()
        {
            // Arrange & Act
            var temp = Temperature.FromCelsius(25.5m);

            // Assert
            Assert.That(temp.Celsius.Value, Is.EqualTo(25.5m));
            Assert.That(temp.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void Constructor_WithInvalidCelsius_ThrowsArgumentException()
        {
            // Below absolute zero
            Assert.Throws<ArgumentException>(() => 
                Temperature.FromCelsius(-300m));
        }

        [Test]
        public void Constructor_WithValidFahrenheit_CreatesTemperature()
        {
            // Arrange & Act
            var temp = Temperature.FromFahrenheit(77m);

            // Assert
            Assert.That(temp.Fahrenheit.Value, Is.EqualTo(77m));
            Assert.That(temp.Celsius.Value, Is.EqualTo(25m).Within(0.01m));
        }

        [Test]
        public void Constructor_WithValidKelvin_CreatesTemperature()
        {
            // Arrange & Act
            var temp = Temperature.FromKelvin(298.15m);

            // Assert
            Assert.That(temp.Kelvin.Value, Is.EqualTo(298.15m));
            Assert.That(temp.Celsius.Value, Is.EqualTo(25m).Within(0.01m));
        }

        [Test]
        public void ToCelsius_ConvertsCorrectly()
        {
            // Arrange
            var fahrenheitTemp = Temperature.FromFahrenheit(32m);
            var kelvinTemp = Temperature.FromKelvin(273.15m);

            // Act & Assert
            Assert.That(fahrenheitTemp.ToCelsius(), Is.EqualTo(0m).Within(0.01m));
            Assert.That(kelvinTemp.ToCelsius(), Is.EqualTo(0m).Within(0.01m));
        }

        [Test]
        public void ToFahrenheit_ConvertsCorrectly()
        {
            // Arrange
            var celsiusTemp = Temperature.FromCelsius(100m);

            // Act & Assert
            Assert.That(celsiusTemp.ToFahrenheit(), Is.EqualTo(212m).Within(0.01m));
        }

        [Test]
        public void ToKelvin_ConvertsCorrectly()
        {
            // Arrange
            var celsiusTemp = Temperature.FromCelsius(0m);

            // Act & Assert
            Assert.That(celsiusTemp.ToKelvin(), Is.EqualTo(273.15m).Within(0.01m));
        }

        [Test]
        public void Addition_WithSameUnits_ReturnsCorrectSum()
        {
            // Arrange
            var temp1 = Temperature.FromCelsius(20m);
            var temp2 = Temperature.FromCelsius(10m);

            // Act
            var result = temp1 + temp2;

            // Assert
            Assert.That(result.Celsius.Value, Is.EqualTo(30m));
            Assert.That(result.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void Addition_WithDifferentUnits_ConvertsAndAddsCorrectly()
        {
            // Arrange
            var celsiusTemp = Temperature.FromCelsius(20m);
            var fahrenheitTemp = Temperature.FromFahrenheit(50m); // 10°C

            // Act
            var result = celsiusTemp + fahrenheitTemp;

            // Assert
            Assert.That(result.Celsius.Value, Is.EqualTo(30m));
            Assert.That(result.Unit, Is.EqualTo(TemperatureUnit.Celsius));
        }

        [Test]
        public void Subtraction_ReturnsCorrectDifference()
        {
            // Arrange
            var temp1 = Temperature.FromCelsius(30m);
            var temp2 = Temperature.FromCelsius(10m);

            // Act
            var result = temp1 - temp2;

            // Assert
            Assert.That(result.Celsius.Value, Is.EqualTo(20m));
        }

        [Test]
        public void Multiplication_ReturnsCorrectProduct()
        {
            // Arrange
            var temp = Temperature.FromCelsius(10m);

            // Act
            var result = temp * 2;

            // Assert
            Assert.That(result.Celsius.Value, Is.EqualTo(20m));
        }

        [Test]
        public void Division_ReturnsCorrectQuotient()
        {
            // Arrange
            var temp = Temperature.FromCelsius(20m);

            // Act
            var result = temp / 2;

            // Assert
            Assert.That(result.Celsius.Value, Is.EqualTo(10m));
        }

        [Test]
        public void ComparisonOperators_WorkCorrectly()
        {
            // Arrange
            var temp1 = Temperature.FromCelsius(20m);
            var temp2 = Temperature.FromCelsius(30m);

            // Act & Assert
            Assert.That(temp1 < temp2, Is.True);
            Assert.That(temp1 <= temp2, Is.True);
            Assert.That(temp1 > temp2, Is.False);
            Assert.That(temp1 >= temp2, Is.False);
            Assert.That(temp1 == temp2, Is.False);
        }

        [Test]
        public void CompareTo_ReturnsCorrectOrdering()
        {
            // Arrange
            var temp1 = Temperature.FromCelsius(20m);
            var temp2 = Temperature.FromCelsius(30m);

            // Act & Assert
            Assert.That(temp1.CompareTo(temp2), Is.LessThan(0));
            Assert.That(temp2.CompareTo(temp1), Is.GreaterThan(0));
            Assert.That(temp1.CompareTo(temp1), Is.EqualTo(0));
        }

        [Test]
        public void IsWithinRange_ReturnsCorrectResult()
        {
            // Arrange
            var temp = Temperature.FromCelsius(22m);
            var min = Temperature.FromCelsius(20m);
            var max = Temperature.FromCelsius(25m);

            // Act & Assert
            Assert.That(temp.IsWithinRange(min, max), Is.True);
            Assert.That(min.IsWithinRange(temp, max), Is.False);
        }

        [Test]
        public void IsComfortable_ReturnsCorrectResult()
        {
            // Arrange
            var comfortableTemp = Temperature.FromCelsius(22m);
            var coldTemp = Temperature.FromCelsius(15m);
            var hotTemp = Temperature.FromCelsius(35m);

            // Act & Assert
            Assert.That(comfortableTemp.IsComfortable(), Is.True);
            Assert.That(coldTemp.IsComfortable(), Is.False);
            Assert.That(hotTemp.IsComfortable(), Is.False);
        }

        [Test]
        public void IsFreezing_ReturnsCorrectResult()
        {
            // Arrange
            var freezingTemp = Temperature.FromCelsius(0m);
            var aboveFreezingTemp = Temperature.FromCelsius(1m);

            // Act & Assert
            Assert.That(freezingTemp.IsFreezing(), Is.True);
            Assert.That(aboveFreezingTemp.IsFreezing(), Is.False);
        }

        [Test]
        public void IsBoiling_ReturnsCorrectResult()
        {
            // Arrange
            var boilingTemp = Temperature.FromCelsius(100m);
            var belowBoilingTemp = Temperature.FromCelsius(99m);

            // Act & Assert
            Assert.That(boilingTemp.IsBoiling(), Is.True);
            Assert.That(belowBoilingTemp.IsBoiling(), Is.False);
        }

        [Test]
        public void ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var celsiusTemp = Temperature.FromCelsius(22.5m);
            var fahrenheitTemp = Temperature.FromFahrenheit(72.5m);

            // Act & Assert
            Assert.That(celsiusTemp.ToString(), Is.EqualTo("22.5°C"));
            Assert.That(fahrenheitTemp.ToString(), Is.EqualTo("72.5°F"));
        }
    }

    /// <summary>
    /// Sensor Reading Value Object Tests
    /// 
    /// Architectural Intent:
    /// - Tests sensor reading creation and quality assessment
    /// - Validates type conversion methods
    /// - Tests quality scoring and metadata handling
    /// - Ensures proper data validation
    /// </summary>
    [TestFixture]
    public class SensorReadingTests
    {
        private Guid _testSensorId;
        private DateTime _testTimestamp;

        [SetUp]
        public void Setup()
        {
            _testSensorId = Guid.NewGuid();
            _testTimestamp = DateTime.UtcNow;
        }

        [Test]
        public void Constructor_WithValidParameters_CreatesSensorReading()
        {
            // Arrange & Act
            var reading = new SensorReading(_testSensorId, _testTimestamp, 25.5m, "°C");

            // Assert
            Assert.That(reading.SensorId, Is.EqualTo(_testSensorId));
            Assert.That(reading.Timestamp, Is.EqualTo(_testTimestamp));
            Assert.That(reading.Value, Is.EqualTo(25.5m));
            Assert.That(reading.Unit, Is.EqualTo("°C"));
            Assert.That(reading.Quality.Level, Is.EqualTo(QualityLevel.Unknown));
        }

        [Test]
        public void Constructor_WithEmptySensorId_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new SensorReading(Guid.Empty, _testTimestamp, 25.5m, "°C"));
        }

        [Test]
        public void Constructor_WithNullValue_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new SensorReading(_testSensorId, _testTimestamp, null, "°C"));
        }

        [Test]
        public void Constructor_WithEmptyUnit_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new SensorReading(_testSensorId, _testTimestamp, 25.5m, ""));
        }

        [Test]
        public void Constructor_WithFutureTimestamp_ThrowsArgumentException()
        {
            // Arrange
            var futureTimestamp = DateTime.UtcNow.AddMinutes(10);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new SensorReading(_testSensorId, futureTimestamp, 25.5m, "°C"));
        }

        [Test]
        public void CreateNumeric_CreatesNumericReading()
        {
            // Arrange & Act
            var reading = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 25.5m, "kWh");

            // Assert
            Assert.That(reading.Value, Is.EqualTo(25.5m));
            Assert.That(reading.Unit, Is.EqualTo("kWh"));
        }

        [Test]
        public void CreateTemperature_CreatesTemperatureReading()
        {
            // Arrange
            var temperature = Temperature.FromCelsius(22.5m);
            var quality = DataQuality.Good();

            // Act
            var reading = SensorReading.CreateTemperature(_testSensorId, _testTimestamp, temperature, quality);

            // Assert
            Assert.That(reading.Value, Is.EqualTo(temperature));
            Assert.That(reading.Unit, Is.EqualTo("Celsius"));
            Assert.That(reading.Quality, Is.EqualTo(quality));
        }

        [Test]
        public void CreateBoolean_CreatesBooleanReading()
        {
            // Arrange & Act
            var reading = SensorReading.CreateBoolean(_testSensorId, _testTimestamp, true);

            // Assert
            Assert.That(reading.Value, Is.EqualTo(true));
            Assert.That(reading.Unit, Is.EqualTo("boolean"));
        }

        [Test]
        public void TryGetNumericValue_WithDecimal_ReturnsTrueAndValue()
        {
            // Arrange
            var reading = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 25.5m, "kWh");

            // Act
            var result = reading.TryGetNumericValue(out var value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(25.5m));
        }

        [Test]
        public void TryGetNumericValue_WithInteger_ReturnsTrueAndValue()
        {
            // Arrange
            var reading = new SensorReading(_testSensorId, _testTimestamp, 42, "count");

            // Act
            var result = reading.TryGetNumericValue(out var value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(42m));
        }

        [Test]
        public void TryGetNumericValue_WithString_ReturnsFalse()
        {
            // Arrange
            var reading = SensorReading.CreateString(_testSensorId, _testTimestamp, "test value");

            // Act
            var result = reading.TryGetNumericValue(out var value);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryGetTemperatureValue_WithTemperature_ReturnsTrueAndValue()
        {
            // Arrange
            var temperature = Temperature.FromCelsius(22.5m);
            var reading = SensorReading.CreateTemperature(_testSensorId, _testTimestamp, temperature);

            // Act
            var result = reading.TryGetTemperatureValue(out var value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(temperature));
        }

        [Test]
        public void TryGetTemperatureValue_WithNonTemperature_ReturnsFalse()
        {
            // Arrange
            var reading = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 25.5m, "kWh");

            // Act
            var result = reading.TryGetTemperatureValue(out var value);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryGetBooleanValue_WithBoolean_ReturnsTrueAndValue()
        {
            // Arrange
            var reading = SensorReading.CreateBoolean(_testSensorId, _testTimestamp, true);

            // Act
            var result = reading.TryGetBooleanValue(out var value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(true));
        }

        [Test]
        public void TryGetBooleanValue_WithNonBoolean_ReturnsFalse()
        {
            // Arrange
            var reading = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 25.5m, "kWh");

            // Act
            var result = reading.TryGetBooleanValue(out var value);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryGetStringValue_WithString_ReturnsTrueAndValue()
        {
            // Arrange
            var reading = SensorReading.CreateString(_testSensorId, _testTimestamp, "test value");

            // Act
            var result = reading.TryGetStringValue(out var value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo("test value"));
        }

        [Test]
        public void TryGetStringValue_WithNonString_ReturnsFalse()
        {
            // Arrange
            var reading = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 25.5m, "kWh");

            // Act
            var result = reading.TryGetStringValue(out var value);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsWithinQualityThreshold_WithHigherThreshold_ReturnsTrue()
        {
            // Arrange
            var quality = DataQuality.Good();
            var reading = new SensorReading(_testSensorId, _testTimestamp, 25.5m, "°C", quality);
            var minimumQuality = DataQuality.Fair();

            // Act & Assert
            Assert.That(reading.IsWithinQualityThreshold(minimumQuality), Is.True);
        }

        [Test]
        public void IsWithinQualityThreshold_WithLowerThreshold_ReturnsFalse()
        {
            // Arrange
            var quality = DataQuality.Fair();
            var reading = new SensorReading(_testSensorId, _testTimestamp, 25.5m, "°C", quality);
            var minimumQuality = DataQuality.Good();

            // Act & Assert
            Assert.That(reading.IsWithinQualityThreshold(minimumQuality), Is.False);
        }

        [Test]
        public void WithQuality_ReturnsNewReadingWithNewQuality()
        {
            // Arrange
            var originalQuality = DataQuality.Unknown;
            var reading = new SensorReading(_testSensorId, _testTimestamp, 25.5m, "°C", originalQuality);
            var newQuality = DataQuality.Good("Test improvement");

            // Act
            var result = reading.WithQuality(newQuality);

            // Assert
            Assert.That(result.Quality, Is.EqualTo(newQuality));
            Assert.That(result.SensorId, Is.EqualTo(_testSensorId));
            Assert.That(result.Value, Is.EqualTo(25.5m));
        }

        [Test]
        public void WithMetadata_ReturnsNewReadingWithAdditionalMetadata()
        {
            // Arrange
            var reading = new SensorReading(_testSensorId, _testTimestamp, 25.5m, "°C");

            // Act
            var result = reading.WithMetadata("TestKey", "TestValue");

            // Assert
            Assert.That(result.Metadata.ContainsKey("TestKey"), Is.True);
            Assert.That(result.Metadata["TestKey"], Is.EqualTo("TestValue"));
        }

        [Test]
        public void Equality_WithSameValues_ReturnsTrue()
        {
            // Arrange
            var reading1 = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 25.5m, "°C");
            var reading2 = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 25.5m, "°C");

            // Act & Assert
            Assert.That(reading1.Equals(reading2), Is.True);
            Assert.That(reading1 == reading2, Is.True);
        }

        [Test]
        public void Equality_WithDifferentValues_ReturnsFalse()
        {
            // Arrange
            var reading1 = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 25.5m, "°C");
            var reading2 = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 26.5m, "°C");

            // Act & Assert
            Assert.That(reading1.Equals(reading2), Is.False);
            Assert.That(reading1 == reading2, Is.False);
        }

        [Test]
        public void GetHashCode_WithSameValues_ReturnsSameHashCode()
        {
            // Arrange
            var reading1 = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 25.5m, "°C");
            var reading2 = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 25.5m, "°C");

            // Act & Assert
            Assert.That(reading1.GetHashCode(), Is.EqualTo(reading2.GetHashCode()));
        }

        [Test]
        public void ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var reading = SensorReading.CreateNumeric(_testSensorId, _testTimestamp, 25.5m, "°C");

            // Act
            var result = reading.ToString();

            // Assert
            Assert.That(result, Does.Contain("Sensor:"));
            Assert.That(result, Does.Contain("Value: 25.5"));
            Assert.That(result, Does.Contain("°C"));
            Assert.That(result, Does.Contain("Quality: Unknown"));
        }
    }
}