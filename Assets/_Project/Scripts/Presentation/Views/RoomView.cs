using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Presentation.Views
{
    /// <summary>
    /// Room View Component
    /// 
    /// Architectural Intent:
    /// - Handles 3D visualization of room and environmental data
    /// - Manages visual representation of room state and conditions
    /// - Provides interactive visualization for room monitoring
    /// - Displays real-time sensor data with visual feedback
    /// 
    /// Key Design Decisions:
    /// 1. Component-based visualization system
    /// 2. Color-coded environmental indicators
    /// 3. Material-based state visualization
    /// 4. Interactive elements for user engagement
    /// </summary>
    public class RoomView : MonoBehaviour
    {
        [Header("Room Visualization")]
        [SerializeField] private MeshRenderer _roomRenderer;
        [SerializeField] private Material _defaultMaterial;
        [SerializeField] private Material _occupiedMaterial;
        [SerializeField] private Material _unoccupiedMaterial;
        
        [Header("Environmental Indicators")]
        [SerializeField] private GameObject _temperatureIndicator;
        [SerializeField] private GameObject _humidityIndicator;
        [SerializeField] private GameObject _lightLevelIndicator;
        [SerializeField] private GameObject _airQualityIndicator;
        
        [Header("UI Elements")]
        [SerializeField] private Canvas _roomCanvas;
        [SerializeField] private TextMeshProUGUI _roomNameText;
        [SerializeField] private TextMeshProUGUI _occupancyText;
        [SerializeField] private TextMeshProUGUI _temperatureText;
        [SerializeField] private TextMeshProUGUI _humidityText;
        [SerializeField] private TextMeshProUGUI _lightText;
        [SerializeField] private TextMeshProUGUI _airQualityText;
        
        [Header("Interaction")]
        [SerializeField] private Collider _roomCollider;
        [SerializeField] private GameObject _selectionHighlight;
        [SerializeField] private bool _enableHoverEffects = true;

        // Private fields
        private Room _room;
        private RoomController _roomController;
        private EnvironmentalConditions _currentConditions;
        private bool _isRoomSelected = false;
        private bool _isHovering = false;
        private Color _originalRoomColor;
        private Camera _mainCamera;

        // Properties
        public Room Room => _room;
        public bool IsOccupied => _room?.IsOccupied ?? false;
        public EnvironmentalConditions CurrentConditions => _currentConditions;

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
            HideUI();
        }

        private void Update()
        {
            HandleUserInput();
            UpdateHoverEffects();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        // Public Methods
        public void Initialize(RoomController roomController)
        {
            _roomController = roomController ?? throw new ArgumentNullException(nameof(roomController));
            _room = roomController.Room;
            _currentConditions = _room.CurrentConditions;
            
            UpdateRoomInfo();
            UpdateEnvironmentalDisplay();
            UpdateOccupancyVisual();
        }

        public void UpdateRoom(Room room, EnvironmentalConditions conditions)
        {
            _room = room;
            _currentConditions = conditions;
            
            UpdateRoomInfo();
            UpdateEnvironmentalDisplay();
            UpdateOccupancyVisual();
        }

        public void UpdateOccupancy(bool occupied)
        {
            UpdateOccupancyVisual();
            UpdateOccupancyText();
        }

        public void ShowRoomDetails(Room room, EnvironmentalConditions conditions)
        {
            _isRoomSelected = true;
            
            if (_selectionHighlight != null)
                _selectionHighlight.SetActive(true);
            
            ShowDetailedUI();
        }

        public void HideRoomDetails()
        {
            _isRoomSelected = false;
            
            if (_selectionHighlight != null)
                _selectionHighlight.SetActive(false);
            
            HideDetailedUI();
        }

        public void UpdateRealTimeData()
        {
            UpdateEnvironmentalDisplay();
        }

        // Private Methods
        private void InitializeComponents()
        {
            // Get required components
            if (_roomRenderer == null)
                _roomRenderer = GetComponent<MeshRenderer>();
            
            if (_roomCollider == null)
                _roomCollider = GetComponent<Collider>();
            
            // Store original color
            if (_roomRenderer != null)
            {
                _originalRoomColor = _roomRenderer.material.color;
            }
            
            // Validate materials
            if (_defaultMaterial == null && _roomRenderer != null)
                _defaultMaterial = _roomRenderer.material;
        }

        private void UpdateRoomInfo()
        {
            if (_roomNameText != null && _room != null)
            {
                _roomNameText.text = _room.Name;
            }
        }

        private void UpdateEnvironmentalDisplay()
        {
            UpdateTemperatureDisplay();
            UpdateHumidityDisplay();
            UpdateLightLevelDisplay();
            UpdateAirQualityDisplay();
            UpdateEnvironmentalIndicators();
        }

        private void UpdateTemperatureDisplay()
        {
            if (_temperatureText != null)
            {
                _temperatureText.text = $"{_currentConditions.Temperature.Celsius.Value:F1}°C";
                
                // Color code based on comfort range
                var temp = _currentConditions.Temperature.Celsius.Value;
                var color = temp switch
                {
                    < 18 => Color.blue,
                    >= 18 and < 24 => Color.green,
                    >= 24 and < 28 => Color.yellow,
                    _ => Color.red
                };
                
                _temperatureText.color = color;
            }
        }

        private void UpdateHumidityDisplay()
        {
            if (_humidityText != null)
            {
                _humidityText.text = $"{_currentConditions.Humidity:F0}%";
                
                var humidity = _currentConditions.Humidity;
                var color = humidity switch
                {
                    < 30 => Color.red,
                    >= 30 and < 60 => Color.green,
                    >= 60 and < 70 => Color.yellow,
                    _ => Color.red
                };
                
                _humidityText.color = color;
            }
        }

        private void UpdateLightLevelDisplay()
        {
            if (_lightText != null)
            {
                _lightText.text = $"{_currentConditions.LightLevel:F0} lux";
                
                var light = _currentConditions.LightLevel;
                var color = light switch
                {
                    < 300 => Color.red,
                    >= 300 and < 1000 => Color.yellow,
                    _ => Color.green
                };
                
                _lightText.color = color;
            }
        }

        private void UpdateAirQualityDisplay()
        {
            if (_airQualityText != null)
            {
                _airQualityText.text = $"AQI: {_currentConditions.AirQuality:F0}";
                
                var airQuality = _currentConditions.AirQuality;
                var color = airQuality switch
                {
                    < 50 => Color.green,
                    >= 50 and < 100 => Color.yellow,
                    >= 100 and < 150 => Color.orange,
                    _ => Color.red
                };
                
                _airQualityText.color = color;
            }
        }

        private void UpdateEnvironmentalIndicators()
        {
            UpdateTemperatureIndicator();
            UpdateHumidityIndicator();
            UpdateLightLevelIndicator();
            UpdateAirQualityIndicator();
        }

        private void UpdateTemperatureIndicator()
        {
            if (_temperatureIndicator == null) return;

            var temp = _currentConditions.Temperature.Celsius.Value;
            var indicatorRenderer = _temperatureIndicator.GetComponent<Renderer>();
            
            if (indicatorRenderer != null)
            {
                var color = temp switch
                {
                    < 18 => Color.blue,
                    >= 18 and < 24 => Color.green,
                    >= 24 and < 28 => Color.yellow,
                    _ => Color.red
                };
                
                indicatorRenderer.material.color = color;
                
                // Scale indicator based on temperature deviation
                var deviation = Math.Abs(temp - 22); // 22°C is ideal
                var scale = Mathf.Clamp(1f + (float)deviation * 0.05f, 0.5f, 2f);
                _temperatureIndicator.transform.localScale = Vector3.one * scale;
            }
        }

        private void UpdateHumidityIndicator()
        {
            if (_humidityIndicator == null) return;

            var humidity = _currentConditions.Humidity;
            var indicatorRenderer = _humidityIndicator.GetComponent<Renderer>();
            
            if (indicatorRenderer != null)
            {
                var color = humidity switch
                {
                    < 30 => Color.red,
                    >= 30 and < 60 => Color.green,
                    >= 60 and < 70 => Color.yellow,
                    _ => Color.red
                };
                
                indicatorRenderer.material.color = color;
                
                // Pulse effect for optimal humidity
                if (humidity >= 40 && humidity <= 60)
                {
                    var pulse = Mathf.Sin(Time.time * 2f) * 0.2f + 1f;
                    _humidityIndicator.transform.localScale = Vector3.one * pulse;
                }
                else
                {
                    _humidityIndicator.transform.localScale = Vector3.one;
                }
            }
        }

        private void UpdateLightLevelIndicator()
        {
            if (_lightLevelIndicator == null) return;

            var light = _currentConditions.LightLevel;
            var indicatorRenderer = _lightLevelIndicator.GetComponent<Renderer>();
            
            if (indicatorRenderer != null)
            {
                var color = light switch
                {
                    < 300 => Color.red,
                    >= 300 and < 1000 => Color.yellow,
                    _ => Color.green
                };
                
                indicatorRenderer.material.color = color;
                
                // Brightness effect based on light level
                var intensity = Mathf.InverseLerp(0, 1500, (float)light);
                indicatorRenderer.material.SetFloat("_Intensity", intensity);
            }
        }

        private void UpdateAirQualityIndicator()
        {
            if (_airQualityIndicator == null) return;

            var airQuality = _currentConditions.AirQuality;
            var indicatorRenderer = _airQualityIndicator.GetComponent<Renderer>();
            
            if (indicatorRenderer != null)
            {
                var color = airQuality switch
                {
                    < 50 => Color.green,
                    >= 50 and < 100 => Color.yellow,
                    >= 100 and < 150 => Color.orange,
                    _ => Color.red
                };
                
                indicatorRenderer.material.color = color;
                
                // Warning animation for poor air quality
                if (airQuality >= 100)
                {
                    var warning = Mathf.Sin(Time.time * 3f) * 0.5f + 0.5f;
                    indicatorRenderer.material.SetFloat("_Warning", warning);
                }
                else
                {
                    indicatorRenderer.material.SetFloat("_Warning", 0f);
                }
            }
        }

        private void UpdateOccupancyVisual()
        {
            if (_room == null || _roomRenderer == null) return;

            var targetMaterial = _room.IsOccupied ? _occupiedMaterial : _unoccupiedMaterial;
            
            if (targetMaterial != null)
            {
                _roomRenderer.material = targetMaterial;
            }
            else
            {
                // Fallback to color changes
                var color = _room.IsOccupied ? Color.yellow : Color.white;
                _roomRenderer.material.color = color;
            }
            
            UpdateOccupancyText();
        }

        private void UpdateOccupancyText()
        {
            if (_occupancyText != null && _room != null)
            {
                var status = _room.IsOccupied ? "Occupied" : "Vacant";
                var color = _room.IsOccupied ? Color.red : Color.green;
                
                _occupancyText.text = status;
                _occupancyText.color = color;
            }
        }

        private void HandleUserInput()
        {
            if (!_enableHoverEffects) return;

            HandleRoomSelection();
        }

        private void HandleRoomSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider == _roomCollider)
                    {
                        OnRoomClicked();
                    }
                }
            }
        }

        private void UpdateHoverEffects()
        {
            if (!_enableHoverEffects) return;

            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            var isHovering = Physics.Raycast(ray, out RaycastHit hit) && hit.collider == _roomCollider;

            if (isHovering != _isHovering)
            {
                _isHovering = isHovering;
                UpdateHoverVisual();
            }
        }

        private void UpdateHoverVisual()
        {
            if (_roomRenderer == null) return;

            if (_isHovering)
            {
                // Highlight effect
                var highlightColor = Color.Lerp(_originalRoomColor, Color.white, 0.3f);
                _roomRenderer.material.color = highlightColor;
                
                // Show tooltip
                ShowRoomTooltip();
            }
            else
            {
                // Restore original color
                if (!_isRoomSelected)
                {
                    _roomRenderer.material.color = _originalRoomColor;
                }
                
                // Hide tooltip
                HideRoomTooltip();
            }
        }

        private void OnRoomClicked()
        {
            Debug.Log($"Room clicked: {_room.Name}");
            
            if (_isRoomSelected)
            {
                HideRoomDetails();
            }
            else
            {
                ShowRoomDetails(_room, _currentConditions);
            }
        }

        private void ShowRoomTooltip()
        {
            // This would show a small tooltip with room info
            // For now, just log the action
            Debug.Log($"Hovering over room: {_room.Name}");
        }

        private void HideRoomTooltip()
        {
            // Hide tooltip
        }

        private void ShowDetailedUI()
        {
            // This would show a detailed UI panel with room information
            Debug.Log($"Showing detailed UI for room: {_room.Name}");
            
            var details = $"Room: {_room.Name}\n" +
                        $"Type: {_room.Type}\n" +
                        $"Area: {_room.Area:F0} m²\n" +
                        $"Max Occupancy: {_room.MaxOccupancy}\n" +
                        $"Temperature: {_currentConditions.Temperature.Celsius.Value:F1}°C\n" +
                        $"Humidity: {_currentConditions.Humidity:F0}%\n" +
                        $"Light Level: {_currentConditions.LightLevel:F0} lux\n" +
                        $"Air Quality: {_currentConditions.AirQuality:F0}\n" +
                        $"Comfort Level: {_currentConditions.GetComfortLevel()}";
            
            Debug.Log($"Room Details:\n{details}");
        }

        private void HideDetailedUI()
        {
            // Hide detailed UI panel
        }

        private void HideUI()
        {
            if (_selectionHighlight != null)
                _selectionHighlight.SetActive(false);
        }

        private void Cleanup()
        {
            // Restore original material color
            if (_roomRenderer != null)
            {
                _roomRenderer.material.color = _originalRoomColor;
            }
        }
    }
}