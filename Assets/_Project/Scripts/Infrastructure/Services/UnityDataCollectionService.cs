using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.ValueObjects;
using UnityEngine;

namespace DigitalTwin.Infrastructure.Services
{
    /// <summary>
    /// Unity-based Data Collection Service Implementation
    /// 
    /// Architectural Intent:
    /// - Implements real-time sensor data collection using Unity's coroutine system
    /// - Provides mock data generation for demonstration purposes
    /// - Supports configurable data quality and simulation modes
    /// - Enables integration with external IoT platforms via adapters
    /// 
    /// Key Design Decisions:
    /// 1. Uses Unity coroutines for real-time data streaming
    /// 2. Generates realistic mock data based on sensor types
    /// 3. Supports data quality simulation for testing scenarios
    /// 4. Event-driven architecture for real-time updates
    /// </summary>
    public class UnityDataCollectionService : MonoBehaviour, IDataCollectionService
    {
        [Header("Data Collection Configuration")]
        [SerializeField] private DataCollectionConfig _config;
        [SerializeField] private bool _enableMockDataGeneration = true;
        [SerializeField] private float _dataUpdateInterval = 5.0f;
        
        [Header("Quality Simulation")]
        [SerializeField] private DataQuality _defaultQuality = DataQuality.Good();
        [SerializeField] private float _errorRate = 0.05f; // 5% error rate for testing

        // Private fields
        private readonly Dictionary<Guid, Coroutine> _activeStreams = new Dictionary<Guid, Coroutine>();
        private readonly Dictionary<Guid, SensorProfile> _sensorProfiles = new Dictionary<Guid, SensorProfile>();
        private readonly Dictionary<Guid, DateTime> _lastDataTimes = new Dictionary<Guid, DateTime>();
        private readonly System.Random _random = new System.Random();

        // Events
        public event Action<SensorReading> SensorDataReceived;
        public event Action<Guid, SensorStatus> SensorStatusChanged;

        // Properties
        public bool IsCollecting => _activeStreams.Count > 0;

        private void Awake()
        {
            if (_config == null)
            {
                _config = CreateDefaultConfig();
            }

            InitializeSensorProfiles();
        }

        private void OnDestroy()
        {
            // Stop all active streams when service is destroyed
            var sensorIds = _activeStreams.Keys.ToList();
            foreach (var sensorId in sensorIds)
            {
                _ = StopDataStreamAsync(new[] { sensorId });
            }
        }

        public async Task<SensorReading> CollectSensorDataAsync(Guid sensorId)
        {
            try
            {
                var reading = await GenerateSensorReadingAsync(sensorId);
                SensorDataReceived?.Invoke(reading);
                return reading;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to collect data from sensor {sensorId}: {ex.Message}");
                return CreateErrorReading(sensorId);
            }
        }

        public async Task<IEnumerable<SensorReading>> CollectMultipleSensorDataAsync(IEnumerable<Guid> sensorIds)
        {
            var tasks = sensorIds.Select(CollectSensorDataAsync);
            var results = await Task.WhenAll(tasks);
            return results.Where(r => r != null);
        }

        public async Task<OperationalMetrics> CollectEquipmentMetricsAsync(Guid equipmentId)
        {
            // Mock implementation - in production this would connect to real equipment APIs
            await Task.Delay(100); // Simulate network latency

            var efficiency = 75 + (_random.NextDouble() * 20); // 75-95%
            var powerConsumption = 10 + (_random.NextDouble() * 50); // 10-60 kW
            var operatingHours = _random.Next(100, 10000);
            var cycleCount = _random.Next(0, 100000);
            var temperature = Temperature.FromCelsius(40 + (_random.NextDouble() * 40)); // 40-80°C

            return new OperationalMetrics(
                (decimal)efficiency,
                (decimal)powerConsumption,
                operatingHours,
                cycleCount,
                temperature,
                DateTime.UtcNow.AddDays(-_random.Next(1, 90)),
                new Dictionary<string, object>
                {
                    ["Vibration"] = _random.Next(0, 10),
                    ["Pressure"] = 100 + _random.NextDouble() * 50,
                    ["FlowRate"] = 50 + _random.NextDouble() * 100
                }
            );
        }

        public async Task<EnvironmentalConditions> CollectRoomConditionsAsync(Guid roomId)
        {
            // Mock implementation - in production this would aggregate data from room sensors
            await Task.Delay(50);

            var baseTemp = 20 + (_random.NextDouble() * 10); // 20-30°C
            var temperature = Temperature.FromCelsius((decimal)baseTemp);
            var humidity = 40 + (_random.NextDouble() * 30); // 40-70%
            var co2Level = 400 + (_random.NextDouble() * 600); // 400-1000 ppm
            var airQuality = _random.Next(0, 150); // AQI 0-150
            var lightLevel = 200 + (_random.NextDouble() * 800); // 200-1000 lux
            var noiseLevel = 30 + (_random.NextDouble() * 40); // 30-70 dB

            return new EnvironmentalConditions(
                temperature,
                (decimal)humidity,
                airQuality,
                (decimal)co2Level,
                (decimal)lightLevel,
                (decimal)noiseLevel,
                DateTime.UtcNow,
                new Dictionary<string, object>
                {
                    ["RoomId"] = roomId,
                    ["Occupancy"] = _random.Next(0, 50),
                    ["AirFlow"] = 100 + _random.NextDouble() * 200
                }
            );
        }

