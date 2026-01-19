using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.ValueObjects;
using DigitalTwin.Core.Interfaces;
using DG.Tweening; // For smooth animations

namespace DigitalTwin.Presentation.UI
{
    /// <summary>
    /// Main Digital Twin Dashboard Manager
    /// 
    /// Architectural Intent:
    /// - Provides centralized management for all UI panels and interactions
    /// - Implements reactive UI updates based on real-time data
    /// - Ensures consistent visual design and user experience
    /// - Supports multiple display modes and configurations
    /// 
    /// Key Design Decisions:
    /// 1. Panel-based architecture for modularity
    /// 2. Event-driven UI updates
    /// 3. Smooth animations and transitions
    /// 4. Responsive design for different screen sizes
    /// </summary>
    public class DigitalTwinDashboardManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject _mainDashboardPanel;
        [SerializeField] private GameObject _buildingInfoPanel;
        [SerializeField] private GameObject _energyPanel;
        [SerializeField] private GameObject _environmentalPanel;
        [SerializeField] private GameObject _sensorDataPanel;
        [SerializeField] private GameObject _alertsPanel;
        [SerializeField] private GameObject _controlsPanel;

        [Header("Dashboard Components")]
        [SerializeField] private DigitalTwinBuildingOverview _buildingOverview;
        [SerializeField] private DigitalTwinEnergyDisplay _energyDisplay;
        [SerializeField] private DigitalTwinEnvironmentalDisplay _environmentalDisplay;
        [SerializeField] private DigitalTwinSensorGrid _sensorGrid;
        [SerializeField] private DigitalTwinAlertsList _alertsList;
        [SerializeField] private DigitalTwinControlPanel _controlPanel;

