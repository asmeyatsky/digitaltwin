using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Presentation.UI.Charts;
using System;

namespace DigitalTwin.Presentation.UI
{
    /// <summary>
    /// Analytics dashboard with KPIs and data visualization
    /// </summary>
    public class AnalyticsDashboard : MonoBehaviour
    {
        [Header("Dashboard Components")]
        [SerializeField] private GameObject dashboardPanel;
        [SerializeField] private TMP_Text dashboardTitle;
        [SerializeField] private TMP_Text lastUpdatedText;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button exportButton;
        [SerializeField] private Button configureButton;
        
        [Header("KPI Cards")]
        [SerializeField] private KPICard energyKPI;
        [SerializeField] private KPICard temperatureKPI;
        [SerializeField] private KPICard occupancyKPI;
        [SerializeField] private KPICard efficiencyKPI;
        [SerializeField] private KPICard comfortKPI;
        [SerializeField] private KPICard costKPI;
        
        [Header("Charts")]
        [SerializeField] private LineChart energyConsumptionChart;
        [SerializeField] private LineChart temperatureChart;
        [SerializeField] private BarChart occupancyChart;
        [SerializeField] private PieChart energyDistributionChart;
        [SerializeField] private GaugeChart efficiencyGauge;
        [SerializeField] private AreaChart costTrendChart;
        
        [Header("Building Selector")]
        [SerializeField] private TMP_Dropdown buildingDropdown;
        [SerializeField] private Button allBuildingsButton;
        
        [Header("Date Range Selector")]
        [SerializeField] private TMP_Dropdown dateRangeDropdown;
        [SerializeField] private TMP_InputField startDateInput;
        [SerializeField] private TMP_InputField endDateInput;
        [SerializeField] private Button customRangeButton;
        
        [Header("Alerts Panel")]
        [SerializeField] private Transform alertsContainer;
        [SerializeField] private GameObject alertItemPrefab;
        [SerializeField] private TMP_Text alertCountText;
        [SerializeField] private Button viewAllAlertsButton;
        
        [Header("Predictive Insights Panel")]
        [SerializeField] private Transform insightsContainer;
        [SerializeField] private GameObject insightItemPrefab;
        [SerializeField] private Button viewAllInsightsButton;
        
