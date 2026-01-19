using System;
using System.Collections.Generic;
using UnityEngine;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Infrastructure.UnityAdapters
{
    /// <summary>
    /// Equipment Controller MonoBehaviour Adapter
    /// 
    /// Architectural Intent:
    /// - Bridges Unity's component system with equipment domain logic
    /// - Handles equipment-specific Unity lifecycle and visualization
    /// - Provides thin adapter layer for equipment operations
    /// - Manages equipment status visualization and animations
    /// 
    /// Key Design Decisions:
    /// 1. MonoBehaviour is thin adapter, domain logic in services
    /// 2. Component-based architecture for equipment visualization
    /// 3. Event-driven updates from sensor data
    /// 4. Efficient state management and visual feedback
    /// </summary>
    public class EquipmentController : MonoBehaviour
    {
        [Header("Equipment Configuration")]
        [SerializeField] private EquipmentConfiguration _equipmentConfig;
        [SerializeField] private EquipmentView _equipmentView;
        [SerializeField] private Animator _equipmentAnimator;
        [SerializeField] private Renderer _equipmentRenderer;
        [SerializeField] private Light _statusLight;
        
        [Header("Status Visualization")]
        [SerializeField] private bool _showStatusLight = true;
        [SerializeField] private bool _showPerformanceMetrics = true;
        [SerializeField] private bool _enableStatusAnimations = true;
        
        [Header("Performance Display")]
        [SerializeField] private GameObject _efficiencyDisplay;
        [SerializeField] private GameObject _powerConsumptionDisplay;
        [SerializeField] private GameObject _uptimeDisplay;

        // Private fields
        private Equipment _equipment;
        private ServiceLocator _serviceLocator;
        private IDataCollectionService _dataCollectionService;
        private List<SensorController> _sensorControllers = new List<SensorController>();
        private OperationalMetrics _currentMetrics;
        private Dictionary<Guid, SensorReading> _latestReadings = new Dictionary<Guid, SensorReading>();
        private EquipmentStatus _lastStatus = EquipmentStatus.Operational;
        private float _lastAnimationUpdate;

        // Properties
        public Equipment Equipment => _equipment;
        public Guid EquipmentId => _equipment?.Id ?? Guid.Empty;
        public EquipmentStatus Status => _equipment?.Status ?? EquipmentStatus.Offline;
        public bool IsOperational => _equipment?.IsOperational() ?? false;
        public OperationalMetrics CurrentMetrics => _currentMetrics;

        // Unity Lifecycle
        private void Awake()
        {
            ValidateConfiguration();
            InitializeEquipmentView();
        }

        private void Start()
        {
            ResolveDependencies();
            StartEquipmentMonitoring();
        }

        private void Update()
        {
            if (_equipment == null) return;

            // Update status visualization if changed
            if (_equipment.Status != _lastStatus)
            {
                UpdateStatusVisualization(_equipment.Status);
                _lastStatus = _equipment.Status;
            }

            // Update performance displays periodically
            if (_showPerformanceMetrics && Time.time - _lastAnimationUpdate > 1.0f)
            {
                UpdatePerformanceDisplays();
                _lastAnimationUpdate = Time.time;
            }

            // Handle status animations
            if (_enableStatusAnimations)
            {
                UpdateStatusAnimations();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        // Public Methods
        public void Initialize(Equipment equipment, ServiceLocator serviceLocator)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
            
            _currentMetrics = equipment.Metrics;
            InitializeComponents();
            UpdateVisualization();
        }

        public void SetStatus(EquipmentStatus newStatus)
        {
            if (_equipment == null)
            {
                Debug.LogError("Equipment not initialized");
                return;
            }

            try
            {
                _equipment = _equipment.SetStatus(newStatus);
                UpdateStatusVisualization(newStatus);
                
                // Update equipment view
                if (_equipmentView != null)
                {
                    _equipmentView.UpdateStatus(newStatus);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set equipment status: {ex.Message}");
            }
        }

        public void UpdateMetrics(OperationalMetrics metrics)
        {
            if (_equipment == null)
            {
                Debug.LogError("Equipment not initialized");
                return;
            }

            try
            {
                _equipment = _equipment.UpdateMetrics(metrics);
                _currentMetrics = metrics;
                UpdatePerformanceDisplays();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update equipment metrics: {ex.Message}");
            }
        }

        public void ScheduleMaintenance(DateTime scheduledDate, string description)
        {
            if (_equipment == null)
            {
                Debug.LogError("Equipment not initialized");
                return;
            }

            try
            {
                _equipment = _equipment.ScheduleMaintenance(scheduledDate, description);
                UpdateMaintenanceVisualization();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to schedule maintenance: {ex.Message}");
            }
        }

        public void UpdateSensorReading(SensorReading reading)
        {
            if (_equipment == null) return;

            // Store latest reading for this sensor
            _latestReadings[reading.SensorId] = reading;

            // Update equipment-specific displays based on sensor type
            UpdateSensorBasedDisplay(reading);
        }

        public bool HasSensor(Guid sensorId)
        {
            // Check if this equipment has a sensor with the given ID
            // This would typically be stored in the equipment metadata or sensors list
            return _latestReadings.ContainsKey(sensorId);
        }

        public void UpdateRealTimeVisualization()
        {
            if (_equipmentView != null)
            {
                _equipmentView.UpdateRealTimeData();
            }
        }

        // Private Methods
        private void ValidateConfiguration()
        {
            if (_equipmentView == null)
            {
                _equipmentView = GetComponent<EquipmentView>();
                if (_equipmentView == null)
                {
                    // Try to find in children
                    _equipmentView = GetComponentInChildren<EquipmentView>();
                    if (_equipmentView == null)
                    {
                        Debug.LogWarning("Equipment view component not found, creating default");
                        var viewObject = new GameObject("EquipmentView");
                        viewObject.transform.SetParent(transform);
                        _equipmentView = viewObject.AddComponent<EquipmentView>();
                    }
                }
            }

            if (_equipmentRenderer == null)
            {
                _equipmentRenderer = GetComponent<Renderer>();
            }

            if (_statusLight == null)
            {
                // Try to find status light in children
                _statusLight = GetComponentInChildren<Light>();
            }
        }

        private void InitializeEquipmentView()
        {
            if (_equipmentView != null)
            {
                _equipmentView.Initialize(this);
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

        private void StartEquipmentMonitoring()
        {
            if (_dataCollectionService == null || _equipment == null) return;

            try
            {
                // This would start monitoring for equipment-specific sensors
                // For now, we'll just log that monitoring started
                Debug.Log($"Started monitoring for equipment: {_equipment.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start equipment monitoring: {ex.Message}");
            }
        }

        private async Task InitializeComponents()
        {
            // Initialize sensor controllers (if any)
            // This would be based on equipment configuration
            
            // Initialize performance displays
            InitializePerformanceDisplays();
            
            // Set initial status visualization
            UpdateStatusVisualization(_equipment.Status);
        }

        private void InitializePerformanceDisplays()
        {
            if (!_showPerformanceMetrics) return;

            // Initialize display components
            if (_efficiencyDisplay != null)
            {
                _efficiencyDisplay.SetActive(true);
            }
            if (_powerConsumptionDisplay != null)
            {
                _powerConsumptionDisplay.SetActive(true);
            }
            if (_uptimeDisplay != null)
            {
                _uptimeDisplay.SetActive(true);
            }
        }

        private void UpdateVisualization()
        {
            if (_equipmentView != null)
            {
                _equipmentView.UpdateEquipment(_equipment, _currentMetrics);
            }

            UpdateStatusVisualization(_equipment.Status);
            UpdatePerformanceDisplays();
        }

        private void UpdateStatusVisualization(EquipmentStatus status)
        {
            // Update status light color
            if (_showStatusLight && _statusLight != null)
            {
                var color = status switch
                {
                    EquipmentStatus.Operational => Color.green,
                    EquipmentStatus.Maintenance => Color.yellow,
                    EquipmentStatus.Failed => Color.red,
                    EquipmentStatus.Offline => Color.gray,
                    EquipmentStatus.Decommissioned => Color.black,
                    _ => Color.white
                };

                _statusLight.color = color;
            }

            // Update renderer material based on status
            if (_equipmentRenderer != null)
            {
                var material = _equipmentRenderer.material;
                var color = status switch
                {
                    EquipmentStatus.Operational => Color.white,
                    EquipmentStatus.Maintenance => Color.yellow,
                    EquipmentStatus.Failed => Color.red * 0.5f,
                    EquipmentStatus.Offline => Color.gray,
                    EquipmentStatus.Decommissioned => Color.black,
                    _ => Color.white
                };

                material.color = color;
            }

            // Update animator
            if (_enableStatusAnimations && _equipmentAnimator != null)
            {
                var animationState = status switch
                {
                    EquipmentStatus.Operational => "Running",
                    EquipmentStatus.Maintenance => "Maintenance",
                    EquipmentStatus.Failed => "Error",
                    EquipmentStatus.Offline => "Idle",
                    EquipmentStatus.Decommissioned => "Disabled",
                    _ => "Idle"
                };

                _equipmentAnimator.Play(animationState);
            }
        }

        private void UpdatePerformanceDisplays()
        {
            if (!_showPerformanceMetrics) return;

            // Update efficiency display
            if (_efficiencyDisplay != null)
            {
                var textComponent = _efficiencyDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = $"Eff: {_currentMetrics.Efficiency:F0}%";
                }
            }

            // Update power consumption display
            if (_powerConsumptionDisplay != null)
            {
                var textComponent = _powerConsumptionDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = $"Power: {_equipment.PowerConsumption:F1}W";
                }
            }

            // Update uptime display
            if (_uptimeDisplay != null)
            {
                var textComponent = _uptimeDisplay.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = $"Uptime: {_currentMetrics.Uptime:F0}%";
                }
            }
        }

        private void UpdateSensorBasedDisplay(SensorReading reading)
        {
            // Update equipment-specific displays based on sensor type
            switch (reading.SensorType)
            {
                case SensorType.Temperature:
                    UpdateTemperatureDisplay(reading);
                    break;
                case SensorType.Power:
                    UpdatePowerDisplay(reading);
                    break;
                case SensorType.Vibration:
                    UpdateVibrationDisplay(reading);
                    break;
                default:
                    break;
            }
        }

        private void UpdateTemperatureDisplay(SensorReading reading)
        {
            // Update temperature-related visualization
            if (_equipmentRenderer != null)
            {
                var temperature = reading.Value;
                var heatColor = temperature switch
                {
                    < 30 => Color.blue,
                    >= 30 and < 50 => Color.green,
                    >= 50 and < 70 => Color.yellow,
                    _ => Color.red
                };

                // Apply heat effect to material
                var material = _equipmentRenderer.material;
                if (material.HasProperty("_HeatColor"))
                {
                    material.SetColor("_HeatColor", heatColor);
                }
            }
        }

        private void UpdatePowerDisplay(SensorReading reading)
        {
            // Update power consumption visualization
            var powerUsage = reading.Value;
            var intensity = Mathf.InverseLerp(0, 1000, (float)powerUsage); // Normalize to 0-1

            if (_statusLight != null)
            {
                _statusLight.intensity = intensity * 2f; // Max intensity of 2
            }
        }

        private void UpdateVibrationDisplay(SensorReading reading)
        {
            // Update vibration visualization through animation
            if (_enableStatusAnimations && _equipmentAnimator != null)
            {
                var vibrationLevel = reading.Value;
                var animationSpeed = Mathf.InverseLerp(0, 10, (float)vibrationLevel);

                // Update animation speed based on vibration
                _equipmentAnimator.speed = animationSpeed;
            }
        }

        private void UpdateStatusAnimations()
        {
            if (!_enableStatusAnimations || _equipmentAnimator == null) return;

            // Add continuous animations for operational status
            if (_equipment.Status == EquipmentStatus.Operational)
            {
                // Subtle rotation or pulsing for operational equipment
                var time = Time.time;
                var pulse = Mathf.Sin(time * 2f) * 0.1f + 1f;
                transform.localScale = Vector3.one * pulse;
            }
            else
            {
                // Reset scale when not operational
                transform.localScale = Vector3.one;
            }
        }

        private void UpdateMaintenanceVisualization()
        {
            // Show maintenance indicators
            if (_equipmentView != null)
            {
                _equipmentView.ShowMaintenanceIndicator(_equipment.Maintenance.IsScheduled());
            }

            // Update status light to indicate maintenance
            if (_showStatusLight && _statusLight != null && _equipment.Maintenance.IsScheduled())
            {
                _statusLight.color = Color.yellow;
            }
        }

        private void Cleanup()
        {
            // Stop any ongoing animations or coroutines
            if (_equipmentAnimator != null)
            {
                _equipmentAnimator.speed = 1f;
            }

            // Reset visual effects
            if (_equipmentRenderer != null)
            {
                var material = _equipmentRenderer.material;
                material.color = Color.white;
            }

            if (_statusLight != null)
            {
                _statusLight.intensity = 1f;
                _statusLight.color = Color.white;
            }
        }
    }
}