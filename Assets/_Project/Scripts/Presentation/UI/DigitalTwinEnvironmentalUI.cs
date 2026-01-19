using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DigitalTwin.Core.ValueObjects;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Presentation.UI
{
    /// <summary>
    /// Environmental Conditions Display Component
    /// 
    /// Architectural Intent:
    /// - Displays real-time environmental data from sensors
    /// - Shows comfort levels and air quality information
    /// - Provides historical trends and comparisons
    /// - Enables environmental control adjustments
    /// </summary>
    public class DigitalTwinEnvironmentalDisplay : MonoBehaviour
    {
        [Header("Current Conditions")]
        [SerializeField] private TextMeshProUGUI _temperatureText;
        [SerializeField] private TextMeshProUGUI _humidityText;
        [SerializeField] private TextMeshProUGUI _airQualityText;
        [SerializeField] private TextMeshProUGUI _co2LevelText;
        [SerializeField] private TextMeshProUGUI _lightLevelText;
        [SerializeField] private TextMeshProUGUI _noiseLevelText;

        [Header("Comfort Indicators")]
        [SerializeField] private Image _comfortIndicator;
        [SerializeField] private TextMeshProUGUI _comfortLevelText;
        [SerializeField] private Slider _temperatureGauge;
        [SerializeField] private Slider _humidityGauge;
        [SerializeField] private Slider _airQualityGauge;

        [Header("Trend Display")]
        [SerializeField] private LineChart _temperatureTrendChart;
        [SerializeField] private LineChart _humidityTrendChart;
        [SerializeField] private LineChart _airQualityTrendChart;
        [SerializeField] private TMP_Dropdown _timeRangeDropdown;

        [Header("Zone Information")]
        [SerializeField] private Transform _zonesContainer;
        [SerializeField] private GameObject _zoneItemPrefab;

        [Header("Controls")]
        [SerializeField] private Button _adjustTargetButton;
        [SerializeField] private Button _runEnvironmentalSimulationButton;

        private DigitalTwinDashboardManager _dashboardManager;
        private ServiceLocator _serviceLocator;
        private Building _currentBuilding;
        private EnvironmentalConditions _currentConditions;
        private List<EnvironmentalConditions> _historicalData = new List<EnvironmentalConditions>();
        private List<ZoneInfo> _zoneInfo = new List<ZoneInfo>();

        public void Initialize(DigitalTwinDashboardManager dashboard, ServiceLocator serviceLocator)
        {
            _dashboardManager = dashboard;
            _serviceLocator = serviceLocator;

            SetupControls();
            SetupTimeRangeOptions();
            ResetDisplay();
        }

        public void SetBuilding(Building building)
        {
            _currentBuilding = building;
            InitializeZoneInfo(building);
            StartEnvironmentalMonitoring();
        }

        public void UpdateConditions(EnvironmentalConditions conditions)
        {
            _currentConditions = conditions;
            _historicalData.Add(conditions);

            // Keep only last 24 hours of data
            if (_historicalData.Count > 288) // 24 hours * 12 readings per hour
            {
                _historicalData.RemoveAt(0);
            }

            UpdateCurrentConditionsDisplay(conditions);
            UpdateComfortIndicators(conditions);
            UpdateTrendCharts();
            UpdateZoneStatuses(conditions);
        }

        public void UpdateSimulationResult(EnvironmentalSimulationResult result)
        {
            if (result.IsSuccess)
            {
                // Update with simulation data
                var avgConditions = result.AverageConditions;
                UpdateConditions(avgConditions);

                // Update comfort score
                _comfortLevelText.text = $"Avg Comfort: {result.ComfortScore}";
                UpdateComfortIndicator(result.ComfortScore);
            }
        }

        private void UpdateCurrentConditionsDisplay(EnvironmentalConditions conditions)
        {
            _temperatureText.text = $"{conditions.Temperature.Celsius.Value:F1}°C";
            _humidityText.text = $"{conditions.Humidity:F0}%";
            _airQualityText.text = $"{conditions.AirQualityIndex:F0} AQI";
            _co2LevelText.text = $"{conditions.CO2Level:F0} ppm";
            _lightLevelText.text = $"{conditions.LightLevel:F0} lux";
            _noiseLevelText.text = $"{conditions.NoiseLevel:F0} dB";

            // Update gauges
            UpdateEnvironmentalGauge(_temperatureGauge, NormalizeTemperature(conditions.Temperature.Celsius.Value));
            UpdateEnvironmentalGauge(_humidityGauge, (float)conditions.Humidity / 100f);
            UpdateEnvironmentalGauge(_airQualityGauge, NormalizeAirQuality(conditions.AirQualityIndex));
        }

        private void UpdateComfortIndicators(EnvironmentalConditions conditions)
        {
            var comfortLevel = conditions.GetComfortLevel();
            var isComfortable = conditions.IsComfortable;

            _comfortLevelText.text = comfortLevel.ToString();
            UpdateComfortIndicator(comfortLevel);
        }

        private void UpdateComfortIndicator(ComfortLevel level)
        {
            if (_comfortIndicator != null)
            {
                var color = GetComfortColor(level);
                _comfortIndicator.color = color;
            }
        }

        private void UpdateEnvironmentalGauge(Slider gauge, float normalizedValue)
        {
            if (gauge != null)
            {
                gauge.value = normalizedValue;

                // Update gauge colors
                var fillImage = gauge.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    var color = GetGaugeColor(normalizedValue);
                    fillImage.color = color;
                }
            }
        }

        private void UpdateTrendCharts()
        {
            if (_historicalData.Count < 2) return;

            var temperatureData = _historicalData.Select(d => d.Temperature.Celsius.Value).ToList();
            var humidityData = _historicalData.Select(d => (float)d.Humidity).ToList();
            var airQualityData = _historicalData.Select(d => (float)d.AirQualityIndex).ToList();

            _temperatureTrendChart?.SetData(temperatureData);
            _humidityTrendChart?.SetData(humidityData);
            _airQualityTrendChart?.SetData(airQualityData);
        }

        private void UpdateZoneStatuses(EnvironmentalConditions conditions)
        {
            // Update zone-specific information
            foreach (var zone in _zoneInfo)
            {
                // In a real implementation, you'd have zone-specific sensors
                // For demo, we'll apply slight variations
                var zoneConditions = CreateZoneConditions(conditions, zone.Id);
                zone.ZoneConditions = zoneConditions;

                // Update zone display if it exists
                UpdateZoneDisplay(zone);
            }
        }

        private void InitializeZoneInfo(Building building)
        {
            _zoneInfo.Clear();

            // Create zones based on building floors and rooms
            var floorNumber = 1;
            foreach (var floor in building.Floors)
            {
                var zoneInfo = new ZoneInfo
                {
                    Id = $"Floor_{floorNumber}",
                    Name = $"Floor {floorNumber}",
                    Type = "Floor",
                    ZoneConditions = EnvironmentalConditions.Default
                };
                _zoneInfo.Add(zoneInfo);

                // Create zones for major rooms
                foreach (var room in floor.Rooms.Where(r => r.Type == "Office" || r.Type == "Conference"))
                {
                    var roomZone = new ZoneInfo
                    {
                        Id = room.Id.ToString(),
                        Name = room.Name,
                        Type = room.Type,
                        ZoneConditions = EnvironmentalConditions.Default
                    };
                    _zoneInfo.Add(roomZone);
                }

                floorNumber++;
            }

            PopulateZoneDisplay();
        }

        private void PopulateZoneDisplay()
        {
            if (_zonesContainer == null || _zoneItemPrefab == null) return;

            // Clear existing zones
            foreach (Transform child in _zonesContainer)
            {
                Destroy(child.gameObject);
            }

            // Create zone items
            foreach (var zone in _zoneInfo)
            {
                var zoneGO = Instantiate(_zoneItemPrefab, _zonesContainer);
                var zoneDisplay = zoneGO.AddComponent<ZoneDisplayItem>();
                zoneDisplay.Initialize(zone);
            }
        }

        private void UpdateZoneDisplay(ZoneInfo zone)
        {
            // Find the zone display and update it
            var zoneDisplay = _zonesContainer.GetComponentsInChildren<ZoneDisplayItem>()
                .FirstOrDefault(zd => zd.ZoneId == zone.Id);

            zoneDisplay?.UpdateConditions(zone.ZoneConditions);
        }

        private void SetupControls()
        {
            if (_adjustTargetButton != null)
                _adjustTargetButton.onClick.AddListener(OnAdjustTargetClicked);

            if (_runEnvironmentalSimulationButton != null)
                _runEnvironmentalSimulationButton.onClick.AddListener(OnRunSimulationClicked);
        }

        private void SetupTimeRangeOptions()
        {
            if (_timeRangeDropdown != null)
            {
                var options = new List<TMP_Dropdown.OptionData>
                {
                    new TMP_Dropdown.OptionData("Last Hour"),
                    new TMP_Dropdown.OptionData("Last 6 Hours"),
                    new TMP_Dropdown.OptionData("Last 24 Hours"),
                    new TMP_Dropdown.OptionData("Last Week"),
                    new TMP_Dropdown.OptionData("Last Month")
                };

                _timeRangeDropdown.options = options;
                _timeRangeDropdown.onValueChanged.AddListener(OnTimeRangeChanged);
            }
        }

        private void StartEnvironmentalMonitoring()
        {
            // Subscribe to environmental data updates
            if (_serviceLocator != null)
            {
                var dataService = _serviceLocator.GetService<IDataCollectionService>();
                if (dataService != null)
                {
                    // Start monitoring environmental conditions
                }
            }
        }

        private void OnAdjustTargetClicked()
        {
            // Open environmental target adjustment panel
            Debug.Log("Adjust environmental targets");
        }

        private void OnRunSimulationClicked()
        {
            if (_currentBuilding != null && _serviceLocator != null)
            {
                var simulationService = _serviceLocator.GetService<ISimulationService>();
                var parameters = new EnvironmentalParameters
                {
                    TargetTemperature = Temperature.FromCelsius(22),
                    TargetHumidity = 45,
                    TargetLightLevel = 500,
                    EnableHVACSimulation = true,
                    EnableLightingSimulation = true
                };

                // Run simulation for first room as example
                var firstRoom = _currentBuilding.Floors.FirstOrDefault()?.Rooms?.FirstOrDefault();
                if (firstRoom != null)
                {
                    _ = simulationService?.SimulateRoomConditionsAsync(
                        firstRoom.Id,
                        TimeSpan.FromHours(24),
                        parameters
                    );
                }
            }
        }

        private void OnTimeRangeChanged(int index)
        {
            // Update trend chart data based on selected time range
            Debug.Log($"Time range changed to: {_timeRangeDropdown.options[index].text}");
        }

        // Helper Methods
        private float NormalizeTemperature(float celsius)
        {
            // Normalize -20°C to 50°C range to 0-1
            return Mathf.Clamp01((celsius + 20f) / 70f);
        }

        private float NormalizeAirQuality(decimal aqi)
        {
            // Normalize 0-500 AQI range to 0-1 (inverted - lower is better)
            return Mathf.Clamp01(1f - ((float)aqi / 500f));
        }

        private Color GetComfortColor(ComfortLevel level)
        {
            return level switch
            {
                ComfortLevel.Excellent => Color.green,
                ComfortLevel.Good => Color.cyan,
                ComfortLevel.Fair => Color.yellow,
                ComfortLevel.Poor => Color.orange,
                ComfortLevel.Unacceptable => Color.red,
                _ => Color.gray
            };
        }

        private Color GetGaugeColor(float value)
        {
            if (value >= 0.7f) return Color.green;
            if (value >= 0.4f) return Color.yellow;
            return Color.red;
        }

        private EnvironmentalConditions CreateZoneConditions(EnvironmentalConditions baseConditions, string zoneId)
        {
            // Create zone-specific variations
            var random = new System.Random(zoneId.GetHashCode());
            var tempVariation = (random.NextDouble() - 0.5) * 4; // ±2°C
            var humidityVariation = (random.NextDouble() - 0.5) * 10; // ±5%
            var aqiVariation = (random.NextDouble() - 0.5) * 20; // ±10 AQI

            return new EnvironmentalConditions(
                Temperature.FromCelsius((decimal)((float)baseConditions.Temperature.Celsius.Value + tempVariation)),
                baseConditions.Humidity + (decimal)humidityVariation,
                baseConditions.AirQualityIndex + (decimal)aqiVariation,
                baseConditions.CO2Level,
                baseConditions.LightLevel,
                baseConditions.NoiseLevel,
                baseConditions.Timestamp
            );
        }

        private void ResetDisplay()
        {
            _temperatureText.text = "--°C";
            _humidityText.text = "--%";
            _airQualityText.text = "-- AQI";
            _co2LevelText.text = "-- ppm";
            _lightLevelText.text = "-- lux";
            _noiseLevelText.text = "-- dB";

            _comfortLevelText.text = "Unknown";
            UpdateComfortIndicator(ComfortLevel.Unacceptable);

            UpdateEnvironmentalGauge(_temperatureGauge, 0);
            UpdateEnvironmentalGauge(_humidityGauge, 0);
            UpdateEnvironmentalGauge(_airQualityGauge, 0);

            _historicalData.Clear();
        }
    }

    /// <summary>
    /// Zone Display Item Component
    /// </summary>
    public class ZoneDisplayItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _zoneNameText;
        [SerializeField] private TextMeshProUGUI _zoneTypeText;
        [SerializeField] private TextMeshProUGUI _zoneTemperatureText;
        [SerializeField] private TextMeshProUGUI _zoneHumidityText;
        [SerializeField] private TextMeshProUGUI _zoneComfortText;
        [SerializeField] private Image _zoneComfortIndicator;

        private string _zoneId;

        public string ZoneId => _zoneId;

        public void Initialize(ZoneInfo zone)
        {
            _zoneId = zone.Id;
            _zoneNameText.text = zone.Name;
            _zoneTypeText.text = zone.Type;
            UpdateConditions(zone.ZoneConditions);
        }

        public void UpdateConditions(EnvironmentalConditions conditions)
        {
            _zoneTemperatureText.text = $"{conditions.Temperature.Celsius.Value:F1}°C";
            _zoneHumidityText.text = $"{conditions.Humidity:F0}%";
            
            var comfortLevel = conditions.GetComfortLevel();
            _zoneComfortText.text = comfortLevel.ToString();
            
            if (_zoneComfortIndicator != null)
            {
                var color = GetComfortColor(comfortLevel);
                _zoneComfortIndicator.color = color;
            }
        }

        private Color GetComfortColor(ComfortLevel level)
        {
            return level switch
            {
                ComfortLevel.Excellent => Color.green,
                ComfortLevel.Good => Color.cyan,
                ComfortLevel.Fair => Color.yellow,
                ComfortLevel.Poor => Color.orange,
                ComfortLevel.Unacceptable => Color.red,
                _ => Color.gray
            };
        }
    }

    /// <summary>
    /// Sensor Data Grid Component
    /// 
    /// Architectural Intent:
    /// - Displays comprehensive sensor data in a grid format
    /// - Shows real-time readings and quality indicators
    /// - Enables sensor status monitoring and alerts
    /// - Provides sensor grouping and filtering
    /// </summary>
    public class DigitalTwinSensorGrid : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private Transform _sensorGridContainer;
        [SerializeField] private GameObject _sensorItemPrefab;
        [SerializeField] private TMP_InputField _searchInput;
        [SerializeField] private TMP_Dropdown _filterDropdown;
        [SerializeField] private TextMeshProUGUI _sensorCountText;

        [Header("Grouping Options")]
        [SerializeField] private Button _groupByTypeButton;
        [SerializeField] private Button _groupByLocationButton;
        [SerializeField] private Button _groupByStatusButton;

        private DigitalTwinDashboardManager _dashboardManager;
        private ServiceLocator _serviceLocator;
        private List<SensorDataItem> _sensorItems = new List<SensorDataItem>();
        private Dictionary<Guid, SensorReading> _latestReadings = new Dictionary<Guid, SensorReading>();
        private SensorGroupingMode _currentGroupingMode = SensorGroupingMode.None;
        private string _currentFilter = "";
        private string _currentTypeFilter = "All";

        public void Initialize(DigitalTwinDashboardManager dashboard, ServiceLocator serviceLocator)
        {
            _dashboardManager = dashboard;
            _serviceLocator = serviceLocator;

            SetupFilterControls();
            SetupGroupingControls();
            RefreshSensorGrid();
        }

        public void UpdateSensorReading(SensorReading reading)
        {
            _latestReadings[reading.SensorId] = reading;

            // Find and update the corresponding sensor item
            var sensorItem = _sensorItems.FirstOrDefault(si => si.SensorId == reading.SensorId);
            sensorItem?.UpdateReading(reading);
        }

        public void UpdateSensorStatus(Guid sensorId, SensorStatus status)
        {
            var sensorItem = _sensorItems.FirstOrDefault(si => si.SensorId == sensorId);
            sensorItem?.UpdateStatus(status);
        }

        private void SetupFilterControls()
        {
            if (_searchInput != null)
            {
                _searchInput.onValueChanged.AddListener(OnSearchChanged);
            }

            if (_filterDropdown != null)
            {
                var options = new List<TMP_Dropdown.OptionData>
                {
                    new TMP_Dropdown.OptionData("All"),
                    new TMP_Dropdown.OptionData("Temperature"),
                    new TMP_Dropdown.OptionData("Humidity"),
                    new TMP_Dropdown.OptionData("Energy"),
                    new TMP_Dropdown.OptionData("Air Quality"),
                    new TMP_Dropdown.OptionData("CO2"),
                    new TMP_Dropdown.OptionData("Light"),
                    new TMP_Dropdown.OptionData("Noise")
                };

                _filterDropdown.options = options;
                _filterDropdown.onValueChanged.AddListener(OnFilterChanged);
            }
        }

        private void SetupGroupingControls()
        {
            if (_groupByTypeButton != null)
                _groupByTypeButton.onClick.AddListener(() => SetGroupingMode(SensorGroupingMode.Type));

            if (_groupByLocationButton != null)
                _groupByLocationButton.onClick.AddListener(() => SetGroupingMode(SensorGroupingMode.Location));

            if (_groupByStatusButton != null)
                _groupByStatusButton.onClick.AddListener(() => SetGroupingMode(SensorGroupingMode.Status));
        }

        private void OnSearchChanged(string searchValue)
        {
            _currentFilter = searchValue.ToLower();
            RefreshSensorGrid();
        }

        private void OnFilterChanged(int index)
        {
            _currentTypeFilter = _filterDropdown.options[index].text;
            RefreshSensorGrid();
        }

        private void SetGroupingMode(SensorGroupingMode mode)
        {
            _currentGroupingMode = mode;
            RefreshSensorGrid();

            // Update button states
            UpdateGroupingButtonStates();
        }

        private void RefreshSensorGrid()
        {
            // Clear existing grid items
            ClearSensorGrid();

            // Filter sensors
            var filteredSensors = GetFilteredSensors();

            // Group sensors if needed
            var groupedSensors = GroupSensors(filteredSensors);

            // Create sensor items
            CreateSensorItems(groupedSensors);

            // Update count
            UpdateSensorCount(filteredSensors.Count);
        }

        private void ClearSensorGrid()
        {
            foreach (Transform child in _sensorGridContainer)
            {
                Destroy(child.gameObject);
            }
            _sensorItems.Clear();
        }

        private List<SensorInfo> GetFilteredSensors()
        {
            // Get all sensors from current building (mock data for demo)
            var allSensors = GetMockSensors();

            return allSensors.Where(sensor =>
            {
                var matchesSearch = string.IsNullOrEmpty(_currentFilter) || 
                                 sensor.Name.ToLower().Contains(_currentFilter) ||
                                 sensor.Type.ToLower().Contains(_currentFilter);
                
                var matchesType = _currentTypeFilter == "All" || 
                                sensor.Type == _currentTypeFilter;

                return matchesSearch && matchesType;
            }).ToList();
        }

        private Dictionary<string, List<SensorInfo>> GroupSensors(List<SensorInfo> sensors)
        {
            var grouped = new Dictionary<string, List<SensorInfo>>();

            switch (_currentGroupingMode)
            {
                case SensorGroupingMode.Type:
                    foreach (var sensor in sensors)
                    {
                        if (!grouped.ContainsKey(sensor.Type))
                            grouped[sensor.Type] = new List<SensorInfo>();
                        grouped[sensor.Type].Add(sensor);
                    }
                    break;

                case SensorGroupingMode.Location:
                    foreach (var sensor in sensors)
                    {
                        if (!grouped.ContainsKey(sensor.Location))
                            grouped[sensor.Location] = new List<SensorInfo>();
                        grouped[sensor.Location].Add(sensor);
                    }
                    break;

                case SensorGroupingMode.Status:
                    foreach (var sensor in sensors)
                    {
                        var status = GetSensorStatus(sensor.Id);
                        if (!grouped.ContainsKey(status))
                            grouped[status] = new List<SensorInfo>();
                        grouped[status].Add(sensor);
                    }
                    break;

                default:
                    grouped["All"] = sensors;
                    break;
            }

            return grouped;
        }

        private void CreateSensorItems(Dictionary<string, List<SensorInfo>> groupedSensors)
        {
            foreach (var group in groupedSensors)
            {
                if (_currentGroupingMode != SensorGroupingMode.None)
                {
                    CreateGroupHeader(group.Key);
                }

                foreach (var sensor in group.Value)
                {
                    var sensorGO = Instantiate(_sensorItemPrefab, _sensorGridContainer);
                    var sensorItem = sensorGO.AddComponent<SensorDataItem>();
                    
                    sensorItem.Initialize(sensor, _latestReadings.GetValueOrDefault(sensor.Id));
                    _sensorItems.Add(sensorItem);
                }
            }
        }

        private void CreateGroupHeader(string groupName)
        {
            var headerGO = new GameObject($"GroupHeader_{groupName}");
            headerGO.transform.SetParent(_sensorGridContainer, false);

            var headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = groupName;
            headerText.fontSize = 14;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = Color.gray;
        }

        private void UpdateSensorCount(int count)
        {
            if (_sensorCountText != null)
            {
                _sensorCountText.text = $"Showing {count} sensors";
            }
        }

        private void UpdateGroupingButtonStates()
        {
            // Reset all buttons
            if (_groupByTypeButton != null)
                _groupByTypeButton.interactable = _currentGroupingMode != SensorGroupingMode.Type;
            
            if (_groupByLocationButton != null)
                _groupByLocationButton.interactable = _currentGroupingMode != SensorGroupingMode.Location;
            
            if (_groupByStatusButton != null)
                _groupByStatusButton.interactable = _currentGroupingMode != SensorGroupingMode.Status;
        }

        private string GetSensorStatus(Guid sensorId)
        {
            // In a real implementation, this would come from the sensor service
            return _latestReadings.ContainsKey(sensorId) ? "Active" : "Offline";
        }

        private List<SensorInfo> GetMockSensors()
        {
            // Mock sensor data for demonstration
            return new List<SensorInfo>
            {
                new SensorInfo { Id = Guid.NewGuid(), Name = "Office Temp Sensor 1", Type = "Temperature", Location = "Floor 1" },
                new SensorInfo { Id = Guid.NewGuid(), Name = "Office Humidity Sensor 1", Type = "Humidity", Location = "Floor 1" },
                new SensorInfo { Id = Guid.NewGuid(), Name = "Conference Room Sensor", Type = "Air Quality", Location = "Floor 2" },
                new SensorInfo { Id = Guid.NewGuid(), Name = "Main Energy Meter", Type = "Energy", Location = "Building" },
                new SensorInfo { Id = Guid.NewGuid(), Name = "HVAC CO2 Sensor", Type = "CO2", Location = "Floor 1" },
                new SensorInfo { Id = Guid.NewGuid(), Name = "Lobby Light Sensor", Type = "Light", Location = "Floor 1" },
                new SensorInfo { Id = Guid.NewGuid(), Name = "Server Room Noise", Type = "Noise", Location = "Floor 3" }
            };
        }

        // Supporting Classes
        public class SensorInfo
        {
            public Guid Id;
            public string Name;
            public string Type;
            public string Location;
        }

        public enum SensorGroupingMode
        {
            None,
            Type,
            Location,
            Status
        }
    }
}