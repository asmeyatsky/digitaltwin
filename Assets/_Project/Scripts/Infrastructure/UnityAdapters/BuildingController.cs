using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.ValueObjects;
using DigitalTwin.Application.Services;

namespace DigitalTwin.Infrastructure.UnityAdapters
{
    /// <summary>
    /// Building Controller MonoBehaviour Adapter
    /// 
    /// Architectural Intent:
    /// - Bridges Unity's component system with domain logic
    /// - Handles Unity lifecycle (Awake, Start, Update, OnDestroy)
    /// - Provides thin adapter layer for building operations
    /// - Manages Unity-specific concerns (prefabs, scenes, serialization)
    /// 
    /// Key Design Decisions:
    /// 1. MonoBehaviour is thin adapter, domain logic in services
    /// 2. Dependency injection through Initialize method
    /// 3. Event-driven architecture for real-time updates
    /// 4. Component composition over inheritance
    /// </summary>
    public class BuildingController : MonoBehaviour
    {
        [Header("Building Configuration")]
        [SerializeField] private BuildingConfiguration _buildingConfig;
        [SerializeField] private Transform _buildingTransform;
        [SerializeField] private BuildingView _buildingView;
        
        [Header("Visualization Settings")]
        [SerializeField] private bool _showRealTimeData = true;
        [SerializeField] private bool _showEnergyOverlays = true;
        [SerializeField] private bool _showEnvironmentalData = true;
        
        [Header("References")]
        [SerializeField] private ServiceLocator _serviceLocator;

        // Private fields
        private Building _building;
        private IBuildingSimulationService _simulationService;
        private IDataAnalyticsService _analyticsService;
        private IDataCollectionService _dataCollectionService;
        private List<FloorController> _floorControllers = new List<FloorController>();
        private List<RoomController> _roomControllers = new List<RoomController>();
        private List<EquipmentController> _equipmentControllers = new List<EquipmentController>();
        private List<SensorController> _sensorControllers = new List<SensorController>();

        // Properties
        public Building Building => _building;
        public BuildingConfiguration Configuration => _buildingConfig;
        public bool IsInitialized => _building != null;

        // Unity Lifecycle
        private void Awake()
        {
            ValidateConfiguration();
            InitializeBuilding();
        }

        private void Start()
        {
            ResolveDependencies();
            StartDataCollection();
            StartSimulation();
        }

