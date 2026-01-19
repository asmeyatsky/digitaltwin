using System;
using UnityEngine;
using TMPro;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Presentation.Views
{
    /// <summary>
    /// Equipment View Component
    /// 
    /// Architectural Intent:
    /// - Handles 3D visualization of equipment and performance metrics
    /// - Manages visual representation of equipment state and status
    /// - Provides interactive visualization for equipment monitoring
    /// - Displays real-time sensor data with visual feedback
    /// 
    /// Key Design Decisions:
    /// 1. Component-based visualization system
    /// 2. Material-based status visualization
    /// 3. Animated indicators for performance feedback
    /// 4. Interactive elements for equipment control
    /// </summary>
    public class EquipmentView : MonoBehaviour
    {
        [Header("Equipment Visualization")]
        [SerializeField] private MeshRenderer _equipmentRenderer;
        [SerializeField] private Animator _equipmentAnimator;
        [SerializeField] private Light _statusLight;
        [SerializeField] private ParticleSystem _effectSystem;
        
        [Header("Status Materials")]
        [SerializeField] private Material _operationalMaterial;
        [SerializeField] private Material _maintenanceMaterial;
        [SerializeField] private Material _failedMaterial;
        [SerializeField] private Material _offlineMaterial;
        
        [Header("Performance Indicators")]
        [SerializeField] private GameObject _efficiencyIndicator;
        [SerializeField] private GameObject _powerIndicator;
        [SerializeField] private GameObject _heatIndicator;
        
        [Header("UI Elements")]
        [SerializeField] private Canvas _equipmentCanvas;
        [SerializeField] private TextMeshProUGUI _equipmentNameText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _efficiencyText;
        [SerializeField] private TextMeshProUGUI _powerText;
        [SerializeField] private GameObject _maintenanceIcon;
        
        [Header("Interaction")]
        [SerializeField] private Collider _equipmentCollider;
        [SerializeField] private bool _enableUserInteraction = true;
        [SerializeField] private bool _showStatusAnimations = true;

        // Private fields
        private Equipment _equipment;
        private EquipmentController _equipmentController;
        private OperationalMetrics _currentMetrics;
        private EquipmentStatus _lastStatus = EquipmentStatus.Offline;
        private bool _isSelected = false;
        private Color _originalEquipmentColor;
        private float _lastAnimationUpdate;

        // Properties
        public Equipment Equipment => _equipment;
        public EquipmentStatus Status => _equipment?.Status ?? EquipmentStatus.Offline;
        public bool IsOperational => _equipment?.IsOperational() ?? false;

        // Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            HideUI();
        }

        private void Update()
        {
            UpdateStatusVisualization();
            UpdatePerformanceIndicators();
            UpdateStatusAnimations();
            HandleUserInteraction();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        // Public Methods
        public void Initialize(EquipmentController equipmentController)
        {
            _equipmentController = equipmentController ?? throw new ArgumentNullException(nameof(equipmentController));
            _equipment = equipmentController.Equipment;
            _currentMetrics = _equipment.Metrics;
            
            UpdateEquipmentInfo();
            UpdateStatusVisualization();
            UpdatePerformanceDisplay();
        }

        public void UpdateEquipment(Equipment equipment, OperationalMetrics metrics)
        {
            _equipment = equipment;
            _currentMetrics = metrics;
            
            UpdateEquipmentInfo();
            UpdateStatusVisualization();
            UpdatePerformanceDisplay();
        }

        public void UpdateStatus(EquipmentStatus status)
        {
            UpdateStatusVisualization();
            UpdateStatusText();
        }

        public void UpdateRealTimeData()
        {
            UpdatePerformanceDisplay();
        }

        public void ShowMaintenanceIndicator(bool show)
        {
            if (_maintenanceIcon != null)
            {
                _maintenanceIcon.SetActive(show);
            }
            
            if (_statusLight != null && show)
            {
                _statusLight.color = Color.yellow;
                _statusLight.intensity = 2f;
            }
        }

        // Private Methods
        private void InitializeComponents()
        {
            // Get required components
            if (_equipmentRenderer == null)
                _equipmentRenderer = GetComponent<MeshRenderer>();
            
            if (_equipmentAnimator == null)
                _equipmentAnimator = GetComponent<Animator>();
            
            if (_equipmentCollider == null)
                _equipmentCollider = GetComponent<Collider>();
            
            if (_statusLight == null)
                _statusLight = GetComponentInChildren<Light>();
            
            // Store original color
            if (_equipmentRenderer != null)
            {
                _originalEquipmentColor = _equipmentRenderer.material.color;
            }
        }

        private void UpdateEquipmentInfo()
        {
            if (_equipmentNameText != null && _equipment != null)
            {
                _equipmentNameText.text = _equipment.Name;
            }
        }

        private void UpdateStatusVisualization()
        {
            if (_equipment == null) return;

            var currentStatus = _equipment.Status;
            if (currentStatus == _lastStatus) return;

            // Update material based on status
            var targetMaterial = currentStatus switch
            {
                EquipmentStatus.Operational => _operationalMaterial,
                EquipmentStatus.Maintenance => _maintenanceMaterial,
                EquipmentStatus.Failed => _failedMaterial,
                EquipmentStatus.Offline => _offlineMaterial,
                _ => _offlineMaterial
            };

            if (targetMaterial != null && _equipmentRenderer != null)
            {
                _equipmentRenderer.material = targetMaterial;
            }

            // Update status light
            UpdateStatusLight(currentStatus);

            // Update animator
            UpdateStatusAnimation(currentStatus);

            _lastStatus = currentStatus;
        }

        private void UpdateStatusLight(EquipmentStatus status)
        {
            if (_statusLight == null) return;

            var lightColor = status switch
            {
                EquipmentStatus.Operational => Color.green,
                EquipmentStatus.Maintenance => Color.yellow,
                EquipmentStatus.Failed => Color.red,
                EquipmentStatus.Offline => Color.gray,
                _ => Color.white
            };

            var lightIntensity = status switch
            {
                EquipmentStatus.Operational => 1f,
                EquipmentStatus.Maintenance => 1.5f,
                EquipmentStatus.Failed => 2f, // Pulsing for failed
                EquipmentStatus.Offline => 0.5f,
                _ => 1f
            };

            _statusLight.color = lightColor;
            _statusLight.intensity = lightIntensity;
        }

        private void UpdateStatusAnimation(EquipmentStatus status)
        {
            if (_equipmentAnimator == null || !_showStatusAnimations) return;

            var animationState = status switch
            {
                EquipmentStatus.Operational => "Running",
                EquipmentStatus.Maintenance => "Maintenance",
                EquipmentStatus.Failed => "Error",
                EquipmentStatus.Offline => "Idle",
                _ => "Idle"
            };

            _equipmentAnimator.Play(animationState);
        }

        private void UpdatePerformanceDisplay()
        {
            UpdateEfficiencyDisplay();
            UpdatePowerDisplay();
            UpdatePerformanceTexts();
        }

        private void UpdateEfficiencyDisplay()
        {
            if (_efficiencyIndicator == null) return;

            var efficiency = _currentMetrics.Efficiency;
            var indicatorRenderer = _efficiencyIndicator.GetComponent<Renderer>();
            
            if (indicatorRenderer != null)
            {
                // Color code based on efficiency
                var color = efficiency switch
                {
                    >= 90 => Color.green,
                    >= 75 => Color.yellow,
                    >= 50 => Color.orange,
                    _ => Color.red
                };
                
                indicatorRenderer.material.color = color;
                
                // Scale indicator based on efficiency
                var scale = Mathf.InverseLerp(0, 100, (float)efficiency);
                _efficiencyIndicator.transform.localScale = Vector3.one * (0.5f + scale * 0.5f);
            }
        }

        private void UpdatePowerDisplay()
        {
            if (_powerIndicator == null) return;

            var powerUsage = _equipment.PowerConsumption;
            var indicatorRenderer = _powerIndicator.GetComponent<Renderer>();
            
            if (indicatorRenderer != null)
            {
                // Color based on power consumption level
                var normalizedPower = Mathf.InverseLerp(0, 5000, (float)powerUsage);
                var color = Color.Lerp(Color.green, Color.red, normalizedPower);
                
                indicatorRenderer.material.color = color;
                
                // Intensity based on power
                var intensity = normalizedPower;
                if (indicatorRenderer.material.HasProperty("_Intensity"))
                {
                    indicatorRenderer.material.SetFloat("_Intensity", intensity);
                }
            }
        }

        private void UpdatePerformanceTexts()
        {
            if (_efficiencyText != null)
            {
                _efficiencyText.text = $"Eff: {_currentMetrics.Efficiency:F0}%";
                
                // Color code efficiency text
                var color = _currentMetrics.Efficiency switch
                {
                    >= 90 => Color.green,
                    >= 75 => Color.yellow,
                    >= 50 => Color.orange,
                    _ => Color.red
                };
                
                _efficiencyText.color = color;
            }

            if (_powerText != null)
            {
                _powerText.text = $"Power: {_equipment.PowerConsumption:F1}W";
            }
        }

        private void UpdateStatusText()
        {
            if (_statusText == null) return;

            var statusText = _equipment.Status switch
            {
                EquipmentStatus.Operational => "Operational",
                EquipmentStatus.Maintenance => "Maintenance",
                EquipmentStatus.Failed => "Failed",
                EquipmentStatus.Offline => "Offline",
                _ => "Unknown"
            };

            var statusColor = _equipment.Status switch
            {
                EquipmentStatus.Operational => Color.green,
                EquipmentStatus.Maintenance => Color.yellow,
                EquipmentStatus.Failed => Color.red,
                EquipmentStatus.Offline => Color.gray,
                _ => Color.white
            };

            _statusText.text = statusText;
            _statusText.color = statusColor;
        }

        private void UpdateStatusAnimations()
        {
            if (!_showStatusAnimations) return;

            // Pulsing effect for failed equipment
            if (_equipment.Status == EquipmentStatus.Failed)
            {
                var pulse = Mathf.Sin(Time.time * 3f) * 0.5f + 0.5f;
                
                if (_statusLight != null)
                {
                    _statusLight.intensity = Mathf.Lerp(0.5f, 2f, pulse);
                }
                
                if (_equipmentRenderer != null)
                {
                    var color = Color.Lerp(Color.red, Color.white, pulse);
                    _equipmentRenderer.material.color = color;
                }
            }

            // Subtle rotation for operational equipment
            if (_equipment.Status == EquipmentStatus.Operational && _equipmentAnimator == null)
            {
                transform.Rotate(0, Time.deltaTime * 10f, 0);
            }

            // Slow blink for maintenance
            if (_equipment.Status == EquipmentStatus.Maintenance)
            {
                var blink = Mathf.Sin(Time.time * 2f) > 0;
                
                if (_statusLight != null)
                {
                    _statusLight.enabled = blink;
                }
            }
        }

        private void HandleUserInteraction()
        {
            if (!_enableUserInteraction) return;

            HandleEquipmentClick();
            HandleEquipmentHover();
        }

        private void HandleEquipmentClick()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider == _equipmentCollider)
                    {
                        OnEquipmentClicked();
                    }
                }
            }
        }

        private void HandleEquipmentHover()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var isHovering = Physics.Raycast(ray, out RaycastHit hit) && hit.collider == _equipmentCollider;

            // Update hover visual
            if (_equipmentRenderer != null)
            {
                var targetColor = isHovering ? Color.Lerp(_originalEquipmentColor, Color.white, 0.3f) : _originalEquipmentColor;
                _equipmentRenderer.material.color = Color.Lerp(_equipmentRenderer.material.color, targetColor, Time.deltaTime * 5f);
            }
        }

        private void OnEquipmentClicked()
        {
            Debug.Log($"Equipment clicked: {_equipment.Name}");
            
            // This would show equipment details UI
            ShowEquipmentDetails();
        }

        private void ShowEquipmentDetails()
        {
            Debug.Log($"Equipment Details:\n" +
                      $"Name: {_equipment.Name}\n" +
                      $"Type: {_equipment.Type}\n" +
                      $"Model: {_equipment.Model}\n" +
                      $"Status: {_equipment.Status}\n" +
                      $"Power: {_equipment.PowerConsumption:F1}W\n" +
                      $"Efficiency: {_currentMetrics.Efficiency:F0}%\n" +
                      $"Uptime: {_currentMetrics.Uptime:F0}%\n" +
                      $"Errors: {_currentMetrics.ErrorCount}");
        }

        private void HideUI()
        {
            // Hide any UI elements that should be hidden by default
        }

        private void Cleanup()
        {
            // Restore original material
            if (_equipmentRenderer != null)
            {
                _equipmentRenderer.material.color = _originalEquipmentColor;
            }

            // Reset status light
            if (_statusLight != null)
            {
                _statusLight.color = Color.white;
                _statusLight.intensity = 1f;
                _statusLight.enabled = true;
            }
        }
    }
}