        public async Task StartDataStreamAsync(IEnumerable<Guid> sensorIds, Action<SensorReading> onDataReceived)
        {
            foreach (var sensorId in sensorIds)
            {
                if (!_activeStreams.ContainsKey(sensorId))
                {
                    // Subscribe to data received event if callback provided
                    if (onDataReceived != null)
                    {
                        SensorDataReceived += onDataReceived;
                    }

                    // Start coroutine for continuous data collection
                    var coroutine = StartCoroutine(DataStreamCoroutine(sensorId));
                    _activeStreams[sensorId] = coroutine;

                    // Update sensor status
                    SensorStatusChanged?.Invoke(sensorId, SensorStatus.Active);
                    _lastDataTimes[sensorId] = DateTime.UtcNow;
                }
            }

            await Task.CompletedTask;
        }

        public async Task StopDataStreamAsync(IEnumerable<Guid> sensorIds)
        {
            foreach (var sensorId in sensorIds)
            {
                if (_activeStreams.TryGetValue(sensorId, out var coroutine))
                {
                    StopCoroutine(coroutine);
                    _activeStreams.Remove(sensorId);
                    _lastDataTimes.Remove(sensorId);

                    // Update sensor status
                    SensorStatusChanged?.Invoke(sensorId, SensorStatus.Inactive);
                }
            }

            await Task.CompletedTask;
        }

        public async Task<DataQualityReport> ValidateDataQualityAsync(Guid sensorId, TimeSpan timeWindow)
        {
            // Mock implementation - in production this would analyze historical data
            await Task.Delay(100);

            var endTime = DateTime.UtcNow;
            var startTime = endTime - timeWindow;
            var expectedReadings = (int)(timeWindow.TotalSeconds / _dataUpdateInterval);
            var actualReadings = _random.Next((int)(expectedReadings * 0.8), expectedReadings);
            var validReadings = (int)(actualReadings * (0.9 + _random.NextDouble() * 0.1));

            var quality = validReadings > 0 ? DataQuality.Good($"Quality: {(decimal)validReadings / actualReadings:P2}") 
                                          : DataQuality.Bad("No valid readings");

            return new DataQualityReport
            {
                SensorId = sensorId,
                StartTime = startTime,
                EndTime = endTime,
                ExpectedReadings = expectedReadings,
                ActualReadings = actualReadings,
                ValidReadings = validReadings,
                OverallQuality = quality,
                Issues = validReadings < actualReadings ? new[] { "Some readings failed validation" } : new string[0]
            };
        }

        public async Task<IEnumerable<SensorReading>> GetHistoricalDataAsync(Guid sensorId, DateTime startTime, DateTime endTime)
        {
            // Mock implementation - in production this would query a database
            await Task.Delay(50);

            var readings = new List<SensorReading>();
            var currentTime = startTime;
            var interval = TimeSpan.FromMinutes(5);

            while (currentTime < endTime)
            {
                var reading = await GenerateSensorReadingAsync(sensorId, currentTime);
                readings.Add(reading);
                currentTime = currentTime.Add(interval);
            }

            return readings;
        }

        public async Task<bool> IsSensorOnlineAsync(Guid sensorId)
        {
            // Check if we have recent data from the sensor
            if (_lastDataTimes.TryGetValue(sensorId, out var lastDataTime))
            {
                var timeSinceLastData = DateTime.UtcNow - lastDataTime;
                return timeSinceLastData.TotalMinutes < _dataUpdateInterval * 2;
            }

            // For unknown sensors, randomly return true/false to simulate connectivity issues
            await Task.Delay(10);
            return _random.NextDouble() > _errorRate;
        }

        // Private methods
        private DataCollectionConfig CreateDefaultConfig()
        {
            return new DataCollectionConfig
            {
                DataUpdateInterval = _dataUpdateInterval,
                MaxRetries = 3,
                RetryDelay = 1.0f,
                EnableQualityValidation = true,
                QualityThreshold = 0.8m
            };
        }

        private void InitializeSensorProfiles()
        {
            // Create default sensor profiles for common sensor types
            // In production, these would be loaded from configuration or database

            // Temperature sensor profile
            _sensorProfiles[Guid.NewGuid()] = new SensorProfile
            {
                Type = "Temperature",
                Unit = "Celsius",
                MinValue = -20,
                MaxValue = 50,
                NormalRange = (18, 26),
                Variance = 2.0
            };

            // Humidity sensor profile
            _sensorProfiles[Guid.NewGuid()] = new SensorProfile
            {
                Type = "Humidity",
                Unit = "%",
                MinValue = 0,
                MaxValue = 100,
                NormalRange = (30, 60),
                Variance = 5.0
            };

            // Energy sensor profile
            _sensorProfiles[Guid.NewGuid()] = new SensorProfile
            {
                Type = "Energy",
                Unit = "kWh",
                MinValue = 0,
                MaxValue = 1000,
                NormalRange = (0, 100),
                Variance = 10.0
            };
        }

