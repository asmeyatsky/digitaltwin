using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.ValueObjects;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Presentation.Views
{
    /// <summary>
    /// Building View Component
    /// 
    /// Architectural Intent:
    /// - Handles 3D visualization of building and real-time data overlays
    /// - Manages visual representation of building state and metrics
    /// - Provides interactive visualization for user exploration
    /// - Maintains efficient rendering with level of detail
    /// 
    /// Key Design Decisions:
    /// 1. Component-based visualization system
    /// 2. Efficient batching of visual updates
    /// 3. Material-based data visualization (heatmaps, overlays)
    /// 4. User interaction handling for building exploration
    /// </summary>
    public class BuildingView : MonoBehaviour
    {
        [Header("Building Visualization")]
        [SerializeField] private MeshRenderer _buildingRenderer;
        [SerializeField] private Material _defaultMaterial;
        [SerializeField] private Material _energyHeatmapMaterial;
        [SerializeField] private Material _maintenanceMaterial;
        
        [Header("UI Elements")]
        [SerializeField] private Canvas _buildingCanvas;
        [SerializeField] private TextMeshProUGUI _buildingNameText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _energyConsumptionText;
        [SerializeField] private TextMeshProUGUI _occupancyText;
        
        [Header("Data Overlays")]
        [SerializeField] private bool _showEnergyOverlay = false;
        [SerializeField] private bool _showTemperatureOverlay = false;
        [SerializeField] private bool _showOccupancyOverlay = false;
        [SerializeField] private Gradient _energyGradient;
        [SerializeField] private Gradient _temperatureGradient;
        
        [Header("Interactive Elements")]
        [SerializeField] private Collider _buildingCollider;
        [SerializeField] private GameObject _selectionIndicator;
        [SerializeField] private GameObject _detailsPanel;

        // Private fields
        private Building _building;
        private BuildingController _buildingController;
        private EnergySimulationResult _lastEnergyResult;
        private Dictionary<int, float> _energyDataByFloor = new Dictionary<int, float>();
        private bool _isBuildingSelected = false;
        private Vector3 _lastClickPosition;
        private Camera _mainCamera;

        // Properties
        public Building Building => _building;
        public bool IsShowingEnergyOverlay => _showEnergyOverlay;
        public bool IsShowingTemperatureOverlay => _showTemperatureOverlay;
        public bool IsShowingOccupancyOverlay => _showOccupancyOverlay;

        // Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
            _mainCamera = Camera.main;
            if (_mainCamera == null)
                _mainCamera = FindObjectOfType<Camera>();
        }

        private void Start()
        {
            SetupEventListeners();
            HideUI();
        }

        private void Update()
        {
            HandleUserInput();
            UpdateOverlays();
        }

        private void OnDestroy()
        {
            CleanupEventListeners();
        }

        // Public Methods
        public async void Initialize(Building building)
        {
            _building = building ?? throw new ArgumentNullException(nameof(building));
            _buildingController = GetComponent<BuildingController>();
            
            UpdateBuildingInfo();
            UpdateStatusDisplay();
            CreateFloorOverlays();
            
            await Task.Yield(); // Allow async initialization
        }

        public void UpdateBuilding(Building building)
        {
            _building = building;
            UpdateBuildingInfo();
            UpdateStatusDisplay();
        }

        public void UpdateRealTimeData()
        {
            UpdateEnergyConsumptionDisplay();
            UpdateOccupancyDisplay();
        }

        public void UpdateEnergyData(EnergySimulationResult result)
        {
            _lastEnergyResult = result;
            ProcessEnergyData(result);
            
            if (_showEnergyOverlay)
            {
                UpdateEnergyOverlay();
            }
        }

        public void UpdateSensorData(SensorReading reading)
        {
            // Update relevant overlays based on sensor type
            switch (reading.SensorType)
            {
                case SensorType.Temperature:
                    if (_showTemperatureOverlay)
                        UpdateTemperatureOverlay();
                    break;
                case SensorType.Power:
                    if (_showEnergyOverlay)
                        UpdateEnergyOverlay();
                    break;
            }
        }

        public void ShowBuildingDetails(Building building)
        {
            _isBuildingSelected = true;
            
            if (_selectionIndicator != null)
                _selectionIndicator.SetActive(true);
            
            if (_detailsPanel != null)
            {
                _detailsPanel.SetActive(true);
                PopulateDetailsPanel(building);
            }
        }

        public void HideBuildingDetails()
        {
            _isBuildingSelected = false;
            
            if (_selectionIndicator != null)
                _selectionIndicator.SetActive(false);
            
            if (_detailsPanel != null)
                _detailsPanel.SetActive(false);
        }

        public void ShowMaintenanceMode(bool inMaintenance)
        {
            if (inMaintenance)
            {
                ApplyMaterial(_maintenanceMaterial);
                ShowMaintenanceIndicator();
            }
            else
            {
                ApplyMaterial(_defaultMaterial);
                HideMaintenanceIndicator();
            }
        }

        public void ToggleEnergyOverlay()
        {
            _showEnergyOverlay = !_showEnergyOverlay;
            
            if (_showEnergyOverlay)
            {
                UpdateEnergyOverlay();
            }
            else
            {
                ClearEnergyOverlay();
            }
        }

        public void ToggleTemperatureOverlay()
        {
            _showTemperatureOverlay = !_showTemperatureOverlay;
            
            if (_showTemperatureOverlay)
            {
                UpdateTemperatureOverlay();
            }
            else
            {
                ClearTemperatureOverlay();
            }
        }

        public void ToggleOccupancyOverlay()
        {
            _showOccupancyOverlay = !_showOccupancyOverlay;
            
            if (_showOccupancyOverlay)
            {
                UpdateOccupancyOverlay();
            }
            else
            {
                ClearOccupancyOverlay();
            }
        }

        // Private Methods
        private void InitializeComponents()
        {
            // Get required components
            if (_buildingRenderer == null)
                _buildingRenderer = GetComponent<MeshRenderer>();
            
            if (_buildingCollider == null)
                _buildingCollider = GetComponent<Collider>();
            
            // Initialize materials
            if (_defaultMaterial == null && _buildingRenderer != null)
                _defaultMaterial = _buildingRenderer.material;
            
            // Setup gradients if not assigned
            if (_energyGradient == null)
                _energyGradient = CreateDefaultEnergyGradient();
            
            if (_temperatureGradient == null)
                _temperatureGradient = CreateDefaultTemperatureGradient();
        }

        private void SetupEventListeners()
        {
            // This would set up event listeners for real-time data updates
        }

        private void CleanupEventListeners()
        {
            // Clean up event listeners
        }

        private void UpdateBuildingInfo()
        {
            if (_buildingNameText != null)
                _buildingNameText.text = _building.Name;
        }

        private void UpdateStatusDisplay()
        {
            if (_statusText != null)
            {
                var statusText = _building.Status switch
                {
                    BuildingStatus.Operational => "Operational",
                    BuildingStatus.Maintenance => "Under Maintenance",
                    BuildingStatus.Emergency => "Emergency",
                    BuildingStatus.Decommissioned => "Decommissioned",
                    _ => "Unknown"
                };

                var statusColor = _building.Status switch
                {
                    BuildingStatus.Operational => Color.green,
                    BuildingStatus.Maintenance => Color.yellow,
                    BuildingStatus.Emergency => Color.red,
                    BuildingStatus.Decommissioned => Color.gray,
                    _ => Color.white
                };

                _statusText.text = statusText;
                _statusText.color = statusColor;
            }
        }

        private void UpdateEnergyConsumptionDisplay()
        {
            if (_energyConsumptionText != null && _lastEnergyResult != null)
            {
                var totalConsumption = _lastEnergyResult.TotalConsumption.ToKilowattHours();
                _energyConsumptionText.text = $"Energy: {totalConsumption:F1} kWh";
            }
        }

        private void UpdateOccupancyDisplay()
        {
            if (_occupancyText != null && _building != null)
            {
                _occupancyText.text = $"Occupancy: {_building.TotalOccupancy} people";
            }
        }

        private void CreateFloorOverlays()
        {
            if (_building == null) return;

            foreach (var floor in _building.Floors)
            {
                CreateFloorOverlay(floor);
            }
        }

        private void CreateFloorOverlay(Floor floor)
        {
            // This would create individual floor overlay renderers
            // For now, we'll just store floor data
            _energyDataByFloor[floor.Number] = 0f;
        }

        private void ProcessEnergyData(EnergySimulationResult result)
        {
            // Process energy data by floor
            if (result.ConsumptionByFloor != null)
            {
                foreach (var kvp in result.ConsumptionByFloor)
                {
                    var floorNumber = int.Parse(kvp.Key.Split('_')[1]);
                    var consumption = kvp.Value.ToKilowattHours();
                    _energyDataByFloor[floorNumber] = (float)consumption;
                }
            }
        }

        private void UpdateEnergyOverlay()
        {
            if (_energyHeatmapMaterial == null || _energyDataByFloor.Count == 0) return;

            // Apply energy data to heatmap material
            var maxConsumption = _energyDataByFloor.Values.Max();
            
            foreach (var kvp in _energyDataByFloor)
            {
                var normalizedValue = kvp.Value / maxConsumption;
                var color = _energyGradient.Evaluate(normalizedValue);
                
                // This would apply the color to the specific floor renderer
                ApplyFloorColor(kvp.Key, color);
            }

            // Apply heatmap material
            ApplyMaterial(_energyHeatmapMaterial);
        }

        private void UpdateTemperatureOverlay()
        {
            // This would update temperature visualization
            // For now, apply a basic temperature gradient
            ApplyMaterial(_energyHeatmapMaterial); // Reuse energy material for temperature
        }

        private void UpdateOccupancyOverlay()
        {
            if (_building == null) return;

            // Calculate occupancy by floor
            foreach (var floor in _building.Floors)
            {
                var occupancyRatio = (float)floor.Rooms.Count / (float)floor.MaxOccupancy;
                var color = _energyGradient.Evaluate(occupancyRatio);
                
                ApplyFloorColor(floor.Number, color);
            }
        }

        private void ClearEnergyOverlay()
        {
            ApplyMaterial(_defaultMaterial);
            ResetFloorColors();
        }

        private void ClearTemperatureOverlay()
        {
            ApplyMaterial(_defaultMaterial);
            ResetFloorColors();
        }

        private void ClearOccupancyOverlay()
        {
            ApplyMaterial(_defaultMaterial);
            ResetFloorColors();
        }

        private void ApplyMaterial(Material material)
        {
            if (_buildingRenderer != null && material != null)
            {
                _buildingRenderer.material = material;
            }
        }

        private void ApplyFloorColor(int floorNumber, Color color)
        {
            // This would find the specific floor renderer and apply color
            // For now, we'll log the action
            Debug.Log($"Applying color {color} to floor {floorNumber}");
        }

        private void ResetFloorColors()
        {
            // Reset all floor colors to default
            foreach (var floorNumber in _energyDataByFloor.Keys)
            {
                ApplyFloorColor(floorNumber, Color.white);
            }
        }

        private void ShowMaintenanceIndicator()
        {
            if (_statusText != null)
            {
                _statusText.text = "⚠️ MAINTENANCE";
                _statusText.color = Color.yellow;
            }
        }

        private void HideMaintenanceIndicator()
        {
            UpdateStatusDisplay();
        }

        private void HandleUserInput()
        {
            HandleBuildingSelection();
            HandleOverlayToggles();
        }

        private void HandleBuildingSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider == _buildingCollider)
                    {
                        _lastClickPosition = hit.point;
                        
                        if (_isBuildingSelected)
                        {
                            HideBuildingDetails();
                        }
                        else
                        {
                            ShowBuildingDetails(_building);
                        }
                    }
                }
            }
        }

        private void HandleOverlayToggles()
        {
            // Keyboard shortcuts for overlay toggles
            if (Input.GetKeyDown(KeyCode.E))
            {
                ToggleEnergyOverlay();
            }
            
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleTemperatureOverlay();
            }
            
            if (Input.GetKeyDown(KeyCode.O))
            {
                ToggleOccupancyOverlay();
            }
        }

        private void UpdateOverlays()
        {
            // Update all active overlays
            if (_showEnergyOverlay && Time.frameCount % 30 == 0) // Update every 30 frames
            {
                UpdateEnergyOverlay();
            }
            
            if (_showTemperatureOverlay && Time.frameCount % 30 == 0)
            {
                UpdateTemperatureOverlay();
            }
            
            if (_showOccupancyOverlay && Time.frameCount % 30 == 0)
            {
                UpdateOccupancyOverlay();
            }
        }

        private void PopulateDetailsPanel(Building building)
        {
            // This would populate the details panel with building information
            // For now, just show basic info
            
            var detailsText = $"Building: {building.Name}\n" +
                            $"Address: {building.Address}\n" +
                            $"Floors: {building.Floors.Count}\n" +
                            $"Total Area: {building.TotalArea:F0} m²\n" +
                            $"Status: {building.Status}";
            
            Debug.Log($"Building Details:\n{detailsText}");
        }

        private void HideUI()
        {
            if (_detailsPanel != null)
                _detailsPanel.SetActive(false);
            
            if (_selectionIndicator != null)
                _selectionIndicator.SetActive(false);
        }

        private Gradient CreateDefaultEnergyGradient()
        {
            var gradient = new Gradient();
            var colors = new GradientColorKey[3];
            var alphas = new GradientAlphaKey[2];

            colors[0] = new GradientColorKey(Color.green, 0.0f);
            colors[1] = new GradientColorKey(Color.yellow, 0.5f);
            colors[2] = new GradientColorKey(Color.red, 1.0f);

            alphas[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphas[1] = new GradientAlphaKey(1.0f, 1.0f);

            gradient.SetKeys(colors, alphas);
            return gradient;
        }

        private Gradient CreateDefaultTemperatureGradient()
        {
            var gradient = new Gradient();
            var colors = new GradientColorKey[4];
            var alphas = new GradientAlphaKey[2];

            colors[0] = new GradientColorKey(Color.blue, 0.0f);
            colors[1] = new GradientColorKey(Color.cyan, 0.33f);
            colors[2] = new GradientColorKey(Color.yellow, 0.66f);
            colors[3] = new GradientColorKey(Color.red, 1.0f);

            alphas[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphas[1] = new GradientAlphaKey(1.0f, 1.0f);

            gradient.SetKeys(colors, alphas);
            return gradient;
        }
    }
}