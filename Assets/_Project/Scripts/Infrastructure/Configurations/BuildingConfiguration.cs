using System;
using System.Collections.Generic;
using UnityEngine;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Metadata;

namespace DigitalTwin.Infrastructure.Configurations
{
    /// <summary>
    /// Building Configuration ScriptableObject
    /// 
    /// Architectural Intent:
    /// - Provides immutable configuration data for building setup
    /// - Enables data-driven building creation and customization
    /// - Supports asset references for prefabs and materials
    /// - Maintains separation between data and behavior
    /// 
    /// Key Design Decisions:
    /// 1. ScriptableObject for data-driven configuration
    /// 2. Asset references for Unity-specific resources
    /// 3. Default values for robust fallback behavior
    /// 4. Validation methods for data integrity
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingConfiguration", menuName = "Digital Twin/Building Configuration")]
    public class BuildingConfiguration : ScriptableObject
    {
        [Header("Building Data")]
        [SerializeField] private Building _buildingData;
        [SerializeField] private string _buildingName = "Default Building";
        [SerializeField] private string _address = "123 Main Street";
        [SerializeField] private BuildingCategory _category = BuildingCategory.Commercial;
        
        [Header("Physical Properties")]
        [SerializeField] private int _yearBuilt = 2020;
        [SerializeField] private decimal _squareFootage = 10000;
        [SerializeField] private string _owner = "Building Owner";
        [SerializeField] private string _contactInfo = "contact@building.com";
        
        [Header("Location")]
        [SerializeField] private decimal _latitude = 40.7128m;
        [SerializeField] private decimal _longitude = -74.0060m;
        [SerializeField] private decimal _altitude = 0m;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject _buildingPrefab;
        [SerializeField] private GameObject _floorPrefab;
        [SerializeField] private GameObject _roomPrefab;
        [SerializeField] private List<EquipmentPrefabMapping> _equipmentPrefabs = new List<EquipmentPrefabMapping>();
        [SerializeField] private List<SensorPrefabMapping> _sensorPrefabs = new List<SensorPrefabMapping>();
        
        [Header("Materials")]
        [SerializeField] private Material _defaultBuildingMaterial;
        [SerializeField] private Material _energyHeatmapMaterial;
        [SerializeField] private Material _temperatureHeatmapMaterial;
        
        [Header("Visualization Settings")]
        [SerializeField] private bool _enableRealTimeVisualization = true;
        [SerializeField] private bool _showEnergyOverlays = true;
        [SerializeField] private bool _showEnvironmentalData = true;
        [SerializeField] private float _updateFrequency = 1.0f;
        
        [Header("Performance Settings")]
        [SerializeField] private int _maxSimultaneousDataUpdates = 100;
        [SerializeField] private float _dataUpdateInterval = 0.1f;
        [SerializeField] private bool _enableBatching = true;

        // Properties
        public Building BuildingData => _buildingData;
        public string BuildingName => _buildingName;
        public string Address => _address;
        public BuildingCategory Category => _category;
        public int YearBuilt => _yearBuilt;
        public decimal SquareFootage => _squareFootage;
        public string Owner => _owner;
        public string ContactInfo => _contactInfo;
        public decimal Latitude => _latitude;
        public decimal Longitude => _longitude;
        public decimal Altitude => _altitude;
        
        public GameObject BuildingPrefab => _buildingPrefab;
        public GameObject FloorPrefab => _floorPrefab;
        public GameObject RoomPrefab => _roomPrefab;
        public List<EquipmentPrefabMapping> EquipmentPrefabs => _equipmentPrefabs;
        public List<SensorPrefabMapping> SensorPrefabs => _sensorPrefabs;
        
        public Material DefaultBuildingMaterial => _defaultBuildingMaterial;
        public Material EnergyHeatmapMaterial => _energyHeatmapMaterial;
        public Material TemperatureHeatmapMaterial => _temperatureHeatmapMaterial;
        
        public bool EnableRealTimeVisualization => _enableRealTimeVisualization;
        public bool ShowEnergyOverlays => _showEnergyOverlays;
        public bool ShowEnvironmentalData => _showEnvironmentalData;
        public float UpdateFrequency => _updateFrequency;
        