        [Header("UI Settings")]
        [SerializeField] private bool _enableAnimations = true;
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private Color _primaryColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);
        [SerializeField] private Color _successColor = Color.green;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;

        // Private fields
        private ServiceLocator _serviceLocator;
        private Building _currentBuilding;
        private Dictionary<string, GameObject> _panels;
        private Dictionary<AlertLevel, List<AlertMessage>> _alertsByLevel;
        private readonly Dictionary<Guid, SensorReading> _latestSensorReadings = new Dictionary<Guid, SensorReading>();
        private bool _isInitialized = false;

        // Events
        public event Action<Building> OnBuildingSelected;
        public event Action<AlertMessage> OnAlertTriggered;
        public event Action<ControlCommand> OnControlCommand;

        private void Start()
        {
            InitializeDashboard();
        }

        private void InitializeDashboard()
        {
            if (_isInitialized) return;

            _serviceLocator = FindObjectOfType<ServiceLocator>();
            if (_serviceLocator == null)
            {
                Debug.LogError("Service Locator not found!");
                return;
            }

            InitializePanels();
            InitializeComponents();
            SubscribeToEvents();
            SetupDefaultView();

            _isInitialized = true;
            Debug.Log("Digital Twin Dashboard initialized successfully!");
        }

        private void InitializePanels()
        {
            _panels = new Dictionary<string, GameObject>
            {
                ["MainDashboard"] = _mainDashboardPanel,
                ["BuildingInfo"] = _buildingInfoPanel,
                ["Energy"] = _energyPanel,
                ["Environmental"] = _environmentalPanel,
                ["SensorData"] = _sensorDataPanel,
                ["Alerts"] = _alertsPanel,
                ["Controls"] = _controlsPanel
            };

            // Initially show main dashboard
            SetPanelVisibility("MainDashboard", true);
            foreach (var kvp in _panels.Where(p => p.Key != "MainDashboard"))
            {
                SetPanelVisibility(kvp.Key, false);
            }
        }

        private void InitializeComponents()
        {
            // Initialize each component with proper references
            if (_buildingOverview != null)
            {
                _buildingOverview.Initialize(this, _primaryColor);
            }

            if (_energyDisplay != null)
            {
                _energyDisplay.Initialize(this, _serviceLocator, _successColor, _warningColor, _errorColor);
            }

            if (_environmentalDisplay != null)
            {
                _environmentalDisplay.Initialize(this, _serviceLocator);
            }

            if (_sensorGrid != null)
            {
                _sensorGrid.Initialize(this, _serviceLocator);
            }

            if (_alertsList != null)
            {
                _alertsList.Initialize(this);
            }

            if (_controlPanel != null)
            {
                _controlPanel.Initialize(this, _serviceLocator);
            }
        }

        private void SubscribeToEvents()
        {
            // Subscribe to data collection events
            var dataService = _serviceLocator.GetService<IDataCollectionService>();
            if (dataService != null)
            {
                dataService.SensorDataReceived += OnSensorDataReceived;
                dataService.SensorStatusChanged += OnSensorStatusChanged;
            }

            // Subscribe to simulation events
            var simulationService = _serviceLocator.GetService<ISimulationService>();
            if (simulationService != null)
            {
                simulationService.SimulationCompleted += OnSimulationCompleted;
            }

            // Subscribe to input events
            var inputManager = FindObjectOfType<DigitalTwinInputManager>();
            if (inputManager != null)
            {
                inputManager.OnBuildingSelected += OnBuildingSelectedHandler;
            }

            _alertsByLevel = new Dictionary<AlertLevel, List<AlertMessage>>
            {
                [AlertLevel.Info] = new List<AlertMessage>(),
                [AlertLevel.Warning] = new List<AlertMessage>(),
                [AlertLevel.Error] = new List<AlertMessage>(),
                [AlertLevel.Critical] = new List<AlertMessage>()
            };
        }

        private void SetupDefaultView()
        {
            ShowMainDashboard();
        }

        // Public API Methods
        public void SetBuilding(Building building)
        {
            _currentBuilding = building;
            OnBuildingSelected?.Invoke(building);

            // Update all components with new building data
            if (_buildingOverview != null)
                _buildingOverview.UpdateBuilding(building);

            if (_energyDisplay != null)
                _energyDisplay.SetBuilding(building);

            if (_environmentalDisplay != null)
                _environmentalDisplay.SetBuilding(building);
        }

        public void ShowMainDashboard()
        {
            ShowPanel("MainDashboard");
        }

        public void ShowBuildingInfo()
        {
            ShowPanel("BuildingInfo");
        }

        public void ShowEnergyPanel()
        {
            ShowPanel("Energy");
        }

        public void ShowEnvironmentalPanel()
        {
            ShowPanel("Environmental");
        }

        public void ShowSensorData()
        {
            ShowPanel("SensorData");
        }

        public void ShowAlerts()
        {
            ShowPanel("Alerts");
        }

        public void ShowControls()
        {
            ShowPanel("Controls");
        }

        public void AddAlert(AlertMessage alert)
        {
            if (_alertsByLevel.ContainsKey(alert.Level))
            {
                _alertsByLevel[alert.Level].Add(alert);
                _alertsList?.AddAlert(alert);
                OnAlertTriggered?.Invoke(alert);

                // Show alert notification
                ShowAlertNotification(alert);
            }
        }

        public void UpdateSensorReading(SensorReading reading)
        {
            _latestSensorReadings[reading.SensorId] = reading;
            _sensorGrid?.UpdateSensorReading(reading);
        }

        public void UpdateEnergyData(decimal currentConsumption, decimal dailyTotal, decimal monthlyTotal)
        {
            _energyDisplay?.UpdateEnergyData(currentConsumption, dailyTotal, monthlyTotal);
        }

        public void UpdateEnvironmentalData(EnvironmentalConditions conditions)
        {
            _environmentalDisplay?.UpdateConditions(conditions);
        }

        // Private Methods
        private void ShowPanel(string panelName)
        {
            if (!_panels.ContainsKey(panelName)) return;

            foreach (var kvp in _panels)
            {
                SetPanelVisibility(kvp.Key, kvp.Key == panelName);
            }
        }

        private void SetPanelVisibility(string panelName, bool visible)
        {
            if (_panels.TryGetValue(panelName, out GameObject panel))
            {
                if (_enableAnimations)
                {
                    if (visible)
                    panel.transform.localScale = Vector3.zero;

                    panel.SetActive(true);
                    panel.transform.DOScale(Vector3.one, _animationDuration).SetEase(Ease.OutBack);
                }
                else
                {
                    panel.transform.DOScale(Vector3.zero, _animationDuration).SetEase(Ease.InBack)
                        .OnComplete(() => panel.SetActive(false));
                }
            }
            else
            {
                panel?.SetActive(visible);
            }
        }

        private void ShowAlertNotification(AlertMessage alert)
        {
            // Create temporary alert notification popup
            var notificationGO = new GameObject("AlertNotification");
            notificationGO.transform.SetParent(transform);

            var notification = notificationGO.AddComponent<AlertNotification>();
            notification.Initialize(alert, GetAlertColor(alert.Level), 5f); // Auto-dismiss after 5 seconds
        }

        private Color GetAlertColor(AlertLevel level)
        {
            return level switch
            {
                AlertLevel.Info => Color.blue,
                AlertLevel.Warning => _warningColor,
                AlertLevel.Error => _errorColor,
                AlertLevel.Critical => Color.magenta,
                _ => Color.white
            };
        }

        // Event Handlers
        private void OnBuildingSelectedHandler(GameObject buildingGO)
        {
            var controller = buildingGO?.GetComponent<BuildingController>();
            var building = controller?.Building;
            if (building != null)
            {
                SetBuilding(building);
            }
        }

        private void OnSensorDataReceived(SensorReading reading)
        {
            UpdateSensorReading(reading);
        }

        private void OnSensorStatusChanged(Guid sensorId, SensorStatus status)
        {
            _sensorGrid?.UpdateSensorStatus(sensorId, status);
        }

        private void OnSimulationCompleted(SimulationResult result)
        {
            if (result is EnergySimulationResult energyResult)
            {
                _energyDisplay?.UpdateSimulationResult(energyResult);
            }
            else if (result is EnvironmentalSimulationResult envResult)
            {
                _environmentalDisplay?.UpdateSimulationResult(envResult);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            var dataService = _serviceLocator?.GetService<IDataCollectionService>();
            if (dataService != null)
            {
                dataService.SensorDataReceived -= OnSensorDataReceived;
                dataService.SensorStatusChanged -= OnSensorStatusChanged;
            }

            var simulationService = _serviceLocator?.GetService<ISimulationService>();
            if (simulationService != null)
            {
                simulationService.SimulationCompleted -= OnSimulationCompleted;
            }
        }
    }

    /// <summary>
    /// Alert Notification Component
    /// </summary>
    public class AlertNotification : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _alertText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private float _lifetime = 5f;

        private float _currentTime;

        public void Initialize(AlertMessage alert, Color backgroundColor, float lifetime)
        {
            _alertText.text = $"{alert.Level}: {alert.Message}";
            _backgroundImage.color = backgroundColor;
            _lifetime = lifetime;
            _currentTime = 0f;

            // Position at top right of screen
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(-10, -10);
        }

        private void Update()
        {
            _currentTime += Time.deltaTime;
            
            // Fade out near end of lifetime
            if (_currentTime > _lifetime - 1f)
            {
                var alpha = Mathf.Lerp(1f, 0f, _currentTime - (_lifetime - 1f));
                _backgroundImage.color = new Color(_backgroundImage.color.r, _backgroundImage.color.g, _backgroundImage.color.b, alpha);
                _alertText.color = new Color(_alertText.color.r, _alertText.color.g, _alertText.color.b, alpha);
            }

            // Destroy after lifetime
            if (_currentTime >= _lifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Alert Data Structure
    /// </summary>
    [System.Serializable]
    public class AlertMessage
    {
        public string Id;
        public AlertLevel Level;
        public string Title;
        public string Message;
        public DateTime Timestamp;
        public string Source;
        public Dictionary<string, object> Data;

        public AlertMessage(string title, string message, AlertLevel level, string source = null)
        {
            Id = Guid.NewGuid().ToString();
            Title = title;
            Message = message;
            Level = level;
            Timestamp = DateTime.UtcNow;
            Source = source ?? "System";
            Data = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Control Command Data Structure
    /// </summary>
    [System.Serializable]
    public class ControlCommand
    {
        public string Type;
        public string Target;
        public Dictionary<string, object> Parameters;
        public DateTime Timestamp;

        public ControlCommand(string type, string target, Dictionary<string, object> parameters = null)
        {
            Type = type;
            Target = target;
            Parameters = parameters ?? new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Alert Level Enum
    /// </summary>
    public enum AlertLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }
}