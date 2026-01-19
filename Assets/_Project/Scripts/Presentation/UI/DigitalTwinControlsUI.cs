using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Presentation.UI
{
    /// <summary>
    /// Alerts List Component
    /// 
    /// Architectural Intent:
    /// - Displays active and historical alerts
    /// - Provides alert filtering and sorting
    /// - Shows alert details and actions
    /// - Enables alert acknowledgment and resolution
    /// </summary>
    public class DigitalTwinAlertsList : MonoBehaviour
    {
        [Header("Alert Display")]
        [SerializeField] private Transform _alertItemsContainer;
        [SerializeField] private GameObject _alertItemPrefab;
        [SerializeField] private TextMeshProUGUI _alertCountText;
        [SerializeField] private TextMeshProUGUI _activeAlertsText;

        [Header("Filter Controls")]
        [SerializeField] private TMP_Dropdown _levelFilter;
        [SerializeField] private TMP_Dropdown _timeFilter;
        [SerializeField] private Toggle _showResolvedToggle;

        [Header("Alert Management")]
        [SerializeField] private Button _clearAllButton;
        [SerializeField] private Button _exportButton;

        private DigitalTwinDashboardManager _dashboardManager;
        private List<AlertMessage> _activeAlerts = new List<AlertMessage>();
        private List<AlertMessage> _historicalAlerts = new List<AlertMessage>();
        private List<AlertItem> _alertItems = new List<AlertItem>();
        private AlertLevel _currentLevelFilter = AlertLevel.Info;
        private TimeSpan _currentTimeFilter = TimeSpan.FromHours(24);

        public void Initialize(DigitalTwinDashboardManager dashboard)
        {
            _dashboardManager = dashboard;
            SetupFilterControls();
            SetupManagementButtons();
            RefreshAlertsList();
        }

        public void AddAlert(AlertMessage alert)
        {
            _activeAlerts.Add(alert);
            CreateAlertItem(alert);
            UpdateAlertCount();
            AutoScrollToTop();
        }

        private void SetupFilterControls()
        {
            // Level filter
            if (_levelFilter != null)
            {
                var levelOptions = new List<TMP_Dropdown.OptionData>
                {
                    new TMP_Dropdown.OptionData("All Levels"),
                    new TMP_Dropdown.OptionData("Critical"),
                    new TMP_Dropdown.OptionData("Error"),
                    new TMP_Dropdown.OptionData("Warning"),
                    new TMP_Dropdown.OptionData("Info")
                };

                _levelFilter.options = levelOptions;
                _levelFilter.onValueChanged.AddListener(OnLevelFilterChanged);
            }

            // Time filter
            if (_timeFilter != null)
            {
                var timeOptions = new List<TMP_Dropdown.OptionData>
                {
                    new TMP_Dropdown.OptionData("Last Hour"),
                    new TMP_Dropdown.OptionData("Last 6 Hours"),
                    new TMP_Dropdown.OptionData("Last 24 Hours"),
                    new TMP_Dropdown.OptionData("Last Week"),
                    new TMP_Dropdown.OptionData("Last Month")
                };

                _timeFilter.options = timeOptions;
                _timeFilter.onValueChanged.AddListener(OnTimeFilterChanged);
            }

            // Show resolved toggle
            if (_showResolvedToggle != null)
            {
                _showResolvedToggle.onValueChanged.AddListener(OnShowResolvedChanged);
            }
        }

        private void SetupManagementButtons()
        {
            if (_clearAllButton != null)
                _clearAllButton.onClick.AddListener(OnClearAllClicked);

            if (_exportButton != null)
                _exportButton.onClick.AddListener(OnExportClicked);
        }
        private void OnLevelFilterChanged(int index)
        {
            if (index == 0)
                _currentLevelFilter = AlertLevel.Info; // "All Levels"
            else
                _currentLevelFilter = (AlertLevel)(index - 1);

            RefreshAlertsList();
        }

        private void OnTimeFilterChanged(int index)
        {
            _currentTimeFilter = index switch
            {
                0 => TimeSpan.FromHours(1),
                1 => TimeSpan.FromHours(6),
                2 => TimeSpan.FromHours(24),
                3 => TimeSpan.FromDays(7),
                4 => TimeSpan.FromDays(30),
                _ => TimeSpan.FromHours(24)
            };

            RefreshAlertsList();
        }

        private void OnShowResolvedChanged(bool showResolved)
        {
            RefreshAlertsList();
        }

        private void OnClearAllClicked()
        {
            ClearAllAlerts();
        }

        private void OnExportClicked()
        {
            ExportAlerts();
        }

        private void CreateAlertItem(AlertMessage alert)
        {
            var alertGO = Instantiate(_alertItemPrefab, _alertItemsContainer);
            var alertItem = alertGO.AddComponent<AlertItem>();
            alertItem.Initialize(alert, OnAlertAction);
            _alertItems.Add(alertItem);
        }

        private void RefreshAlertsList()
        {
            // Clear existing items
            ClearAlertItems();

            // Filter alerts
            var filteredAlerts = GetFilteredAlerts();

            // Create alert items
            foreach (var alert in filteredAlerts.OrderByDescending(a => a.Timestamp))
            {
                CreateAlertItem(alert);
            }

            UpdateAlertCount();
        }

        private List<AlertMessage> GetFilteredAlerts()
        {
            var allAlerts = new List<AlertMessage>();
            allAlerts.AddRange(_activeAlerts);
            
            if (_showResolvedToggle != null && _showResolvedToggle.isOn)
                allAlerts.AddRange(_historicalAlerts);

            var cutoffTime = DateTime.UtcNow - _currentTimeFilter;

            return allAlerts.Where(alert =>
                (_currentLevelFilter == AlertLevel.Info || alert.Level == _currentLevelFilter) &&
                alert.Timestamp >= cutoffTime
            ).ToList();
        }

        private void ClearAlertItems()
        {
            foreach (Transform child in _alertItemsContainer)
            {
                Destroy(child.gameObject);
            }
            _alertItems.Clear();
        }

        private void ClearAllAlerts()
        {
            _historicalAlerts.AddRange(_activeAlerts);
            _activeAlerts.Clear();
            RefreshAlertsList();
        }

        private void UpdateAlertCount()
        {
            if (_alertCountText != null)
                _alertCountText.text = _activeAlerts.Count.ToString();

            if (_activeAlertsText != null)
                _activeAlertsText.text = $"{_activeAlerts.Count} Active Alerts";
        }

        private void AutoScrollToTop()
        {
            // Scroll to top when new alert is added
            var scrollRect = _alertItemsContainer.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1;
            }
        }

        private void OnAlertAction(AlertMessage alert, string action)
        {
            switch (action)
            {
                case "Acknowledge":
                    AcknowledgeAlert(alert);
                    break;
                case "Resolve":
                    ResolveAlert(alert);
                    break;
                case "Details":
                    ShowAlertDetails(alert);
                    break;
            }
        }

        private void AcknowledgeAlert(AlertMessage alert)
        {
            // Mark alert as acknowledged
            Debug.Log($"Alert acknowledged: {alert.Title}");
        }

        private void ResolveAlert(AlertMessage alert)
        {
            // Move alert to historical and remove from active
            _activeAlerts.Remove(alert);
            _historicalAlerts.Add(alert);
            RefreshAlertsList();
        }

        private void ShowAlertDetails(AlertMessage alert)
        {
            // Show detailed alert information
            Debug.Log($"Show alert details: {alert.Title}");
        }

        private void ExportAlerts()
        {
            // Export alerts to CSV or other format
            Debug.Log("Export alerts");
        }
    }

    /// <summary>
    /// Alert Item Component
    /// </summary>
    public class AlertItem : MonoBehaviour
    {
        [SerializeField] private Image _levelIndicator;
        [SerializeField] private TextMeshProUGUI _alertTitleText;
        [SerializeField] private TextMeshProUGUI _alertMessageText;
        [SerializeField] private TextMeshProUGUI _timestampText;
        [SerializeField] private Button _acknowledgeButton;
        [SerializeField] private Button _resolveButton;
        [SerializeField] private Button _detailsButton;

        private AlertMessage _alert;
        private System.Action<AlertMessage, string> _onAction;

        public void Initialize(AlertMessage alert, System.Action<AlertMessage, string> onAction)
        {
            _alert = alert;
            _onAction = onAction;

            UpdateDisplay();
            SetupButtons();
        }

        private void UpdateDisplay()
        {
            _alertTitleText.text = _alert.Title;
            _alertMessageText.text = _alert.Message;
            _timestampText.text = _alert.Timestamp.ToString("MMM dd, HH:mm");

            // Update level indicator
            if (_levelIndicator != null)
            {
                var levelColor = GetAlertColor(_alert.Level);
                _levelIndicator.color = levelColor;
            }
        }

        private void SetupButtons()
        {
            if (_acknowledgeButton != null)
            {
                _acknowledgeButton.onClick.AddListener(() => _onAction?.Invoke(_alert, "Acknowledge"));
            }

            if (_resolveButton != null)
            {
                _resolveButton.onClick.AddListener(() => _onAction?.Invoke(_alert, "Resolve"));
            }

            if (_detailsButton != null)
            {
                _detailsButton.onClick.AddListener(() => _onAction?.Invoke(_alert, "Details"));
            }
        }

        private Color GetAlertColor(AlertLevel level)
        {
            return level switch
            {
                AlertLevel.Critical => Color.magenta,
                AlertLevel.Error => Color.red,
                AlertLevel.Warning => Color.yellow,
                AlertLevel.Info => Color.blue,
                _ => Color.gray
            };
        }
    }

    /// <summary>
    /// Control Panel Component
    /// 
    /// Architectural Intent:
    /// - Provides control interface for building systems
    /// - Enables equipment control and configuration
    /// - Shows control permissions and access levels
    /// - Supports both manual and automated control modes
    /// </summary>
    public class DigitalTwinControlPanel : MonoBehaviour
    {
        [Header("Control Categories")]
        [SerializeField] private Transform _controlTabsContainer;
        [SerializeField] private GameObject _hvacControls;
        [SerializeField] private GameObject _lightingControls;
        [SerializeField] private GameObject _securityControls;
        [SerializeField] private GameObject _emergencyControls;

        [Header("HVAC Controls")]
        [SerializeField] private Slider _targetTemperatureSlider;
        [SerializeField] private TextMeshProUGUI _targetTemperatureText;
        [SerializeField] private Toggle _hvacAutoToggle;
        [SerializeField] private Button _applyHVACButton;

        [Header("Lighting Controls")]
        [SerializeField] private Slider _lightingLevelSlider;
        [SerializeField] private TextMeshProUGUI _lightingLevelText;
        [SerializeField] private Toggle _autoLightingToggle;
        [SerializeField] private Button _applyLightingButton;

        [Header("Security Controls")]
        [SerializeField] private Button _lockAllButton;
        [SerializeField] private Button _unlockAllButton;
        [SerializeField] private Button _armSystemButton;
        [SerializeField] private Button _disarmSystemButton;

        [Header("Emergency Controls")]
        [SerializeField] private Button _evacuationButton;
        [SerializeField] private Button _fireAlarmButton;
        [SerializeField] private Button _emergencyShutdownButton;

        private DigitalTwinDashboardManager _dashboardManager;
        private ServiceLocator _serviceLocator;
        private string _activeControlTab = "HVAC";

        public void Initialize(DigitalTwinDashboardManager dashboard, ServiceLocator serviceLocator)
        {
            _dashboardManager = dashboard;
            _serviceLocator = serviceLocator;

            SetupControlTabs();
            SetupControlButtons();
            SetActiveControlTab("HVAC");
        }

        private void SetupControlTabs()
        {
            // Create tab buttons
            var tabs = new[] { "HVAC", "Lighting", "Security", "Emergency" };
            
            foreach (var tab in tabs)
            {
                var tabButtonGO = new GameObject($"{tab}Tab");
                tabButtonGO.transform.SetParent(_controlTabsContainer, false);
                
                var button = tabButtonGO.AddComponent<Button>();
                var text = tabButtonGO.AddComponent<TextMeshProUGUI>();
                text.text = tab;
                text.fontSize = 12;
                
                button.onClick.AddListener(() => SetActiveControlTab(tab));
            }
        }

        private void SetupControlButtons()
        {
            // HVAC Controls
            if (_targetTemperatureSlider != null)
            {
                _targetTemperatureSlider.minValue = 16f;
                _targetTemperatureSlider.maxValue = 30f;
                _targetTemperatureSlider.value = 22f;
                _targetTemperatureSlider.onValueChanged.AddListener(OnTargetTemperatureChanged);
            }

            if (_hvacAutoToggle != null)
                _hvacAutoToggle.onValueChanged.AddListener(OnHVACAutoToggleChanged);

            if (_applyHVACButton != null)
                _applyHVACButton.onClick.AddListener(OnApplyHVACSettings);

            // Lighting Controls
            if (_lightingLevelSlider != null)
            {
                _lightingLevelSlider.minValue = 0f;
                _lightingLevelSlider.maxValue = 100f;
                _lightingLevelSlider.value = 75f;
                _lightingLevelSlider.onValueChanged.AddListener(OnLightingLevelChanged);
            }

            if (_autoLightingToggle != null)
                _autoLightingToggle.onValueChanged.AddListener(OnAutoLightingToggleChanged);

            if (_applyLightingButton != null)
                _applyLightingButton.onClick.AddListener(OnApplyLightingSettings);

            // Security Controls
            if (_lockAllButton != null)
                _lockAllButton.onClick.AddListener(OnLockAllClicked);

            if (_unlockAllButton != null)
                _unlockAllButton.onClick.AddListener(OnUnlockAllClicked);

            if (_armSystemButton != null)
                _armSystemButton.onClick.AddListener(OnArmSystemClicked);

            if (_disarmSystemButton != null)
                _disarmSystemButton.onClick.AddListener(OnDisarmSystemClicked);

            // Emergency Controls
            if (_evacuationButton != null)
                _evacuationButton.onClick.AddListener(OnEvacuationClicked);

            if (_fireAlarmButton != null)
                _fireAlarmButton.onClick.AddListener(OnFireAlarmClicked);

            if (_emergencyShutdownButton != null)
                _emergencyShutdownButton.onClick.AddListener(OnEmergencyShutdownClicked);
        }

        private void SetActiveControlTab(string tabName)
        {
            _activeControlTab = tabName;

            // Hide all control panels
            _hvacControls?.SetActive(false);
            _lightingControls?.SetActive(false);
            _securityControls?.SetActive(false);
            _emergencyControls?.SetActive(false);

            // Show active control panel
            switch (tabName)
            {
                case "HVAC":
                    _hvacControls?.SetActive(true);
                    break;
                case "Lighting":
                    _lightingControls?.SetActive(true);
                    break;
                case "Security":
                    _securityControls?.SetActive(true);
                    break;
                case "Emergency":
                    _emergencyControls?.SetActive(true);
                    break;
            }

            UpdateTabButtonStates();
        }

        private void UpdateTabButtonStates()
        {
            // Update tab button appearances
            foreach (Transform child in _controlTabsContainer)
            {
                var button = child.GetComponent<Button>();
                var text = child.GetComponent<TextMeshProUGUI>();
                
                if (text != null && button != null)
                {
                    var isActive = text.text == _activeControlTab;
                    button.interactable = !isActive;
                    
                    if (isActive)
                    {
                        text.color = Color.white;
                        text.fontStyle = FontStyles.Bold;
                    }
                    else
                    {
                        text.color = Color.gray;
                        text.fontStyle = FontStyles.Normal;
                    }
                }
            }
        }

        // HVAC Control Handlers
        private void OnTargetTemperatureChanged(float value)
        {
            if (_targetTemperatureText != null)
                _targetTemperatureText.text = $"{value:F1}°C";
        }

        private void OnHVACAutoToggleChanged(bool isOn)
        {
            Debug.Log($"HVAC Auto mode: {isOn}");
        }

        private void OnApplyHVACSettings()
        {
            var command = new ControlCommand
            {
                Type = "HVAC_SetTargetTemperature",
                Target = "Building",
                Parameters = new Dictionary<string, object>
                {
                    ["Temperature"] = _targetTemperatureSlider.value,
                    ["AutoMode"] = _hvacAutoToggle?.isOn ?? false
                }
            };

            _dashboardManager?.OnControlCommand?.Invoke(command);
        }

        // Lighting Control Handlers
        private void OnLightingLevelChanged(float value)
        {
            if (_lightingLevelText != null)
                _lightingLevelText.text = $"{value:F0}%";
        }

        private void OnAutoLightingToggleChanged(bool isOn)
        {
            Debug.Log($"Auto Lighting mode: {isOn}");
        }

        private void OnApplyLightingSettings()
        {
            var command = new ControlCommand
            {
                Type = "Lighting_SetLevel",
                Target = "Building",
                Parameters = new Dictionary<string, object>
                {
                    ["Level"] = _lightingLevelSlider.value,
                    ["AutoMode"] = _autoLightingToggle?.isOn ?? false
                }
            };

            _dashboardManager?.OnControlCommand?.Invoke(command);
        }

        // Security Control Handlers
        private void OnLockAllClicked()
        {
            SendSecurityCommand("Lock_All");
        }

        private void OnUnlockAllClicked()
        {
            SendSecurityCommand("Unlock_All");
        }

        private void OnArmSystemClicked()
        {
            SendSecurityCommand("Arm_System");
        }

        private void OnDisarmSystemClicked()
        {
            SendSecurityCommand("Disarm_System");
        }

        private void SendSecurityCommand(string command)
        {
            var controlCommand = new ControlCommand
            {
                Type = command,
                Target = "SecuritySystem"
            };

            _dashboardManager?.OnControlCommand?.Invoke(controlCommand);
        }

        // Emergency Control Handlers
        private void OnEvacuationClicked()
        {
            ShowEmergencyConfirmation("Evacuation", "Are you sure you want to trigger building evacuation?");
        }

        private void OnFireAlarmClicked()
        {
            ShowEmergencyConfirmation("FireAlarm", "Are you sure you want to trigger the fire alarm?");
        }

        private void OnEmergencyShutdownClicked()
        {
            ShowEmergencyConfirmation("EmergencyShutdown", "Are you sure you want to initiate emergency shutdown?");
        }

        private void ShowEmergencyConfirmation(string command, string message)
        {
            // Show confirmation dialog
            Debug.Log($"Emergency command: {command} - {message}");
        }
    }

    /// <summary>
    /// Simple Line Chart Component
    /// </summary>
    public class LineChart : MonoBehaviour
    {
        [SerializeField] private RectTransform _chartArea;
        [SerializeField] private GameObject _linePointPrefab;
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private int _maxPoints = 100;

        private List<float> _dataPoints = new List<float>();
        private List<GameObject> _pointObjects = new List<GameObject>();

        public void SetData(List<float> data)
        {
            _dataPoints = data.Take(_maxPoints).ToList();
            DrawChart();
        }

        private void DrawChart()
        {
            ClearChart();

            if (_dataPoints.Count < 2) return;

            // Calculate chart dimensions
            var chartWidth = _chartArea.rect.width;
            var chartHeight = _chartArea.rect.height;

            // Calculate min/max for scaling
            var minValue = _dataPoints.Min();
            var maxValue = _dataPoints.Max();
            var valueRange = maxValue - minValue;
            if (valueRange == 0) valueRange = 1;

            // Create points and line
            var positions = new Vector3[_dataPoints.Count];
            for (int i = 0; i < _dataPoints.Count; i++)
            {
                var x = (float)i / (_dataPoints.Count - 1) * chartWidth;
                var y = ((_dataPoints[i] - minValue) / valueRange) * chartHeight;
                var position = new Vector3(x, y, 0);

                positions[i] = position;

                // Create point visual
                var pointGO = Instantiate(_linePointPrefab, _chartArea);
                pointGO.transform.localPosition = position;
                _pointObjects.Add(pointGO);
            }

            // Set line renderer positions
            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = positions.Length;
                _lineRenderer.SetPositions(positions);
            }
        }

        private void ClearChart()
        {
            foreach (var point in _pointObjects)
            {
                Destroy(point);
            }
            _pointObjects.Clear();

            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = 0;
            }
        }
    }

    // Supporting Data Structures
    public class ZoneInfo
    {
        public string Id;
        public string Name;
        public string Type;
        public EnvironmentalConditions ZoneConditions;
    }
}