        private async Task<SensorReading> GenerateSensorReadingAsync(Guid sensorId, DateTime? timestamp = null)
        {
            await Task.Delay(10); // Simulate processing time

            var actualTimestamp = timestamp ?? DateTime.UtcNow;
            
            // Get or create sensor profile
            if (!_sensorProfiles.TryGetValue(sensorId, out var profile))
            {
                profile = CreateRandomSensorProfile();
                _sensorProfiles[sensorId] = profile;
            }

            // Generate reading based on sensor type
            var (value, quality) = GenerateValueBySensorType(profile);

            return new SensorReading(
                sensorId,
                actualTimestamp,
                value,
                profile.Unit,
                quality,
                new Dictionary<string, object>
                {
                    ["SensorType"] = profile.Type,
                    ["Location"] = "Room-A",
                    ["Source"] = "UnityMock"
                }
            );
        }

        private (object value, DataQuality quality) GenerateValueBySensorType(SensorProfile profile)
        {
            // Simulate data quality issues
            var hasQualityIssue = _random.NextDouble() < _errorRate;
            var quality = hasQualityIssue ? DataQuality.Poor("Simulated quality issue") : _defaultQuality;

            var (min, max) = profile.NormalRange;
            var center = (min + max) / 2;
            var variance = profile.Variance;

            object value = profile.Type switch
            {
                "Temperature" => Temperature.FromCelsius((decimal)(center + (_random.NextDouble() - 0.5) * variance * 2)),
                "Humidity" => Math.Max(0, Math.Min(100, center + (_random.NextDouble() - 0.5) * variance * 2)),
                "Energy" => Math.Max(0, center + (_random.NextDouble() - 0.5) * variance * 2),
                "Pressure" => center + (_random.NextDouble() - 0.5) * variance * 2,
                "Flow" => Math.Max(0, center + (_random.NextDouble() - 0.5) * variance * 2),
                _ => _random.NextDouble() * 100
            };

            return (value, quality);
        }

        private SensorProfile CreateRandomSensorProfile()
        {
            var types = new[] { "Temperature", "Humidity", "Energy", "Pressure", "Flow" };
            var type = types[_random.Next(types.Length)];

            return new SensorProfile
            {
                Type = type,
                Unit = GetDefaultUnit(type),
                MinValue = 0,
                MaxValue = 100,
                NormalRange = (20, 80),
                Variance = 5.0
            };
        }

        private string GetDefaultUnit(string sensorType)
        {
            return sensorType switch
            {
                "Temperature" => "Celsius",
                "Humidity" => "%",
                "Energy" => "kWh",
                "Pressure" => "Pa",
                "Flow" => "L/min",
                _ => "Unit"
            };
        }

        private SensorReading CreateErrorReading(Guid sensorId)
        {
            return new SensorReading(
                sensorId,
                DateTime.UtcNow,
                null,
                "Error",
                DataQuality.Bad("Collection failed"),
                new Dictionary<string, object>
                {
                    ["Error"] = "Data collection failed",
                    ["Timestamp"] = DateTime.UtcNow
                }
            );
        }

        private System.Collections.IEnumerator DataStreamCoroutine(Guid sensorId)
        {
            while (true)
            {
                try
                {
                    var reading = GenerateSensorReadingAsync(sensorId).Result;
                    _lastDataTimes[sensorId] = DateTime.UtcNow;
                    SensorDataReceived?.Invoke(reading);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in data stream for sensor {sensorId}: {ex.Message}");
                    SensorStatusChanged?.Invoke(sensorId, SensorStatus.Error);
                }

                yield return new WaitForSeconds(_dataUpdateInterval);
            }
        }
    }

    // Supporting classes
    [Serializable]
    public class DataCollectionConfig
    {
        public float DataUpdateInterval = 5.0f;
        public int MaxRetries = 3;
        public float RetryDelay = 1.0f;
        public bool EnableQualityValidation = true;
        public decimal QualityThreshold = 0.8m;
    }

    [Serializable]
    public class SensorProfile
    {
        public string Type;
        public string Unit;
        public double MinValue;
        public double MaxValue;
        public (double Min, double Max) NormalRange;
        public double Variance;
    }

    public class DataQualityReport
    {
        public Guid SensorId;
        public DateTime StartTime;
        public DateTime EndTime;
        public int ExpectedReadings;
        public int ActualReadings;
        public int ValidReadings;
        public DataQuality OverallQuality;
        public string[] Issues;
    }

    public enum SensorStatus
    {
        Active,
        Inactive,
        Error,
        Maintenance
    }
}