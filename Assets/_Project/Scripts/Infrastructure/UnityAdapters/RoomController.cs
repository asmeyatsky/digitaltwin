using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Infrastructure.UnityAdapters
{
    /// <summary>
    /// Room Controller MonoBehaviour Adapter
    /// 
    /// Architectural Intent:
    /// - Bridges Unity's component system with room domain logic
    /// - Handles room-specific Unity lifecycle and visualization
    /// - Provides thin adapter layer for room operations
    /// - Manages room-level environmental data display
    /// 
    /// Key Design Decisions:
    /// 1. MonoBehaviour is thin adapter, domain logic in services
    /// 2. Component-based architecture for visualization
    /// 3. Event-driven updates from sensor data
    /// 4. Efficient batching of visual updates
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        [Header("Room Configuration")]
        [SerializeField] private RoomConfiguration _roomConfig;
        [SerializeField] private RoomView _roomView;
        [SerializeField] private Transform _equipmentContainer;
        [SerializeField] private Transform _sensorContainer;
        
        [Header("Visualization Settings")]
        [SerializeField] private bool _showEquipment = true;
        [SerializeField] private bool _showSensors = true;
        [SerializeField] private bool _showEnvironmentalOverlays = true;
        [SerializeField] private bool _enableUserInteraction = true;
        
        [Header("Environmental Display")]
        [SerializeField] private GameObject _temperatureDisplay;
        [SerializeField] private GameObject _humidityDisplay;
        [SerializeField] private GameObject _lightLevelDisplay;
        [SerializeField] private GameObject _airQualityDisplay;

        // Private fields
        private Room _room;
        private ServiceLocator _serviceLocator;
        private IDataCollectionService _dataCollectionService;
        private List<EquipmentController> _equipmentControllers = new List<EquipmentController>();
        private List<SensorController> _sensorControllers = new List<SensorController>();
        private EnvironmentalConditions _currentConditions;
        private Dictionary<SensorType, SensorReading> _latestReadings = new Dictionary<SensorType, SensorReading>();
        private bool _needsVisualUpdate = false;
        private float _lastUpdateTime;

        // Properties
        public Room Room => _room;
        public Guid RoomId => _room?.Id ?? Guid.Empty;
        public bool IsOccupied => _room?.IsOccupied ?? false;
        public EnvironmentalConditions CurrentConditions => _currentConditions;

        // Unity Lifecycle
        private void Awake()
        {
            ValidateConfiguration();
            InitializeRoomView();
        }

        private void Start()
        {
            ResolveDependencies();
            StartRoomMonitoring();
        }

        private void Update()
        {
            if (_room == null) return;

            // Batch visual updates for performance
            if (_needsVisualUpdate && Time.time - _lastUpdateTime > 0.1f) // Update every 100ms
            {
                UpdateVisualization();
                _needsVisualUpdate = false;
                _lastUpdateTime = Time.time;
            }

            // Handle user interactions
            if (_enableUserInteraction)
            {
                HandleUserInput();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        // Public Methods
        public void Initialize(Room room, ServiceLocator serviceLocator)
        {
            _room = room ?? throw new ArgumentNullException(nameof(room));
            _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
            
            _currentConditions = room.CurrentConditions;
            InitializeComponents();
            UpdateVisualization();
        }

        public void AddEquipment(Equipment equipment)
        {
            if (_room == null)
            {
                Debug.LogError("Room not initialized");
                return;
            }

            try
            {
                _room = _room.AddEquipment(equipment);
                CreateEquipmentController(equipment);
                UpdateVisualization();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add equipment: {ex.Message}");
            }
        }

        public void RemoveEquipment(Guid equipmentId)
        {
            if (_room == null)
            {
                Debug.LogError("Room not initialized");
                return;
            }

            try
            {
                _room = _room.RemoveEquipment(equipmentId);
                RemoveEquipmentController(equipmentId);
                UpdateVisualization();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove equipment: {ex.Message}");
            }
        }

        public void AddSensor(Sensor sensor)
        {
            if (_room == null)
            {
                Debug.LogError("Room not initialized");
                return;
            }

            try
            {
                _room = _room.AddSensor(sensor);
                CreateSensorController(sensor);
                UpdateVisualization();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add sensor: {ex.Message}");
            }
        }

        public void RemoveSensor(Guid sensorId)
        {
            if (_room == null)
            {
                Debug.LogError("Room not initialized");
                return;
            }

            try
            {
                _room = _room.RemoveSensor(sensorId);
                RemoveSensorController(sensorId);
                UpdateVisualization();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove sensor: {ex.Message}");
            }
        }

        public void SetOccupied(bool occupied)
        {
            if (_room == null)
            {
                Debug.LogError("Room not initialized");
                return;
            }

            try
            {
                _room = _room.SetOccupied(occupied);
                UpdateVisualization();
                
                // Update room view to reflect occupancy
                if (_roomView != null)
                {
                    _roomView.UpdateOccupancy(occupied);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set occupancy: {ex.Message}");
            }
        }

        public void UpdateEnvironmentalConditions(EnvironmentalConditions conditions)
        {
            if (_room == null)
            {
                Debug.LogError("Room not initialized");
                return;
            }

            try
            {
                _room = _room.UpdateEnvironmentalConditions(conditions);
                _currentConditions = conditions;
                _needsVisualUpdate = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update environmental conditions: {ex.Message}");
            }
        }

        public void UpdateSensorReading(SensorReading reading)
        {
            if (_room == null) return;

            // Store latest reading for this sensor type
            _latestReadings[reading.SensorType] = reading;

            // Update equipment controller if it's an equipment sensor
            var equipmentController = FindEquipmentControllerForSensor(reading.SensorId);
            if (equipmentController != null)
            {
                equipmentController.UpdateSensorReading(reading);
            }

            // Mark for visual update
            _needsVisualUpdate = true;
        }

        public void UpdateRealTimeVisualization()
        {
            _needsVisualUpdate = true;
        }

        public EquipmentController GetEquipmentController(Guid equipmentId)
        {
            return _equipmentControllers.Find(ec => ec.Equipment.Id == equipmentId);
        }

        public SensorController GetSensorController(Guid sensorId)
        {
            return _sensorControllers.Find(sc => sc.Sensor.Id == sensorId);
        }

        // Private Methods
        private void ValidateConfiguration()
        {
            if (_roomView == null)
            {
                _roomView = GetComponent<RoomView>();
                if (_roomView == null)
                {
                    throw new InvalidOperationException("Room view component is required");
                }
            }

            if (_equipmentContainer == null)
            {
                _equipmentContainer = transform.Find("EquipmentContainer");
                if (_equipmentContainer == null)
                {
                    var container = new GameObject("EquipmentContainer");
                    container.transform.SetParent(transform);
                    _equipmentContainer = container.transform;
                }
            }

            if (_sensorContainer == null)
            {
                _sensorContainer = transform.Find("SensorContainer");
                if (_sensorContainer == null)
                {
                    var container = new GameObject("SensorContainer");
                    container.transform.SetParent(transform);
                    _sensorContainer = container.transform;
                }
            }
        }

        private void InitializeRoomView()
        {
            if (_roomView != null)
            {
                _roomView.Initialize(this);
            }
        }

        private void ResolveDependencies()
        {
            _serviceLocator = FindObjectOfType<ServiceLocator>();
            if (_serviceLocator != null)
            {
                _dataCollectionService = _serviceLocator.GetService<IDataCollectionService>();
            }

            if (_dataCollectionService == null)
            {
                Debug.LogWarning("Data collection service not found");
            }
        }

        private void StartRoomMonitoring()
        {
            if (_dataCollectionService == null || _room == null) return;

            try
            {
                // Start data collection for room sensors
                var sensorIds = new List<Guid>();
                foreach (var sensor in _room.Sensors)
                {
                    sensorIds.Add(sensor.Id);
                }

                if (sensorIds.Count > 0)
                {
                    _dataCollectionService.StartDataStreamAsync(sensorIds, OnSensorDataReceived);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start room monitoring: {ex.Message}");
            }
        }

        private async Task InitializeComponents()
        {
            // Initialize equipment controllers
            foreach (var equipment in _room.Equipment)
            {
                CreateEquipmentController(equipment);
            }

            // Initialize sensor controllers
            foreach (var sensor in _room.Sensors)
            {
                CreateSensorController(sensor);
            }

            // Initialize environmental displays
            InitializeEnvironmentalDisplays();
        }

        private void CreateEquipmentController(Equipment equipment)
        {
            var equipmentController = InstantiateEquipmentPrefab(equipment);
            if (equipmentController != null)
            {
                equipmentController.Initialize(equipment, _serviceLocator);
                equipmentController.transform.SetParent(_equipmentContainer);
                _equipmentControllers.Add(equipmentController);
            }
        }

        private void CreateSensorController(Sensor sensor)
        {
            var sensorController = InstantiateSensorPrefab(sensor);
            if (sensorController != null)
            {
                sensorController.Initialize(sensor, _serviceLocator);
                sensorController.transform.SetParent(_sensorContainer);
                _sensorControllers.Add(sensorController);
            }
        }

        private EquipmentController InstantiateEquipmentPrefab(Equipment equipment)
        {
            // This would use a prefab mapping system
            var prefab = GetEquipmentPrefab(equipment.Type);
            if (prefab != null)
            {
                var equipmentObject = Instantiate(prefab, _equipmentContainer);
                return equipmentObject.GetComponent<EquipmentController>();
            }
            return null;
        }

        private SensorController InstantiateSensorPrefab(Sensor sensor)
        {
            // This would use a prefab mapping system
            var prefab = GetSensorPrefab(sensor.Type);
            if (prefab != null)
            {
                var sensorObject = Instantiate(prefab, _sensorContainer);
                return sensorObject.GetComponent<SensorController>();
            }
            return null;
        }

        private GameObject GetEquipmentPrefab(EquipmentType type)
        {
            // This would load from resources or addressables
            switch (type)
            {
                case EquipmentType.HVAC:
                    return Resources.Load<GameObject>("Prefabs/Equipment/HVAC");
                case EquipmentType.Lighting:
                    return Resources.Load<GameObject>("Prefabs/Equipment/Lighting");
                case EquipmentType.Server:
                    return Resources.Load<GameObject>("Prefabs/Equipment/Server");
                default:
                    return Resources.Load<GameObject>("Prefabs/Equipment/Default");
            }
        }

        private GameObject GetSensorPrefab(SensorType type)
        {
            // This would load from resources or addressables
            switch (type)
            {
                case SensorType.Temperature:
                    return Resources.Load<GameObject>("Prefabs/Sensors/TemperatureSensor");
                case SensorType.Humidity:
                    return Resources.Load<GameObject>("Prefabs/Sensors/HumiditySensor");
                case SensorType.Motion:
                    return Resources.Load<GameObject>("Prefabs/Sensors/MotionSensor");
                default:
                    return Resources.Load<GameObject>("Prefabs/Sensors/Default");
            }
        }

        private void InitializeEnvironmentalDisplays()
        {
            if (_temperatureDisplay != null)
            {
                _temperatureDisplay.SetActive(_showEnvironmentalOverlays);
            }
            if (_humidityDisplay != null)
            {
                _humidityDisplay.SetActive(_showEnvironmentalOverlays);
            }
            if (_lightLevelDisplay != null)
            {
                _lightLevelDisplay.SetActive(_showEnvironmentalOverlays);
            }
            if (_airQualityDisplay != null)
            {
                _airQualityDisplay.SetActive(_showEnvironmentalOverlays);
            }
        }

        private void UpdateVisualization()
        {
            if (_roomView != null)
            {
                _roomView.UpdateRoom(_room, _currentConditions);
            }

            UpdateEnvironmentalDisplays();
        }

        private void UpdateEnvironmentalDisplays()
        {
            if (!_showEnvironmentalOverlays) return;

            // Update temperature display
            if (_temperatureDisplay != null && _latestReadings.ContainsKey(SensorType.Temperature))
            {
                var tempReading = _latestReadings[SensorType.Temperature];
                UpdateTemperatureDisplay(tempReading);
            }

            // Update humidity display
            if (_humidityDisplay != null && _latestReadings.ContainsKey(SensorType.Humidity))
            {
                var humidityReading = _latestReadings[SensorType.Humidity];
                UpdateHumidityDisplay(humidityReading);
            }

            // Update light level display
            if (_lightLevelDisplay != null && _latestReadings.ContainsKey(SensorType.Light))
            {
                var lightReading = _latestReadings[SensorType.Light];
                UpdateLightLevelDisplay(lightReading);
            }

            // Update air quality display
            if (_airQualityDisplay != null && _latestReadings.ContainsKey(SensorType.AirQuality))
            {
                var airQualityReading = _latestReadings[SensorType.AirQuality];
                UpdateAirQualityDisplay(airQualityReading);
            }
        }

        private void UpdateTemperatureDisplay(SensorReading reading)
        {
            // Update UI text or visual indicators
            var textComponent = _temperatureDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"{reading.Value:F1}°C";
            }

            // Update color based on temperature range
            var temperature = Temperature.FromCelsius(reading.Value);
            var color = temperature.Celsius.Value switch
            {
                < 18 => Color.blue,
                >= 18 and < 24 => Color.green,
                >= 24 and < 28 => Color.yellow,
                _ => Color.red
            };

            var renderer = _temperatureDisplay.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private void UpdateHumidityDisplay(SensorReading reading)
        {
            var textComponent = _humidityDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"{reading.Value:F0}%";
            }

            var color = reading.Value switch
            {
                < 30 => Color.red,
                >= 30 and < 60 => Color.green,
                >= 60 and < 70 => Color.yellow,
                _ => Color.red
            };

            var renderer = _humidityDisplay.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private void UpdateLightLevelDisplay(SensorReading reading)
        {
            var textComponent = _lightLevelDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"{reading.Value:F0} lux";
            }

            var color = reading.Value switch
            {
                < 300 => Color.red,
                >= 300 and < 1000 => Color.yellow,
                _ => Color.green
            };

            var renderer = _lightLevelDisplay.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private void UpdateAirQualityDisplay(SensorReading reading)
        {
            var textComponent = _airQualityDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"AQI: {reading.Value:F0}";
            }

            var color = reading.Value switch
            {
                < 50 => Color.green,
                >= 50 and < 100 => Color.yellow,
                >= 100 and < 150 => Color.orange,
                _ => Color.red
            };

            var renderer = _airQualityDisplay.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private void HandleUserInput()
        {
            // Handle mouse clicks for selection
            if (Input.GetMouseButtonDown(0))
            {
                HandleRoomClick();
            }
        }

        private void HandleRoomClick()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var roomView = hit.collider.GetComponent<RoomView>();
                if (roomView != null && roomView == _roomView)
                {
                    OnRoomSelected();
                }
            }
        }

        private void OnRoomSelected()
        {
            Debug.Log($"Room selected: {_room.Name}");
            
            // Show room details UI
            if (_roomView != null)
            {
                _roomView.ShowRoomDetails(_room, _currentConditions);
            }
        }

        private EquipmentController FindEquipmentControllerForSensor(Guid sensorId)
        {
            // Find which equipment this sensor belongs to
            foreach (var equipmentController in _equipmentControllers)
            {
                if (equipmentController.HasSensor(sensorId))
                {
                    return equipmentController;
                }
            }
            return null;
        }

        private void RemoveEquipmentController(Guid equipmentId)
        {
            var controller = GetEquipmentController(equipmentId);
            if (controller != null)
            {
                _equipmentControllers.Remove(controller);
                if (Application.isPlaying)
                    Destroy(controller.gameObject);
                else
                    DestroyImmediate(controller.gameObject);
            }
        }

        private void RemoveSensorController(Guid sensorId)
        {
            var controller = GetSensorController(sensorId);
            if (controller != null)
            {
                _sensorControllers.Remove(controller);
                if (Application.isPlaying)
                    Destroy(controller.gameObject);
                else
                    DestroyImmediate(controller.gameObject);
            }
        }

        private async void OnSensorDataReceived(SensorReading reading)
        {
            // Check if this reading belongs to this room
            var sensor = _room.GetSensor(reading.SensorId);
            if (sensor != null)
            {
                UpdateSensorReading(reading);
            }
        }

        private void Cleanup()
        {
            // Stop data collection
            if (_dataCollectionService != null && _room != null)
            {
                var sensorIds = new List<Guid>();
                foreach (var sensor in _room.Sensors)
                {
                    sensorIds.Add(sensor.Id);
                }

                if (sensorIds.Count > 0)
                {
                    _dataCollectionService.StopDataStreamAsync(sensorIds);
                }
            }
        }
    }
}