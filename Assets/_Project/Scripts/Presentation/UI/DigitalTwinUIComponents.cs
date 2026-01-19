using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.ValueObjects;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Presentation.UI
{
    /// <summary>
    /// Building Overview Panel Component
    /// 
    /// Architectural Intent:
    /// - Displays high-level building information and status
    /// - Provides quick access to building metrics
    /// - Shows building metadata and operational status
    /// - Enables building selection and navigation
    /// </summary>
    public class DigitalTwinBuildingOverview : MonoBehaviour
    {
        [Header("Building Information")]
        [SerializeField] private TextMeshProUGUI _buildingNameText;
        [SerializeField] private TextMeshProUGUI _buildingAddressText;
        [SerializeField] private TextMeshProUGUI _buildingTypeText;
        [SerializeField] private TextMeshProUGUI _buildingStatusText;
        [SerializeField] private Image _statusIndicator;

        [Header("Building Metrics")]
        [SerializeField] private TextMeshProUGUI _totalAreaText;
        [SerializeField] private TextMeshProUGUI _totalFloorsText;
        [SerializeField] private TextMeshProUGUI _totalRoomsText;
        [SerializeField] private TextMeshProUGUI _totalOccupancyText;
        [SerializeField] private TextMeshProUGUI _activeEquipmentText;
        [SerializeField] private TextMeshProUGUI _activeSensorsText;

        [Header("Performance Indicators")]
        [SerializeField] private Slider _efficiencySlider;
        [SerializeField] private Slider _comfortSlider;
        [SerializeField] private Slider _maintenanceSlider;
        [SerializeField] private TextMeshProUGUI _efficiencyText;
        [SerializeField] private TextMeshProUGUI _comfortText;
        [SerializeField] private TextMeshProUGUI _maintenanceText;

        [Header("Navigation")]
        [SerializeField] private Button _viewDetailsButton;
        [SerializeField] private Button _viewFloorsButton;
        [SerializeField] private Button _viewEquipmentButton;

        private DigitalTwinDashboardManager _dashboardManager;
        private Building _currentBuilding;
        private Color _primaryColor;

        public void Initialize(DigitalTwinDashboardManager dashboard, Color primaryColor)
        {
            _dashboardManager = dashboard;
            _primaryColor = primaryColor;

            SetupNavigationButtons();
            ResetDisplay();
        }

        public void UpdateBuilding(Building building)
        {
            _currentBuilding = building;
            if (building == null)
            {
                ResetDisplay();
                return;
            }

            UpdateBuildingInfo(building);
            UpdateBuildingMetrics(building);
            UpdatePerformanceIndicators(building);
        }

        private void UpdateBuildingInfo(Building building)
        {
            _buildingNameText.text = building.Name;
            _buildingAddressText.text = building.Address;
            _buildingTypeText.text = building.Metadata.Category.ToString();
            
            var statusText = building.Status.ToString();
            var statusColor = GetStatusColor(building.Status);
            
            _buildingStatusText.text = statusText;
            _statusIndicator.color = statusColor;
        }

        private void UpdateBuildingMetrics(Building building)
        {
            _totalAreaText.text = $"{building.TotalArea:F0} m²";
            _totalFloorsText.text = building.Floors.Count.ToString();
            _totalRoomsText.text = building.Rooms.Count().ToString();
            _totalOccupancyText.text = building.TotalOccupancy.ToString();
            _activeEquipmentText.text = building.GetAllEquipment().Count(e => e.Status != "Offline").ToString();
            _activeSensorsText.text = building.GetAllSensors().Count(s => s.Status != "Offline").ToString();
        }

        private void UpdatePerformanceIndicators(Building building)
        {
            // Calculate performance metrics (simplified for demo)
            var efficiency = CalculateEfficiency(building);
            var comfort = CalculateComfort(building);
            var maintenance = CalculateMaintenanceStatus(building);

            UpdatePerformanceSlider(_efficiencySlider, _efficiencyText, efficiency, "Efficiency");
            UpdatePerformanceSlider(_comfortSlider, _comfortText, comfort, "Comfort");
            UpdatePerformanceSlider(_maintenanceSlider, _maintenanceText, maintenance, "Maintenance");
        }

        private void UpdatePerformanceSlider(Slider slider, TextMeshProUGUI text, float value, string label)
        {
            slider.value = value;
            text.text = $"{label}: {value:F0}%";
            
            // Update slider color based on value
            var sliderColors = slider.GetComponentsInChildren<Image>();
            if (sliderColors.Length > 0)
            {
                var fillColor = value >= 80 ? Color.green : value >= 60 ? Color.yellow : Color.red;
                sliderColors[1].color = fillColor; // Fill color
            }
        }

        private float CalculateEfficiency(Building building)
        {
            // Simplified efficiency calculation
            var totalEquipment = building.GetAllEquipment().Count();
            var activeEquipment = building.GetAllEquipment().Count(e => e.Status == "Running");
            
            return totalEquipment > 0 ? (activeEquipment / (float)totalEquipment) * 100f : 0f;
        }

        private float CalculateComfort(Building building)
        {
            // Simplified comfort calculation based on environmental conditions
            return 75f; // Placeholder - would calculate from actual sensor data
        }

        private float CalculateMaintenanceStatus(Building building)
        {
            // Calculate maintenance health
            var totalEquipment = building.GetAllEquipment().Count();
            var healthyEquipment = building.GetAllEquipment().Count(e => e.Status != "Error" && e.Status != "Maintenance");
            
            return totalEquipment > 0 ? (healthyEquipment / (float)totalEquipment) * 100f : 100f;
        }

        private Color GetStatusColor(BuildingStatus status)
        {
            return status switch
            {
                BuildingStatus.Operational => Color.green,
                BuildingStatus.Maintenance => Color.yellow,
                BuildingStatus.Emergency => Color.red,
                BuildingStatus.Decommissioned => Color.gray,
                _ => Color.white
            };
        }

        private void SetupNavigationButtons()
        {
            if (_viewDetailsButton != null)
                _viewDetailsButton.onClick.AddListener(OnViewDetailsClicked);

            if (_viewFloorsButton != null)
                _viewFloorsButton.onClick.AddListener(OnViewFloorsClicked);

            if (_viewEquipmentButton != null)
                _viewEquipmentButton.onClick.AddListener(OnViewEquipmentClicked);
        }

        private void OnViewDetailsClicked()
        {
            _dashboardManager?.ShowBuildingInfo();
        }

        private void OnViewFloorsClicked()
        {
            // Navigate to floor view
            Debug.Log("Navigate to floor view");
        }

        private void OnViewEquipmentClicked()
        {
            _dashboardManager?.ShowControls();
        }

        private void ResetDisplay()
        {
            _buildingNameText.text = "No Building Selected";
            _buildingAddressText.text = "Select a building to view details";
            _buildingTypeText.text = "Unknown";
            _buildingStatusText.text = "Unknown";
            _statusIndicator.color = Color.gray;

            _totalAreaText.text = "0 m²";
            _totalFloorsText.text = "0";
            _totalRoomsText.text = "0";
            _totalOccupancyText.text = "0";
            _activeEquipmentText.text = "0";
            _activeSensorsText.text = "0";

            UpdatePerformanceSlider(_efficiencySlider, _efficiencyText, 0f, "Efficiency");
            UpdatePerformanceSlider(_comfortSlider, _comfortText, 0f, "Comfort");
            UpdatePerformanceSlider(_maintenanceSlider, _maintenanceText, 100f, "Maintenance");
        }
    }

    /// <summary>
    /// Energy Consumption Display Component
    /// 
    /// Architectural Intent:
    /// - Visualizes real-time and historical energy consumption
    /// - Provides energy usage breakdown by equipment type
    /// - Shows cost analysis and carbon footprint
    /// - Enables energy optimization controls
    /// </summary>
    public class DigitalTwinEnergyDisplay : MonoBehaviour
    {
        [Header("Current Consumption")]
        [SerializeField] private TextMeshProUGUI _currentConsumptionText;
        [SerializeField] private TextMeshProUGUI _consumptionRateText;
        [SerializeField] private Image _consumptionGauge;

        [Header("Time Period Totals")]
        [SerializeField] private TextMeshProUGUI _dailyTotalText;
        [SerializeField] private TextMeshProUGUI _weeklyTotalText;
        [SerializeField] private TextMeshProUGUI _monthlyTotalText;
        [SerializeField] private TextMeshProUGUI _estimatedCostText;

        [Header("Breakdown Chart")]
        [SerializeField] private PieChart _energyBreakdownChart;
        [SerializeField] private Transform _breakdownItemsContainer;

        [Header("Controls")]
        [SerializeField] private Button _runSimulationButton;
        [SerializeField] private Button _optimizeButton;
        [SerializeField] private Button _exportDataButton;

        [Header("Settings")]
        [SerializeField] private Color _successColor = Color.green;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;

        private DigitalTwinDashboardManager _dashboardManager;
        private ServiceLocator _serviceLocator;
        private Building _currentBuilding;
        private decimal _currentConsumption = 0;
        private decimal _dailyTotal = 0;
        private decimal _weeklyTotal = 0;
        private decimal _monthlyTotal = 0;
        private Dictionary<string, decimal> _consumptionByType = new Dictionary<string, decimal>();

        public void Initialize(DigitalTwinDashboardManager dashboard, ServiceLocator serviceLocator, 
                            Color successColor, Color warningColor, Color errorColor)
        {
            _dashboardManager = dashboard;
            _serviceLocator = serviceLocator;
            _successColor = successColor;
            _warningColor = warningColor;
            _errorColor = errorColor;

            SetupControlButtons();
            ResetDisplay();
        }

        public void SetBuilding(Building building)
        {
            _currentBuilding = building;
            StartEnergyMonitoring();
        }

        public void UpdateEnergyData(decimal current, decimal daily, decimal weekly, decimal monthly)
        {
            _currentConsumption = current;
            _dailyTotal = daily;
            _weeklyTotal = weekly;
            _monthlyTotal = monthly;

            UpdateCurrentConsumptionDisplay();
            UpdateTimePeriodTotals();
            UpdateConsumptionGauge();
        }

        public void UpdateSimulationResult(EnergySimulationResult result)
        {
            if (result.IsSuccess)
            {
                UpdateEnergyData(
                    result.TotalEnergyConsumption,
                    result.TotalEnergyConsumption, // Simplified
                    result.TotalEnergyConsumption * 7, // Weekly estimate
                    result.TotalEnergyConsumption * 30  // Monthly estimate
                );

                // Update breakdown
                if (result.EnergyConsumptionByType != null)
                {
                    _consumptionByType = result.EnergyConsumptionByType;
                    UpdateBreakdownChart();
                }

                // Update cost
                var estimatedCost = result.EstimatedCost;
                _estimatedCostText.text = $"${estimatedCost:F2}";
            }
        }

        private void UpdateCurrentConsumptionDisplay()
        {
            _currentConsumptionText.text = $"{_currentConsumption:F2} kW";
            
            // Calculate consumption rate (kWh per hour)
            var rate = _currentConsumption; // Simplified
            _consumptionRateText.text = $"{rate:F2} kWh/h";
        }

        private void UpdateTimePeriodTotals()
        {
            _dailyTotalText.text = $"{_dailyTotal:F2} kWh";
            _weeklyTotalText.text = $"{_weeklyTotal:F2} kWh";
            _monthlyTotalText.text = $"{_monthlyTotal:F2} kWh";
            
            // Update estimated cost (assuming $0.12 per kWh)
            var dailyCost = _dailyTotal * 0.12m;
            _estimatedCostText.text = $"${dailyCost:F2} / day";
        }

        private void UpdateConsumptionGauge()
        {
            if (_consumptionGauge != null)
            {
                // Normalize current consumption to 0-1 range (assuming max 100kW)
                var fillAmount = Mathf.Clamp01((float)_currentConsumption / 100f);
                _consumptionGauge.fillAmount = fillAmount;

                // Update gauge color based on consumption level
                var gaugeColor = fillAmount < 0.5 ? _successColor : fillAmount < 0.8 ? _warningColor : _errorColor;
                _consumptionGauge.color = gaugeColor;
            }
        }

        private void UpdateBreakdownChart()
        {
            if (_energyBreakdownChart != null)
            {
                var chartData = _consumptionByType.Select(kvp => new PieChartData
                {
                    label = kvp.Key,
                    value = (float)kvp.Value,
                    color = GetColorForType(kvp.Key)
                }).ToList();

                _energyBreakdownChart.SetData(chartData);
            }

            // Update breakdown items list
            UpdateBreakdownItems();
        }

        private void UpdateBreakdownItems()
        {
            // Clear existing items
            foreach (Transform child in _breakdownItemsContainer)
            {
                Destroy(child.gameObject);
            }

            // Create new items
            foreach (var kvp in _consumptionByType.OrderByDescending(kvp => kvp.Value))
            {
                var itemGO = new GameObject($"{kvp.Key}Item");
                itemGO.transform.SetParent(_breakdownItemsContainer, false);

                var item = itemGO.AddComponent<EnergyBreakdownItem>();
                item.Initialize(kvp.Key, kvp.Value, _consumptionByType.Values.Sum());
            }
        }

        private Color GetColorForType(string type)
        {
            return type switch
            {
                "HVAC" => Color.blue,
                "Lighting" => Color.yellow,
                "Equipment" => Color.green,
                "Base" => Color.gray,
                _ => Color.white
            };
        }

        private void SetupControlButtons()
        {
            if (_runSimulationButton != null)
                _runSimulationButton.onClick.AddListener(OnRunSimulationClicked);

            if (_optimizeButton != null)
                _optimizeButton.onClick.AddListener(OnOptimizeClicked);

            if (_exportDataButton != null)
                _exportDataButton.onClick.AddListener(OnExportDataClicked);
        }

        private void OnRunSimulationClicked()
        {
            if (_currentBuilding != null && _serviceLocator != null)
            {
                var simulationService = _serviceLocator.GetService<ISimulationService>();
                var parameters = new SimulationParameters
                {
                    EnableRealTimeData = true,
                    EnergyRate = 0.12m,
                    EnableHVAC = true,
                    EnableLighting = true,
                    EnableEquipment = true
                };

                _ = simulationService?.SimulateEnergyConsumptionAsync(
                    _currentBuilding.Id, 
                    TimeSpan.FromHours(24), 
                    parameters
                );
            }
        }

        private void OnOptimizeClicked()
        {
            // Show optimization panel
            Debug.Log("Energy optimization requested");
        }

        private void OnExportDataClicked()
        {
            // Export energy data
            Debug.Log("Export energy data");
        }

        private void StartEnergyMonitoring()
        {
            // Subscribe to energy data updates
            if (_serviceLocator != null)
            {
                var dataService = _serviceLocator.GetService<IDataCollectionService>();
                if (dataService != null)
                {
                    // Start monitoring energy consumption
                }
            }
        }

        private void ResetDisplay()
        {
            _currentConsumptionText.text = "0 kW";
            _consumptionRateText.text = "0 kWh/h";
            _dailyTotalText.text = "0 kWh";
            _weeklyTotalText.text = "0 kWh";
            _monthlyTotalText.text = "0 kWh";
            _estimatedCostText.text = "$0.00 / day";

            if (_consumptionGauge != null)
                _consumptionGauge.fillAmount = 0;

            _consumptionByType.Clear();
            UpdateBreakdownChart();
        }
    }

    /// <summary>
    /// Energy Breakdown List Item Component
    /// </summary>
    public class EnergyBreakdownItem : MonoBehaviour
    {
        [SerializeField] private Image _colorIndicator;
        [SerializeField] private TextMeshProUGUI _typeText;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private TextMeshProUGUI _percentageText;

        public void Initialize(string type, decimal value, decimal total)
        {
            _typeText.text = type;
            _valueText.text = $"{value:F2} kWh";
            
            var percentage = total > 0 ? (value / total) * 100 : 0;
            _percentageText.text = $"{percentage:F1}%";

            _colorIndicator.color = GetColorForType(type);
        }

        private Color GetColorForType(string type)
        {
            return type switch
            {
                "HVAC" => Color.blue,
                "Lighting" => Color.yellow,
                "Equipment" => Color.green,
                "Base" => Color.gray,
                _ => Color.white
            };
        }
    }

    /// <summary>
    /// Simple Pie Chart Component for Energy Breakdown
    /// </summary>
    public class PieChart : MonoBehaviour
    {
        [SerializeField] private Image _pieImage;
        [SerializeField] private Transform _legendContainer;

        private List<PieChartData> _chartData = new List<PieChartData>();

        public void SetData(List<PieChartData> data)
        {
            _chartData = data;
            RedrawChart();
        }

        private void RedrawChart()
        {
            if (_chartData.Count == 0) return;

            // Create pie wedges (simplified - using sprite fill for demo)
            var total = _chartData.Sum(d => d.value);
            var currentAngle = 0f;

            // In a real implementation, you'd create individual wedge sprites
            // For now, just update the main color to the dominant category
            if (_chartData.Count > 0)
            {
                var dominant = _chartData.OrderByDescending(d => d.value).First();
                _pieImage.color = dominant.color;
            }
        }
    }

    [System.Serializable]
    public class PieChartData
    {
        public string label;
        public float value;
        public Color color;
    }
}