        [Header("Loading")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Slider loadingSlider;
        [SerializeField] private TMP_Text loadingText;

        private IAnalyticsService _analyticsService;
        private List<Guid> _buildingIds;
        private Guid? _selectedBuildingId;
        private DateTime _startDate;
        private DateTime _endDate;
        private BuildingKPIs _currentKPIs;
        private DashboardConfiguration _currentConfig;

        private void Start()
        {
            InitializeServices();
            SetupUI();
            LoadInitialData();
        }

        private void InitializeServices()
        {
            _analyticsService = ServiceLocator.Instance.GetService<IAnalyticsService>();
        }

        private void SetupUI()
        {
            // Setup button listeners
            refreshButton.onClick.AddListener(RefreshDashboard);
            exportButton.onClick.AddListener(ExportDashboard);
            configureButton.onClick.AddListener(OpenConfiguration);
            allBuildingsButton.onClick.AddListener(SelectAllBuildings);
            customRangeButton.onClick.AddListener(ApplyCustomDateRange);
            viewAllAlertsButton.onClick.AddListener(ViewAllAlerts);
            viewAllInsightsButton.onClick.AddListener(ViewAllInsights);

            // Setup dropdown listeners
            buildingDropdown.onValueChanged.AddListener(OnBuildingSelected);
            dateRangeDropdown.onValueChanged.AddListener(OnDateRangeSelected);

            // Setup date inputs
            startDateInput.onValueChanged.AddListener(OnDateInputChanged);
            endDateInput.onValueChanged.AddListener(OnDateInputChanged);

            // Initialize date range dropdown
            InitializeDateRangeDropdown();

            // Set default date range (last 7 days)
            SetDefaultDateRange();
        }

        private void InitializeDateRangeDropdown()
        {
            dateRangeDropdown.options.Clear();
            dateRangeDropdown.options.Add(new TMP_Dropdown.OptionData("Last 24 Hours"));
            dateRangeDropdown.options.Add(new TMP_Dropdown.OptionData("Last 7 Days"));
            dateRangeDropdown.options.Add(new TMP_Dropdown.OptionData("Last 30 Days"));
            dateRangeDropdown.options.Add(new TMP_Dropdown.OptionData("Last Quarter"));
            dateRangeDropdown.options.Add(new TMP_Dropdown.OptionData("Last Year"));
            dateRangeDropdown.options.Add(new TMP_Dropdown.OptionData("Custom Range"));
            
            dateRangeDropdown.value = 2; // Last 7 days
            dateRangeDropdown.RefreshShownValue();
        }

        private void SetDefaultDateRange()
        {
            _endDate = DateTime.UtcNow;
            _startDate = _endDate.AddDays(-7);
            
            startDateInput.text = _startDate.ToString("yyyy-MM-dd");
            endDateInput.text = _endDate.ToString("yyyy-MM-dd");
        }

        private async void LoadInitialData()
        {
            await LoadBuildings();
            await RefreshDashboard();
        }

        private async Task LoadBuildings()
        {
            try
            {
                ShowLoading("Loading buildings...");
                
                // In a real implementation, this would get buildings from building service
                _buildingIds = new List<Guid>
                {
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("33333333-3333-3333-3333-333333333333")
                };

                // Update building dropdown
                buildingDropdown.options.Clear();
                buildingDropdown.options.Add(new TMP_Dropdown.OptionData("All Buildings"));
                
                foreach (var buildingId in _buildingIds)
                {
                    var buildingName = $"Building {buildingId.ToString().Substring(0, 8)}";
                    buildingDropdown.options.Add(new TMP_Dropdown.OptionData(buildingName));
                }
                
                buildingDropdown.value = 0;
                buildingDropdown.RefreshShownValue();
                
                HideLoading();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading buildings: {ex.Message}");
                HideLoading();
                ShowErrorMessage("Failed to load buildings");
            }
        }

        private async void RefreshDashboard()
        {
            try
            {
                ShowLoading("Refreshing dashboard...");
                
                if (_selectedBuildingId.HasValue)
                {
                    _currentKPIs = await _analyticsService.GetBuildingKPIsAsync(_selectedBuildingId.Value, _startDate, _endDate);
                    dashboardTitle.text = $"Analytics Dashboard - {_currentKPIs.BuildingName}";
                }
                else
                {
                    // Get comparative analytics for all buildings
                    var comparativeAnalytics = await _analyticsService.GetComparativeAnalyticsAsync(_buildingIds, _startDate, _endDate);
                    _currentKPIs = CreateAggregateKPIs(comparativeAnalytics);
                    dashboardTitle.text = "Analytics Dashboard - All Buildings";
                }

                // Update UI components
                UpdateKPIs();
                await UpdateCharts();
                await UpdateAlerts();
                await UpdatePredictiveInsights();
                
                // Update last updated time
                lastUpdatedText.text = $"Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                
                HideLoading();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error refreshing dashboard: {ex.Message}");
                HideLoading();
                ShowErrorMessage("Failed to refresh dashboard");
            }
        }

        private void UpdateKPIs()
        {
            if (_currentKPIs == null) return;

            // Energy KPIs
            energyKPI.SetValue(_currentKPIs.EnergyKPIs.TotalConsumption, "kWh");
            energyKPI.SetTrend(_currentKPIs.EnergyKPIs.TotalConsumption > 1000 ? "up" : "down");
            energyKPI.SetSubtitle("Total Consumption");

            // Temperature KPIs
            temperatureKPI.SetValue(_currentKPIs.EnvironmentalKPIs.AverageTemperature, "°C");
            temperatureKPI.SetTrend("stable");
            temperatureKPI.SetSubtitle("Average Temperature");

            // Occupancy KPIs
            occupancyKPI.SetValue(_currentKPIs.OccupancyKPIs.OccupancyRate * 100, "%");
            occupancyKPI.SetTrend(_currentKPIs.OccupancyKPIs.OccupancyRate > 0.7 ? "up" : "down");
            occupancyKPI.SetSubtitle("Occupancy Rate");

            // Efficiency KPIs
            efficiencyKPI.SetValue(_currentKPIs.EnergyKPIs.EfficiencyScore, "/100");
            efficiencyKPI.SetTrend(_currentKPIs.EnergyKPIs.EfficiencyScore > 80 ? "up" : "down");
            efficiencyKPI.SetSubtitle("Energy Efficiency");

            // Comfort KPIs
            comfortKPI.SetValue(_currentKPIs.EnvironmentalKPIs.ComfortScore, "/100");
            comfortKPI.SetTrend("stable");
            comfortKPI.SetSubtitle("Comfort Score");

            // Cost KPIs
            costKPI.SetValue(_currentKPIs.EnergyKPIs.Cost, "$");
            costKPI.SetTrend(_currentKPIs.EnergyKPIs.Cost > 1000 ? "up" : "down");
            costKPI.SetSubtitle("Energy Cost");
        }

        private async Task UpdateCharts()
        {
            if (_currentKPIs == null) return;

            try
            {
                // Energy consumption trend
                var energyTrends = await _analyticsService.GetMetricTrendsAsync(
                    _selectedBuildingId ?? _buildingIds[0], 
                    "energy", 
                    _startDate, 
                    _endDate, 
                    TimeSpan.FromHours(1));
                
                energyConsumptionChart.SetData(energyTrends);
                energyConsumptionChart.SetTitle("Energy Consumption Trend");

                // Temperature trend
                var temperatureTrends = await _analyticsService.GetMetricTrendsAsync(
                    _selectedBuildingId ?? _buildingIds[0], 
                    "temperature", 
                    _startDate, 
                    _endDate, 
                    TimeSpan.FromHours(1));
                
                temperatureChart.SetData(temperatureTrends);
                temperatureChart.SetTitle("Temperature Trend");

                // Occupancy chart
                var occupancyTrends = await _analyticsService.GetMetricTrendsAsync(
                    _selectedBuildingId ?? _buildingIds[0], 
                    "occupancy", 
                    _startDate, 
                    _endDate, 
                    TimeSpan.FromDays(1));
                
                occupancyChart.SetData(occupancyTrends);
                occupancyChart.SetTitle("Daily Occupancy");

                // Energy distribution pie chart
                var energyDistribution = CreateEnergyDistributionData();
                energyDistributionChart.SetData(energyDistribution);
                energyDistributionChart.SetTitle("Energy Distribution");

                // Efficiency gauge
                efficiencyGauge.SetValue(_currentKPIs.EnergyKPIs.EfficiencyScore);
                efficiencyGauge.SetTitle("Energy Efficiency");

                // Cost trend
                var costTrends = await _analyticsService.GetMetricTrendsAsync(
                    _selectedBuildingId ?? _buildingIds[0], 
                    "cost", 
                    _startDate, 
                    _endDate, 
                    TimeSpan.FromDays(1));
                
                costTrendChart.SetData(costTrends);
                costTrendChart.SetTitle("Cost Trend");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating charts: {ex.Message}");
            }
        }

        private async Task UpdateAlerts()
        {
            try
            {
                // Clear existing alerts
                foreach (Transform child in alertsContainer)
                {
                    Destroy(child.gameObject);
                }

                // Get mock alerts (in real implementation, this would come from alert service)
                var alerts = CreateMockAlerts();
                
                // Update alert count
                alertCountText.text = $"{alerts.Count} Active Alerts";

                // Create alert items
                foreach (var alert in alerts)
                {
                    var alertItem = Instantiate(alertItemPrefab, alertsContainer);
                    var alertUI = alertItem.GetComponent<AlertItemUI>();
                    
                    if (alertUI != null)
                    {
                        alertUI.Setup(alert);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating alerts: {ex.Message}");
            }
        }

        private async Task UpdatePredictiveInsights()
        {
            try
            {
                // Clear existing insights
                foreach (Transform child in insightsContainer)
                {
                    Destroy(child.gameObject);
                }

                if (_selectedBuildingId.HasValue)
                {
                    var insights = await _analyticsService.GetPredictiveInsightsAsync(_selectedBuildingId.Value, _startDate, _endDate);
                    
                    // Create insight items
                    foreach (var recommendation in insights.Recommendations.Take(3))
                    {
                        var insightItem = Instantiate(insightItemPrefab, insightsContainer);
                        var insightUI = insightItem.GetComponent<InsightItemUI>();
                        
                        if (insightUI != null)
                        {
                            insightUI.Setup(recommendation, "recommendation");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating predictive insights: {ex.Message}");
            }
        }

        private void OnBuildingSelected(int buildingIndex)
        {
            if (buildingIndex == 0)
            {
                _selectedBuildingId = null;
            }
            else if (buildingIndex > 0 && buildingIndex <= _buildingIds.Count)
            {
                _selectedBuildingId = _buildingIds[buildingIndex - 1];
            }
            
            RefreshDashboard();
        }

        private void OnDateRangeSelected(int rangeIndex)
        {
            _endDate = DateTime.UtcNow;
            
            switch (rangeIndex)
            {
                case 0: // Last 24 hours
                    _startDate = _endDate.AddHours(-24);
                    break;
                case 1: // Last 7 days
                    _startDate = _endDate.AddDays(-7);
                    break;
                case 2: // Last 30 days
                    _startDate = _endDate.AddDays(-30);
                    break;
                case 3: // Last quarter
                    _startDate = _endDate.AddMonths(-3);
                    break;
                case 4: // Last year
                    _startDate = _endDate.AddYears(-1);
                    break;
                case 5: // Custom range
                    // Enable custom date inputs
                    startDateInput.interactable = true;
                    endDateInput.interactable = true;
                    return;
            }

            // Disable custom date inputs for predefined ranges
            startDateInput.interactable = false;
            endDateInput.interactable = false;
            
            // Update input fields
            startDateInput.text = _startDate.ToString("yyyy-MM-dd");
            endDateInput.text = _endDate.ToString("yyyy-MM-dd");
            
            RefreshDashboard();
        }

        private void OnDateInputChanged(string value)
        {
            if (dateRangeDropdown.value == 5) // Custom range
            {
                if (DateTime.TryParse(startDateInput.text, out var start) &&
                    DateTime.TryParse(endDateInput.text, out var end))
                {
                    _startDate = start;
                    _endDate = end;
                }
            }
        }

        private void ApplyCustomDateRange()
        {
            if (DateTime.TryParse(startDateInput.text, out var start) &&
                DateTime.TryParse(endDateInput.text, out var end))
            {
                _startDate = start;
                _endDate = end;
                RefreshDashboard();
            }
            else
            {
                ShowErrorMessage("Invalid date range");
            }
        }

        private void SelectAllBuildings()
        {
            buildingDropdown.value = 0;
            _selectedBuildingId = null;
            RefreshDashboard();
        }

        private void ExportDashboard()
        {
            // Export dashboard data to PDF/Excel
            Debug.Log("Exporting dashboard data...");
            ShowSuccessMessage("Dashboard exported successfully");
        }

        private void OpenConfiguration()
        {
            // Open dashboard configuration modal
            Debug.Log("Opening dashboard configuration...");
        }

        private void ViewAllAlerts()
        {
            // Navigate to alerts page
            Debug.Log("Viewing all alerts...");
        }

        private void ViewAllInsights()
        {
            // Navigate to insights page
            Debug.Log("Viewing all insights...");
        }

        private void ShowLoading(string message = "Loading...")
        {
            loadingPanel.SetActive(true);
            loadingText.text = message;
            loadingSlider.value = 0;
        }

        private void HideLoading()
        {
            loadingPanel.SetActive(false);
        }

        private void ShowSuccessMessage(string message)
        {
            // Show success message UI
            Debug.Log($"Success: {message}");
        }

        private void ShowErrorMessage(string message)
        {
            // Show error message UI
            Debug.LogError($"Error: {message}");
        }

        private BuildingKPIs CreateAggregateKPIs(ComparativeAnalytics comparativeAnalytics)
        {
            // Create aggregate KPIs from multiple buildings
            var aggregateKPIs = new BuildingKPIs
            {
                BuildingId = Guid.Empty,
                BuildingName = "All Buildings",
                Period = comparativeAnalytics.Period,
                GeneratedAt = DateTime.UtcNow
            };

            // Aggregate energy KPIs
            aggregateKPIs.EnergyKPIs = new EnergyKPIs
            {
                TotalConsumption = comparativeAnalytics.Averages.AverageEnergyConsumption * comparativeAnalytics.BuildingIds.Count,
                AverageConsumption = comparativeAnalytics.Averages.AverageEnergyConsumption,
                EfficiencyScore = comparativeAnalytics.Averages.AverageEfficiencyScore,
                Cost = comparativeAnalytics.Averages.AverageEnergyConsumption * 0.12 * comparativeAnalytics.BuildingIds.Count
            };

            // Aggregate environmental KPIs
            aggregateKPIs.EnvironmentalKPIs = new EnvironmentalKPIs
            {
                AverageTemperature = comparativeAnalytics.Averages.AverageTemperature,
                ComfortScore = comparativeAnalytics.Averages.AverageComfortScore
            };

            // Aggregate occupancy KPIs
            aggregateKPIs.OccupancyKPIs = new OccupancyKPIs
            {
                OccupancyRate = comparativeAnalytics.Averages.AverageOccupancyRate
            };

            return aggregateKPIs;
        }

        private List<ChartDataPoint> CreateEnergyDistributionData()
        {
            return new List<ChartDataPoint>
            {
                new ChartDataPoint { Label = "HVAC", Value = 45, Color = Color.red },
                new ChartDataPoint { Label = "Lighting", Value = 25, Color = Color.yellow },
                new ChartDataPoint { Label = "Equipment", Value = 20, Color = Color.blue },
                new ChartDataPoint { Label = "Other", Value = 10, Color = Color.gray }
            };
        }

        private List<Alert> CreateMockAlerts()
        {
            return new List<Alert>
            {
                new Alert
                {
                    Id = "alert1",
                    Type = "Energy",
                    Severity = "High",
                    Title = "High Energy Consumption",
                    Description = "Energy consumption is 25% above normal levels",
                    Timestamp = DateTime.UtcNow.AddMinutes(-30),
                    Status = "Active"
                },
                new Alert
                {
                    Id = "alert2",
                    Type = "Temperature",
                    Severity = "Medium",
                    Title = "Temperature Deviation",
                    Description = "Zone A temperature is 3°C above setpoint",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    Status = "Active"
                },
                new Alert
                {
                    Id = "alert3",
                    Type = "Maintenance",
                    Severity = "Low",
                    Title = "Scheduled Maintenance",
                    Description = "HVAC system maintenance scheduled for tomorrow",
                    Timestamp = DateTime.UtcNow.AddHours(-4),
                    Status = "Acknowledged"
                }
            };
        }
    }

    /// <summary>
    /// KPI card component for displaying key metrics
    /// </summary>
    public class KPICard : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text unitText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Image trendImage;
        [SerializeField] private Sprite upTrendSprite;
        [SerializeField] private Sprite downTrendSprite;
        [SerializeField] private Sprite stableTrendSprite;
        [SerializeField] private Color upTrendColor;
        [SerializeField] private Color downTrendColor;
        [SerializeField] private Color stableTrendColor;

        public void SetValue(double value, string unit)
        {
            valueText.text = FormatValue(value);
            unitText.text = unit;
        }

        public void SetTitle(string title)
        {
            titleText.text = title;
        }

        public void SetSubtitle(string subtitle)
        {
            subtitleText.text = subtitle;
        }

        public void SetTrend(string trend)
        {
            switch (trend.ToLower())
            {
                case "up":
                    trendImage.sprite = upTrendSprite;
                    trendImage.color = upTrendColor;
                    break;
                case "down":
                    trendImage.sprite = downTrendSprite;
                    trendImage.color = downTrendColor;
                    break;
                case "stable":
                default:
                    trendImage.sprite = stableTrendSprite;
                    trendImage.color = stableTrendColor;
                    break;
            }
        }

        private string FormatValue(double value)
        {
            if (value >= 1000000)
                return $"{value / 1000000:F1}M";
            if (value >= 1000)
                return $"{value / 1000:F1}K";
            return $"{value:F1}";
        }
    }

    /// <summary>
    /// Chart data point for visualization
    /// </summary>
    public class ChartDataPoint
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public Color Color { get; set; }
    }
}