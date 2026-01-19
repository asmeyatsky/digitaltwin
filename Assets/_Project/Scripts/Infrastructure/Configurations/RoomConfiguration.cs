using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Metadata;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Infrastructure.Configurations
{
    /// <summary>
    /// Room Configuration ScriptableObject
    /// 
    /// Architectural Intent:
    /// - Provides configuration data for room setup and behavior
    /// - Enables data-driven room creation and customization
    /// - Supports room templates and common configurations
    /// - Maintains separation between room data and logic
    /// 
    /// Key Design Decisions:
    /// 1. ScriptableObject for reusable room configurations
    /// 2. Template system for common room types
    /// 3. Asset references for room-specific resources
    /// 4. Validation methods for room constraints
    /// </summary>
    [CreateAssetMenu(fileName = "RoomConfiguration", menuName = "Digital Twin/Room Configuration")]
    public class RoomConfiguration : ScriptableObject
    {
        [Header("Room Template")]
        [SerializeField] private RoomTemplate _template = RoomTemplate.Custom;
        [SerializeField] private string _roomName = "Default Room";
        [SerializeField] private RoomType _roomType = RoomType.Office;
        [SerializeField] private string _description = "Default room configuration";
        
        [Header("Physical Properties")]
        [SerializeField] private int _roomNumber = 1;
        [SerializeField] private decimal _area = 50m;
        [SerializeField] private int _maxOccupancy = 10;
        [SerializeField] private RoomMaterial _material = RoomMaterial.Drywall;
        
        [Header("Environmental Settings")]
        [SerializeField] private bool _hasWindows = true;
        [SerializeField] private int _windowCount = 2;
        [SerializeField] private VentilationType _ventilation = VentilationType.Mechanical;
        [SerializeField] private AccessibilityFeatures _accessibility = AccessibilityFeatures.None;
        
        [Header("Environmental Targets")]
        [SerializeField] private float _targetTemperature = 22f;
        [SerializeField] private float _targetHumidity = 45f;
        [SerializeField] private float _targetLightLevel = 500f;
        [SerializeField] private float _targetAirQuality = 50f;
        [SerializeField] private float _targetNoiseLevel = 40f;
        
        [Header("Default Equipment")]
        [SerializeField] private List<EquipmentTemplate> _defaultEquipment = new List<EquipmentTemplate>();
        [SerializeField] private List<SensorTemplate> _defaultSensors = new List<SensorTemplate>();
        
        [Header("Visualization")]
        [SerializeField] private GameObject _roomPrefab;
        [SerializeField] private Material _defaultMaterial;
        [SerializeField] private Material _occupiedMaterial;
        [SerializeField] private Material _vacantMaterial;
        
        [Header("Behavior Settings")]
        [SerializeField] private bool _enableAutoLighting = true;
        [SerializeField] private bool _enableAutoHVAC = true;
        [SerializeField] private bool _enableOccupancyDetection = true;
        [SerializeField] private float _occupancyTimeoutMinutes = 15f;

        // Properties
        public RoomTemplate Template => _template;
        public string RoomName => _roomName;
        public RoomType RoomType => _roomType;
        public string Description => _description;
        public int RoomNumber => _roomNumber;
        public decimal Area => _area;
        public int MaxOccupancy => _maxOccupancy;
        public RoomMaterial Material => _material;
        
        public bool HasWindows => _hasWindows;
        public int WindowCount => _hasWindows ? _windowCount : 0;
        public VentilationType Ventilation => _ventilation;
        public AccessibilityFeatures Accessibility => _accessibility;
        
        public float TargetTemperature => _targetTemperature;
        public float TargetHumidity => _targetHumidity;
        public float TargetLightLevel => _targetLightLevel;
        public float TargetAirQuality => _targetAirQuality;
        public float TargetNoiseLevel => _targetNoiseLevel;
        
        public List<EquipmentTemplate> DefaultEquipment => _defaultEquipment;
        public List<SensorTemplate> DefaultSensors => _defaultSensors;
        
        public GameObject RoomPrefab => _roomPrefab;
        public Material DefaultMaterial => _defaultMaterial;
        public Material OccupiedMaterial => _occupiedMaterial;
        public Material VacantMaterial => _vacantMaterial;
        
        public bool EnableAutoLighting => _enableAutoLighting;
        public bool EnableAutoHVAC => _enableAutoHVAC;
        public bool EnableOccupancyDetection => _enableOccupancyDetection;
        public float OccupancyTimeoutMinutes => _occupancyTimeoutMinutes;

        // Unity Methods
        private void OnValidate()
        {
            ValidateConfiguration();
        }

        private void OnEnable()
        {
            ApplyTemplateSettings();
        }

        // Public Methods
        public Room CreateRoomFromConfiguration(Guid roomId)
        {
            var metadata = CreateRoomMetadata();
            var currentConditions = CreateDefaultEnvironmentalConditions();
            
            var room = new Room(
                roomId,
                _roomNumber,
                _roomName,
                _roomType,
                _area,
                _maxOccupancy,
                metadata
            );

            // Set initial environmental conditions
            return room.UpdateEnvironmentalConditions(currentConditions);
        }

        public List<Equipment> CreateDefaultEquipment(Guid roomId)
        {
            var equipmentList = new List<Equipment>();
            
            foreach (var equipmentTemplate in _defaultEquipment)
            {
                var equipmentId = Guid.NewGuid();
                var metadata = CreateEquipmentMetadata(equipmentTemplate);
                
                var equipment = new Equipment(
                    equipmentId,
                    equipmentTemplate.Name,
                    equipmentTemplate.Model,
                    equipmentTemplate.Manufacturer,
                    equipmentTemplate.Type,
                    equipmentTemplate.PowerConsumption,
                    metadata,
                    DateTime.UtcNow
                );
                
                equipmentList.Add(equipment);
            }
            
            return equipmentList;
        }

        public List<Sensor> CreateDefaultSensors(Guid roomId)
        {
            var sensorList = new List<Sensor>();
            
            foreach (var sensorTemplate in _defaultSensors)
            {
                var sensorId = Guid.NewGuid();
                var metadata = CreateSensorMetadata(sensorTemplate);
                
                var sensor = new Sensor(
                    sensorId,
                    sensorTemplate.Name,
                    sensorTemplate.Model,
                    sensorTemplate.Manufacturer,
                    sensorTemplate.Type,
                    metadata,
                    DateTime.UtcNow,
                    sensorTemplate.ReadingFrequency
                );
                
                sensorList.Add(sensor);
            }
            
            return sensorList;
        }

        public EnvironmentalConditions CreateTargetEnvironmentalConditions()
        {
            var temperature = DigitalTwin.Core.ValueObjects.Temperature.FromCelsius(_targetTemperature);
            
            return new EnvironmentalConditions(
                temperature,
                _targetHumidity,
                _targetLightLevel,
                _targetAirQuality,
                _targetNoiseLevel,
                DateTime.UtcNow
            );
        }

        // Private Methods
        private void ValidateConfiguration()
        {
            // Validate basic properties
            if (_area <= 0)
            {
                Debug.LogWarning($"Invalid room area: {_area}. Using default value.");
                _area = 50m;
            }

            if (_maxOccupancy <= 0)
            {
                Debug.LogWarning($"Invalid max occupancy: {_maxOccupancy}. Using default value.");
                _maxOccupancy = 10;
            }

            if (_roomNumber <= 0)
            {
                Debug.LogWarning($"Invalid room number: {_roomNumber}. Using default value.");
                _roomNumber = 1;
            }

            // Validate environmental targets
            if (_targetTemperature < -50 || _targetTemperature > 100)
            {
                Debug.LogWarning($"Invalid target temperature: {_targetTemperature}°C. Using default value.");
                _targetTemperature = 22f;
            }

            if (_targetHumidity < 0 || _targetHumidity > 100)
            {
                Debug.LogWarning($"Invalid target humidity: {_targetHumidity}%. Using default value.");
                _targetHumidity = 45f;
            }

            if (_targetLightLevel < 0)
            {
                Debug.LogWarning($"Invalid target light level: {_targetLightLevel} lux. Using default value.");
                _targetLightLevel = 500f;
            }

            if (_targetAirQuality < 0 || _targetAirQuality > 500)
            {
                Debug.LogWarning($"Invalid target air quality: {_targetAirQuality}. Using default value.");
                _targetAirQuality = 50f;
            }

            if (_targetNoiseLevel < 0)
            {
                Debug.LogWarning($"Invalid target noise level: {_targetNoiseLevel} dB. Using default value.");
                _targetNoiseLevel = 40f;
            }

            // Validate window count
            if (_windowCount < 0)
            {
                Debug.LogWarning($"Invalid window count: {_windowCount}. Using default value.");
                _windowCount = _hasWindows ? 2 : 0;
            }

            // Validate timeout
            if (_occupancyTimeoutMinutes <= 0)
            {
                Debug.LogWarning($"Invalid occupancy timeout: {_occupancyTimeoutMinutes} minutes. Using default value.");
                _occupancyTimeoutMinutes = 15f;
            }
        }

        private void ApplyTemplateSettings()
        {
            switch (_template)
            {
                case RoomTemplate.Office:
                    ApplyOfficeTemplate();
                    break;
                case RoomTemplate.Conference:
                    ApplyConferenceTemplate();
                    break;
                case RoomTemplate.ServerRoom:
                    ApplyServerRoomTemplate();
                    break;
                case RoomTemplate.Laboratory:
                    ApplyLaboratoryTemplate();
                    break;
                case RoomTemplate.Kitchen:
                    ApplyKitchenTemplate();
                    break;
                case RoomTemplate.Custom:
                default:
                    // Keep custom settings
                    break;
            }
        }

        private void ApplyOfficeTemplate()
        {
            _roomType = RoomType.Office;
            _area = 25m;
            _maxOccupancy = 4;
            _material = RoomMaterial.Drywall;
            _hasWindows = true;
            _windowCount = 2;
            _ventilation = VentilationType.Mechanical;
            _targetTemperature = 22f;
            _targetHumidity = 45f;
            _targetLightLevel = 500f;
            _enableAutoLighting = true;
            _enableAutoHVAC = true;
            _enableOccupancyDetection = true;
            
            // Add default office equipment
            _defaultEquipment.Clear();
            _defaultEquipment.Add(new EquipmentTemplate
            {
                Name = "Office Computer",
                Type = EquipmentType.Computer,
                PowerConsumption = 150m
            });
            
            // Add default office sensors
            _defaultSensors.Clear();
            _defaultSensors.Add(new SensorTemplate
            {
                Name = "Temperature Sensor",
                Type = SensorType.Temperature
            });
            _defaultSensors.Add(new SensorTemplate
            {
                Name = "Motion Sensor",
                Type = SensorType.Motion
            });
        }

        private void ApplyConferenceTemplate()
        {
            _roomType = RoomType.Conference;
            _area = 50m;
            _maxOccupancy = 12;
            _material = RoomMaterial.Drywall;
            _hasWindows = true;
            _windowCount = 4;
            _ventilation = VentilationType.Mechanical;
            _targetTemperature = 21f;
            _targetHumidity = 45f;
            _targetLightLevel = 700f;
            _enableAutoLighting = true;
            _enableAutoHVAC = true;
            _enableOccupancyDetection = true;
            
            // Add conference-specific equipment
            _defaultEquipment.Clear();
            _defaultEquipment.Add(new EquipmentTemplate
            {
                Name = "Projector",
                Type = EquipmentType.Projector,
                PowerConsumption = 300m
            });
            _defaultEquipment.Add(new EquipmentTemplate
            {
                Name = "Video Conference System",
                Type = EquipmentType.VideoConference,
                PowerConsumption = 100m
            });
        }

        private void ApplyServerRoomTemplate()
        {
            _roomType = RoomType.ServerRoom;
            _area = 40m;
            _maxOccupancy = 2;
            _material = RoomMaterial.Concrete;
            _hasWindows = false;
            _windowCount = 0;
            _ventilation = VentilationType.Mechanical;
            _targetTemperature = 18f;
            _targetHumidity = 40f;
            _targetLightLevel = 300f;
            _enableAutoLighting = true;
            _enableAutoHVAC = true;
            _enableOccupancyDetection = false;
            
            // Add server room equipment
            _defaultEquipment.Clear();
            _defaultEquipment.Add(new EquipmentTemplate
            {
                Name = "Server Rack",
                Type = EquipmentType.Server,
                PowerConsumption = 2000m
            });
            _defaultEquipment.Add(new EquipmentTemplate
            {
                Name = "Network Switch",
                Type = EquipmentType.NetworkSwitch,
                PowerConsumption = 100m
            });
            _defaultEquipment.Add(new EquipmentTemplate
            {
                Name = "UPS System",
                Type = EquipmentType.UPS,
                PowerConsumption = 50m
            });
        }

        private void ApplyLaboratoryTemplate()
        {
            _roomType = RoomType.Laboratory;
            _area = 60m;
            _maxOccupancy = 8;
            _material = RoomMaterial.Tile;
            _hasWindows = true;
            _windowCount = 2;
            _ventilation = VentilationType.Mechanical;
            _targetTemperature = 20f;
            _targetHumidity = 50f;
            _targetLightLevel = 800f;
            _accessibility = AccessibilityFeatures.FullAccessibility;
            
            // Add lab-specific sensors
            _defaultSensors.Clear();
            _defaultSensors.Add(new SensorTemplate
            {
                Name = "Temperature Sensor",
                Type = SensorType.Temperature
            });
            _defaultSensors.Add(new SensorTemplate
            {
                Name = "Humidity Sensor",
                Type = SensorType.Humidity
            });
            _defaultSensors.Add(new SensorTemplate
            {
                Name = "Air Quality Sensor",
                Type = SensorType.AirQuality
            });
        }

        private void ApplyKitchenTemplate()
        {
            _roomType = RoomType.Kitchen;
            _area = 30m;
            _maxOccupancy = 6;
            _material = RoomMaterial.Tile;
            _hasWindows = true;
            _windowCount = 1;
            _ventilation = VentilationType.Mechanical;
            _targetTemperature = 20f;
            _targetHumidity = 50f;
            _targetLightLevel = 600f;
            _accessibility = AccessibilityFeatures.WheelchairAccessible;
            
            // Add kitchen equipment
            _defaultEquipment.Clear();
            _defaultEquipment.Add(new EquipmentTemplate
            {
                Name = "Refrigerator",
                Type = EquipmentType.Refrigerator,
                PowerConsumption = 500m
            });
            _defaultEquipment.Add(new EquipmentTemplate
            {
                Name = "Dishwasher",
                Type = EquipmentType.Dishwasher,
                PowerConsumption = 1200m
            });
        }

        private RoomMetadata CreateRoomMetadata()
        {
            return new RoomMetadata(
                _description,
                _material,
                _hasWindows,
                _windowCount,
                _ventilation,
                _accessibility
            );
        }

        private EquipmentMetadata CreateEquipmentMetadata(EquipmentTemplate template)
        {
            var warranty = new WarrantyInfo(
                DateTime.UtcNow,
                DateTime.UtcNow.AddYears(5),
                "Default Manufacturer",
                WarrantyType.Full
            );

            return new EquipmentMetadata(
                $"Standard {template.Type} equipment",
                "STD-001",
                "1.0",
                "Default Manufacturer",
                GetEquipmentCategory(template.Type),
                EnergyEfficiencyClass.B,
                warranty,
                ComplianceStandards.UL
            );
        }

        private SensorMetadata CreateSensorMetadata(SensorTemplate template)
        {
            var operatingRange = new OperatingRange(
                GetSensorMinValue(template.Type),
                GetSensorMaxValue(template.Type),
                GetSensorUnit(template.Type)
            );

            return new SensorMetadata(
                $"Standard {template.Type} sensor",
                "I2C",
                GetSensorCategory(template.Type),
                95m,
                operatingRange,
                CalibrationInterval.Monthly,
                "Default Sensor Manufacturer"
            );
        }

        private EnvironmentalConditions CreateDefaultEnvironmentalConditions()
        {
            var temperature = DigitalTwin.Core.ValueObjects.Temperature.FromCelsius(_targetTemperature);
            
            return new EnvironmentalConditions(
                temperature,
                _targetHumidity,
                _targetLightLevel,
                _targetAirQuality,
                _targetNoiseLevel,
                DateTime.UtcNow
            );
        }

        // Helper methods for template generation
        private EquipmentCategory GetEquipmentCategory(EquipmentType type)
        {
            return type switch
            {
                EquipmentType.HVAC => EquipmentCategory.HVAC,
                EquipmentType.Lighting => EquipmentCategory.Lighting,
                EquipmentType.Computer => EquipmentCategory.Computing,
                EquipmentType.Server => EquipmentCategory.Computing,
                EquipmentType.NetworkSwitch => EquipmentCategory.Communication,
                EquipmentType.Refrigerator => EquipmentCategory.Kitchen,
                _ => EquipmentCategory.Custom
            };
        }

        private SensorCategory GetSensorCategory(SensorType type)
        {
            return type switch
            {
                SensorType.Temperature => SensorCategory.Environmental,
                SensorType.Humidity => SensorCategory.Environmental,
                SensorType.Motion => SensorCategory.Safety,
                SensorType.Light => SensorCategory.Environmental,
                SensorType.Power => SensorCategory.Energy,
                SensorType.AirQuality => SensorCategory.Environmental,
                _ => SensorCategory.Custom
            };
        }

        private decimal GetSensorMinValue(SensorType type)
        {
            return type switch
            {
                SensorType.Temperature => -40m,
                SensorType.Humidity => 0m,
                SensorType.Motion => 0m,
                SensorType.Light => 0m,
                SensorType.Power => 0m,
                SensorType.AirQuality => 0m,
                _ => 0m
            };
        }

        private decimal GetSensorMaxValue(SensorType type)
        {
            return type switch
            {
                SensorType.Temperature => 80m,
                SensorType.Humidity => 100m,
                SensorType.Motion => 1m,
                SensorType.Light => 2000m,
                SensorType.Power => 10000m,
                SensorType.AirQuality => 500m,
                _ => 100m
            };
        }

        private string GetSensorUnit(SensorType type)
        {
            return type switch
            {
                SensorType.Temperature => "°C",
                SensorType.Humidity => "%",
                SensorType.Motion => "",
                SensorType.Light => "lux",
                SensorType.Power => "W",
                SensorType.AirQuality => "AQI",
                _ => ""
            };
        }
    }

    /// <summary>
    /// Room Templates
    /// </summary>
    public enum RoomTemplate
    {
        Custom,
        Office,
        Conference,
        ServerRoom,
        Laboratory,
        Kitchen,
        Storage,
        Restroom,
        Lobby
    }

    /// <summary>
    /// Equipment Template
    /// </summary>
    [Serializable]
    public class EquipmentTemplate
    {
        public string Name;
        public EquipmentType Type;
        public string Model = "Standard Model";
        public string Manufacturer = "Default Manufacturer";
        public decimal PowerConsumption = 100m;
    }

    /// <summary>
    /// Sensor Template
    /// </summary>
    [Serializable]
    public class SensorTemplate
    {
        public string Name;
        public SensorType Type;
        public string Model = "Standard Model";
        public string Manufacturer = "Default Manufacturer";
        public TimeSpan ReadingFrequency = TimeSpan.FromMinutes(5);
    }
}