        public int MaxSimultaneousDataUpdates => _maxSimultaneousDataUpdates;
        public float DataUpdateInterval => _dataUpdateInterval;
        public bool EnableBatching => _enableBatching;

        // Unity Methods
        private void OnValidate()
        {
            ValidateConfiguration();
        }

        private void OnEnable()
        {
            EnsureBuildingDataExists();
        }

        // Public Methods
        public GameObject GetEquipmentPrefab(EquipmentType equipmentType)
        {
            var mapping = _equipmentPrefabs.Find(m => m.EquipmentType == equipmentType);
            return mapping?.Prefab;
        }

        public GameObject GetSensorPrefab(SensorType sensorType)
        {
            var mapping = _sensorPrefabs.Find(m => m.SensorType == sensorType);
            return mapping?.Prefab;
        }

        public BuildingMetadata CreateBuildingMetadata()
        {
            var location = new GeoLocation(_latitude, _longitude, _altitude);
            return new BuildingMetadata(
                "Digital Twin Building",
                _category,
                "Digital Twin Architect",
                _yearBuilt,
                _squareFootage,
                _owner,
                _contactInfo,
                location,
                BuildingCertification.None
            );
        }

        public Building CreateBuildingFromConfiguration()
        {
            // If building data already exists, return it
            if (_buildingData != null)
            {
                return _buildingData;
            }

            // Create building from configuration
            var buildingId = Guid.NewGuid();
            var metadata = CreateBuildingMetadata();
            
            return new Building(
                buildingId,
                _buildingName,
                _address,
                metadata,
                new DateTime(_yearBuilt, 1, 1)
            );
        }

        // Private Methods
        private void ValidateConfiguration()
        {
            // Validate basic properties
            if (_yearBuilt < 1800 || _yearBuilt > DateTime.UtcNow.Year + 10)
            {
                Debug.LogWarning($"Invalid year built: {_yearBuilt}. Using current year.");
                _yearBuilt = DateTime.UtcNow.Year;
            }

            if (_squareFootage <= 0)
            {
                Debug.LogWarning($"Invalid square footage: {_squareFootage}. Using default value.");
                _squareFootage = 10000;
            }

            // Validate coordinates
            if (_latitude < -90 || _latitude > 90)
            {
                Debug.LogWarning($"Invalid latitude: {_latitude}. Using default value.");
                _latitude = 40.7128m;
            }

            if (_longitude < -180 || _longitude > 180)
            {
                Debug.LogWarning($"Invalid longitude: {_longitude}. Using default value.");
                _longitude = -74.0060m;
            }

            // Validate performance settings
            if (_maxSimultaneousDataUpdates <= 0)
            {
                Debug.LogWarning($"Invalid max data updates: {_maxSimultaneousDataUpdates}. Using default value.");
                _maxSimultaneousDataUpdates = 100;
            }

            if (_dataUpdateInterval <= 0)
            {
                Debug.LogWarning($"Invalid data update interval: {_dataUpdateInterval}. Using default value.");
                _dataUpdateInterval = 0.1f;
            }

            if (_updateFrequency <= 0)
            {
                Debug.LogWarning($"Invalid update frequency: {_updateFrequency}. Using default value.");
                _updateFrequency = 1.0f;
            }
        }

        private void EnsureBuildingDataExists()
        {
            if (_buildingData == null)
            {
                _buildingData = CreateBuildingFromConfiguration();
                Debug.Log("Created building data from configuration");
            }
        }
    }

    /// <summary>
    /// Equipment Prefab Mapping
    /// </summary>
    [Serializable]
    public class EquipmentPrefabMapping
    {
        [SerializeField] private EquipmentType _equipmentType;
        [SerializeField] private GameObject _prefab;

        public EquipmentType EquipmentType => _equipmentType;
        public GameObject Prefab => _prefab;
    }

    /// <summary>
    /// Sensor Prefab Mapping
    /// </summary>
    [Serializable]
    public class SensorPrefabMapping
    {
        [SerializeField] private SensorType _sensorType;
        [SerializeField] private GameObject _prefab;

        public SensorType SensorType => _sensorType;
        public GameObject Prefab => _prefab;
    }
}