        private void Update()
        {
            if (!IsInitialized) return;

            // Update visualization based on real-time data
            if (_showRealTimeData)
            {
                UpdateRealTimeVisualization();
            }

            // Process user interactions
            HandleUserInput();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        // Public Methods
        public async void Initialize(Building building, ServiceLocator serviceLocator)
        {
            _building = building ?? throw new ArgumentNullException(nameof(building));
            _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
            
            await InitializeComponents();
            UpdateVisualization();
        }

        public void AddFloor(Floor floor)
        {
            if (_building == null)
            {
                Debug.LogError("Building not initialized");
                return;
            }

            try
            {
                _building = _building.AddFloor(floor);
                CreateFloorController(floor);
                UpdateVisualization();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add floor: {ex.Message}");
            }
        }

        public void RemoveFloor(Guid floorId)
        {
            if (_building == null)
            {
                Debug.LogError("Building not initialized");
                return;
            }

            try
            {
                _building = _building.RemoveFloor(floorId);
                RemoveFloorController(floorId);
                UpdateVisualization();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove floor: {ex.Message}");
            }
        }

        public void SetMaintenanceMode(bool inMaintenance)
        {
            if (_building == null)
            {
                Debug.LogError("Building not initialized");
                return;
            }

            try
            {
                _building = _building.SetMaintenanceMode(inMaintenance);
                UpdateVisualization();
                
                // Notify UI
                if (_buildingView != null)
                {
                    _buildingView.ShowMaintenanceMode(inMaintenance);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set maintenance mode: {ex.Message}");
            }
        }

        public async Task<EnergySimulationResult> RunEnergySimulationAsync(TimeSpan period, SimulationParameters parameters)
        {
            if (_simulationService == null || _building == null)
            {
                Debug.LogError("Services not initialized");
                return null;
            }

            try
            {
                var result = await _simulationService.SimulateEnergyConsumptionAsync(_building, period, parameters);
                
                if (result.IsSuccess && _showEnergyOverlays)
                {
                    UpdateEnergyVisualization(result);
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Energy simulation failed: {ex.Message}");
                return null;
            }
        }

        public async Task<Building> GetBuildingDataAsync()
        {
            // This would load building data from persistence layer
            return _building;
        }

        public FloorController GetFloorController(Guid floorId)
        {
            return _floorControllers.Find(fc => fc.Floor.Id == floorId);
        }

        public RoomController GetRoomController(Guid roomId)
        {
            return _roomControllers.Find(rc => rc.Room.Id == roomId);
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
            if (_buildingConfig == null)
            {
                throw new InvalidOperationException("Building configuration is required");
            }

            if (_serviceLocator == null)
            {
                _serviceLocator = FindObjectOfType<ServiceLocator>();
                if (_serviceLocator == null)
                {
                    throw new InvalidOperationException("Service locator is required");
                }
            }
        }

        private void InitializeBuilding()
        {
            if (_buildingConfig.BuildingData != null)
            {
                _building = _buildingConfig.BuildingData;
            }
            else
            {
                // Create default building
                _building = CreateDefaultBuilding();
            }

            // Initialize child controllers
            InitializeChildControllers();
        }

        private void ResolveDependencies()
        {
            _simulationService = _serviceLocator.GetService<IBuildingSimulationService>();
            _analyticsService = _serviceLocator.GetService<IDataAnalyticsService>();
            _dataCollectionService = _serviceLocator.GetService<IDataCollectionService>();

            if (_simulationService == null)
                Debug.LogWarning("Building simulation service not found");
            if (_analyticsService == null)
                Debug.LogWarning("Analytics service not found");
            if (_dataCollectionService == null)
                Debug.LogWarning("Data collection service not found");
        }

        private async void StartDataCollection()
        {
            if (_dataCollectionService == null) return;

            try
            {
                // Start real-time data collection for all sensors
                var allSensors = _building.GetAllSensors();
                var sensorIds = new List<Guid>();

                foreach (var sensor in allSensors)
                {
                    sensorIds.Add(sensor.Id);
                }

                if (sensorIds.Count > 0)
                {
                    await _dataCollectionService.StartDataStreamAsync(sensorIds, OnSensorDataReceived);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start data collection: {ex.Message}");
            }
        }

        private async void StartSimulation()
        {
            if (_simulationService == null) return;

            try
            {
                // Start real-time simulation
                var parameters = new SimulationParameters
                {
                    TimeStep = TimeSpan.FromMinutes(5),
                    EnableRealTimeData = true,
                    EnableLogging = true
                };

                await _simulationService.StartRealTimeSimulationAsync(_building.Id, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start simulation: {ex.Message}");
            }
        }

        private async Task InitializeComponents()
        {
            // Initialize building view
            if (_buildingView != null)
            {
                await _buildingView.Initialize(_building);
            }

            // Initialize floor controllers
            foreach (var floor in _building.Floors)
            {
                CreateFloorController(floor);
            }

            // Initialize room controllers
            foreach (var floor in _building.Floors)
            {
                foreach (var room in floor.Rooms)
                {
                    CreateRoomController(room);
                }
            }

            // Initialize equipment controllers
            foreach (var equipment in _building.GetAllEquipment())
            {
                CreateEquipmentController(equipment);
            }

            // Initialize sensor controllers
            foreach (var sensor in _building.GetAllSensors())
            {
                CreateSensorController(sensor);
            }
        }

        private void InitializeChildControllers()
        {
            // Get existing child controllers
            _floorControllers = new List<FloorController>(GetComponentsInChildren<FloorController>());
            _roomControllers = new List<RoomController>(GetComponentsInChildren<RoomController>());
            _equipmentControllers = new List<EquipmentController>(GetComponentsInChildren<EquipmentController>());
            _sensorControllers = new List<SensorController>(GetComponentsInChildren<SensorController>());
        }

        private void CreateFloorController(Floor floor)
        {
            var floorController = FindObjectOfType<FloorController>();
            if (floorController == null)
            {
                var floorPrefab = _buildingConfig.FloorPrefab;
                if (floorPrefab != null)
                {
                    var floorObject = Instantiate(floorPrefab, _buildingTransform);
                    floorController = floorObject.GetComponent<FloorController>();
                }
            }

            if (floorController != null)
            {
                floorController.Initialize(floor, _serviceLocator);
                _floorControllers.Add(floorController);
            }
        }

        private void CreateRoomController(Room room)
        {
            var roomController = FindObjectOfType<RoomController>();
            if (roomController == null)
            {
                var roomPrefab = _buildingConfig.RoomPrefab;
                if (roomPrefab != null)
                {
                    var roomObject = Instantiate(roomPrefab, _buildingTransform);
                    roomController = roomObject.GetComponent<RoomController>();
                }
            }

            if (roomController != null)
            {
                roomController.Initialize(room, _serviceLocator);
                _roomControllers.Add(roomController);
            }
        }

        private void CreateEquipmentController(Equipment equipment)
        {
            var equipmentController = FindObjectOfType<EquipmentController>();
            if (equipmentController == null)
            {
                var equipmentPrefab = _buildingConfig.GetEquipmentPrefab(equipment.Type);
                if (equipmentPrefab != null)
                {
                    var equipmentObject = Instantiate(equipmentPrefab, _buildingTransform);
                    equipmentController = equipmentObject.GetComponent<EquipmentController>();
                }
            }

            if (equipmentController != null)
            {
                equipmentController.Initialize(equipment, _serviceLocator);
                _equipmentControllers.Add(equipmentController);
            }
        }

        private void CreateSensorController(Sensor sensor)
        {
            var sensorController = FindObjectOfType<SensorController>();
            if (sensorController == null)
            {
                var sensorPrefab = _buildingConfig.GetSensorPrefab(sensor.Type);
                if (sensorPrefab != null)
                {
                    var sensorObject = Instantiate(sensorPrefab, _buildingTransform);
                    sensorController = sensorObject.GetComponent<SensorController>();
                }
            }

            if (sensorController != null)
            {
                sensorController.Initialize(sensor, _serviceLocator);
                _sensorControllers.Add(sensorController);
            }
        }

        private void RemoveFloorController(Guid floorId)
        {
            var controller = GetFloorController(floorId);
            if (controller != null)
            {
                _floorControllers.Remove(controller);
                if (Application.isPlaying)
                    Destroy(controller.gameObject);
                else
                    DestroyImmediate(controller.gameObject);
            }
        }

        private Building CreateDefaultBuilding()
        {
            var buildingId = Guid.NewGuid();
            var metadata = new DigitalTwin.Core.Metadata.BuildingMetadata(
                "Default Building",
                DigitalTwin.Core.Metadata.BuildingCategory.Commercial,
                "Unknown Architect",
                DateTime.UtcNow.Year,
                10000,
                "Building Owner",
                "contact@building.com",
                new DigitalTwin.Core.Metadata.GeoLocation(0, 0),
                DigitalTwin.Core.Metadata.BuildingCertification.None
            );

            return new Building(buildingId, "Default Building", "123 Main St", metadata, DateTime.UtcNow);
        }

        private void UpdateRealTimeVisualization()
        {
            if (_buildingView != null)
            {
                _buildingView.UpdateRealTimeData();
            }

            // Update child controllers
            foreach (var controller in _floorControllers)
            {
                controller.UpdateRealTimeVisualization();
            }

            foreach (var controller in _roomControllers)
            {
                controller.UpdateRealTimeVisualization();
            }

            foreach (var controller in _equipmentControllers)
            {
                controller.UpdateRealTimeVisualization();
            }

            foreach (var controller in _sensorControllers)
            {
                controller.UpdateRealTimeVisualization();
            }
        }

        private void UpdateVisualization()
        {
            if (_buildingView != null)
            {
                _buildingView.UpdateBuilding(_building);
            }
        }

        private void UpdateEnergyVisualization(EnergySimulationResult result)
        {
            if (_buildingView != null)
            {
                _buildingView.UpdateEnergyData(result);
            }
        }

        private void HandleUserInput()
        {
            // Handle mouse clicks for selection
            if (Input.GetMouseButtonDown(0))
            {
                HandleBuildingClick();
            }

            // Handle keyboard shortcuts
            HandleKeyboardInput();
        }

        private void HandleBuildingClick()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var buildingView = hit.collider.GetComponent<BuildingView>();
                if (buildingView != null)
                {
                    OnBuildingSelected();
                }
            }
        }

        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                SetMaintenanceMode(!_building.Status == DigitalTwin.Core.Entities.BuildingStatus.Maintenance);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                _ = RunEnergySimulationAsync(TimeSpan.FromHours(24), new SimulationParameters());
            }
        }

        private void OnBuildingSelected()
        {
            Debug.Log($"Building selected: {_building.Name}");
            
            // Show building details UI
            if (_buildingView != null)
            {
                _buildingView.ShowBuildingDetails(_building);
            }
        }

        private void OnSensorDataReceived(SensorReading reading)
        {
            // Find the sensor controller and update it
            var controller = GetSensorController(reading.SensorId);
            if (controller != null)
            {
                controller.UpdateSensorReading(reading);
            }

            // Update building view with new data
            if (_showEnvironmentalData && _buildingView != null)
            {
                _buildingView.UpdateSensorData(reading);
            }
        }

        private void Cleanup()
        {
            // Stop data collection
            if (_dataCollectionService != null)
            {
                var allSensorIds = _building?.GetAllSensors().Select(s => s.Id).ToList() ?? new List<Guid>();
                if (allSensorIds.Count > 0)
                {
                    _ = _dataCollectionService.StopDataStreamAsync(allSensorIds);
                }
            }

            // Stop simulation
            if (_simulationService != null && _building != null)
            {
                _ = _simulationService.StopRealTimeSimulationAsync(_building.Id);
            }
        }
    }
}