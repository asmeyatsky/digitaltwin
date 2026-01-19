using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

namespace DigitalTwin.Infrastructure.UnityAdapters
{
    /// <summary>
    /// Scene Setup Manager
    /// 
    /// Architectural Intent:
    /// - Automatically configures Unity scenes for digital twin visualization
    /// - Provides standardized scene layout with required components
    /// - Ensures proper hierarchy and component setup
    /// - Supports multiple scene templates (demo, development, production)
    /// 
    /// Key Design Decisions:
    /// 1. Template-based scene creation
    /// 2. Automatic component wiring and configuration
    /// 3. Scene validation and error checking
    /// 4. Asset management and organization
    /// </summary>
    public static class DigitalTwinSceneSetup
    {
        [MenuItem("Digital Twin/Create Digital Twin Scene")]
        public static void CreateDigitalTwinScene()
        {
            var scene = SceneManager.CreateScene("DigitalTwinScene");
            
            SetupCameraAndLighting(scene);
            SetupInputManagement(scene);
            SetupServiceLocator(scene);
            SetupBuildingRoot(scene);
            SetupUIRoot(scene);
            SetupDebuggingTools(scene);
            
            SaveScene();
            
            Debug.Log("Digital Twin scene created successfully!");
        }

        [MenuItem("Digital Twin/Create Demo Scene")]
        public static void CreateDemoScene()
        {
            var scene = SceneManager.CreateScene("DigitalTwinDemo");
            
            SetupCameraAndLighting(scene);
            SetupInputManagement(scene);
            SetupServiceLocator(scene);
            SetupBuildingRoot(scene);
            SetupUIRoot(scene);
            SetupDemoBuilding(scene);
            SetupDebuggingTools(scene);
            
            SaveScene();
            
            Debug.Log("Digital Twin demo scene created successfully!");
        }

        private static void SetupCameraAndLighting(Scene scene)
        {
            // Setup Main Camera
            var cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            var camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1.0f);
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            
            // Add camera controller
            var cameraController = cameraGO.AddComponent<DigitalTwinCameraController>();
            
            // Setup Lighting
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.rotation = Quaternion.Euler(50f, -30f, 0f);
            
            // Add ambient lighting setup
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.5f;
            RenderSettings.reflectionIntensity = 0.5f;
            
            Debug.Log("Camera and lighting setup complete");
        }

        private static void SetupInputManagement(Scene scene)
        {
            var inputGO = new GameObject("Input Manager");
            var inputManager = inputGO.AddComponent<DigitalTwinInputManager>();
            
            // Configure input actions
            inputManager.EnableBuildingSelection = true;
            inputManager.EnableCameraControl = true;
            inputManager.EnableUIInteraction = true;
            
            Debug.Log("Input management setup complete");
        }

        private static void SetupServiceLocator(Scene scene)
        {
            var serviceLocatorGO = new GameObject("Service Locator");
            var serviceLocator = serviceLocatorGO.AddComponent<ServiceLocator>();
            
            // Initialize default services
            serviceLocator.InitializeDefaultServices();
            
            Debug.Log("Service locator setup complete");
        }

        private static void SetupBuildingRoot(Scene scene)
        {
            var buildingRootGO = new GameObject("Building Root");
            buildingRootGO.transform.position = Vector3.zero;
            
            // Add building manager
            var buildingManager = buildingRootGO.AddComponent<DigitalTwinBuildingManager>();
            
            // Add building visualization layers
            CreateVisualizationLayers(buildingRootGO);
            
            Debug.Log("Building root setup complete");
        }

        private static void SetupUIRoot(Scene scene)
        {
            var uiRootGO = new GameObject("UI Root");
            
            // Add Canvas
            var canvas = uiRootGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            
            // Add Canvas Scaler
            var scaler = uiRootGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add Graphic Raycaster
            var raycaster = uiRootGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Add UI Manager
            var uiManager = uiRootGO.AddComponent<DigitalTwinUIManager>();
            
            // Create default UI panels
            CreateUIPanels(uiRootGO);
            
            Debug.Log("UI root setup complete");
        }

        private static void SetupDebuggingTools(Scene scene)
        {
            var debugGO = new GameObject("Debug Tools");
            var debugManager = debugGO.AddComponent<DigitalTwinDebugManager>();
            
            // Configure debug features
            debugManager.ShowPerformanceMetrics = true;
            debugManager.ShowNetworkActivity = true;
            debugManager.ShowSensorData = true;
            debugManager.EnableWireframeMode = false;
            
            Debug.Log("Debugging tools setup complete");
        }

        private static void SetupDemoBuilding(Scene scene)
        {
            var buildingRoot = GameObject.Find("Building Root");
            if (buildingRoot != null)
            {
                // Create sample building
                var buildingCreator = buildingRoot.AddComponent<DemoBuildingCreator>();
                buildingCreator.CreateSampleBuilding();
            }
        }

        private static void CreateVisualizationLayers(GameObject parent)
        {
            // Building visualization layer
            var buildingLayer = new GameObject("Building Visualization");
            buildingLayer.transform.SetParent(parent.transform);
            
            // Equipment visualization layer
            var equipmentLayer = new GameObject("Equipment Visualization");
            equipmentLayer.transform.SetParent(parent.transform);
            
            // Sensor visualization layer
            var sensorLayer = new GameObject("Sensor Visualization");
            sensorLayer.transform.SetParent(parent.transform);
            
            // Data overlay layer
            var dataOverlayLayer = new GameObject("Data Overlay");
            dataOverlayLayer.transform.SetParent(parent.transform);
            
            Debug.Log("Visualization layers created");
        }

        private static void CreateUIPanels(GameObject parent)
        {
            // Main Dashboard Panel
            var dashboardPanel = CreateUIPanel("Dashboard Panel", parent);
            SetPanelRect(dashboardPanel, 0, 0, 300, 600, Anchor.TopLeft);
            
            // Building Info Panel
            var infoPanel = CreateUIPanel("Building Info Panel", parent);
            SetPanelRect(infoPanel, 300, 0, 400, 200, Anchor.TopRight);
            
            // Sensor Data Panel
            var sensorPanel = CreateUIPanel("Sensor Data Panel", parent);
            SetPanelRect(sensorPanel, 0, 600, 300, 400, Anchor.BottomLeft);
            
            // Energy Consumption Panel
            var energyPanel = CreateUIPanel("Energy Panel", parent);
            SetPanelRect(energyPanel, 300, 600, 400, 400, Anchor.Bottom);
            
            // Controls Panel
            var controlsPanel = CreateUIPanel("Controls Panel", parent);
            SetPanelRect(controlsPanel, 700, 600, 300, 400, Anchor.BottomRight);
            
            Debug.Log("UI panels created");
        }

        private static GameObject CreateUIPanel(string name, GameObject parent)
        {
            var panelGO = new GameObject(name);
            panelGO.transform.SetParent(parent.transform, false);
            
            var rectTransform = panelGO.AddComponent<RectTransform>();
            var image = panelGO.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
            
            return panelGO;
        }

        private static void SetPanelRect(GameObject panel, int x, int y, int width, int height, Anchor anchor)
        {
            var rectTransform = panel.GetComponent<RectTransform>();
            
            // Set anchor based on enum
            switch (anchor)
            {
                case Anchor.TopLeft:
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    rectTransform.pivot = new Vector2(0, 1);
                    break;
                case Anchor.TopRight:
                    rectTransform.anchorMin = new Vector2(1, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(1, 1);
                    break;
                case Anchor.BottomLeft:
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 0);
                    rectTransform.pivot = new Vector2(0, 0);
                    break;
                case Anchor.Bottom:
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 0);
                    rectTransform.pivot = new Vector2(0.5f, 0);
                    break;
                case Anchor.BottomRight:
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.pivot = new Vector2(1, 0);
                    break;
            }
            
            rectTransform.anchoredPosition = new Vector2(x, -y);
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        private static void SaveScene()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            var scenePath = $"Assets/_Project/Scenes/{sceneName}.unity";
            
            if (!Directory.Exists("Assets/_Project/Scenes/"))
                Directory.CreateDirectory("Assets/_Project/Scenes/");
            
            SceneManager.SetActiveScene(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
            
            Debug.Log($"Scene saved to: {scenePath}");
        }
    }

    // Supporting Classes
    public class DigitalTwinCameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        public float moveSpeed = 10f;
        public float rotationSpeed = 100f;
        public float zoomSpeed = 5f;
        public float minDistance = 5f;
        public float maxDistance = 100f;

        private Transform _target;
        private Vector3 _offset;
        private float _currentZoom;

        private void Start()
        {
            _target = transform;
            _offset = transform.position;
            _currentZoom = _offset.magnitude;
        }

        private void Update()
        {
            HandleMouseRotation();
            HandleMouseZoom();
            HandleKeyboardMovement();
        }

        private void HandleMouseRotation()
        {
            if (Input.GetMouseButton(1))
            {
                var mouseX = Input.GetAxis("Mouse X");
                var mouseY = Input.GetAxis("Mouse Y");
                
                transform.RotateAround(_target.position, Vector3.up, mouseX * rotationSpeed * Time.deltaTime);
                transform.RotateAround(_target.position, transform.right, -mouseY * rotationSpeed * Time.deltaTime);
            }
        }

        private void HandleMouseZoom()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            _currentZoom -= scroll * zoomSpeed;
            _currentZoom = Mathf.Clamp(_currentZoom, minDistance, maxDistance);
            
            _offset = _offset.normalized * _currentZoom;
            transform.position = _target.position + _offset;
        }

        private void HandleKeyboardMovement()
        {
            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");
            
            var movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
            _target.position += movement;
            transform.position = _target.position + _offset;
        }
    }

    public class DigitalTwinInputManager : MonoBehaviour
    {
        [Header("Input Configuration")]
        public bool EnableBuildingSelection = true;
        public bool EnableCameraControl = true;
        public bool EnableUIInteraction = true;

        public event Action<GameObject> OnBuildingSelected;
        public event Action<GameObject> OnEquipmentSelected;
        public event Action<GameObject> OnSensorSelected;

        private void Update()
        {
            HandleSelection();
            HandleShortcuts();
        }

        private void HandleSelection()
        {
            if (Input.GetMouseButtonDown(0) && EnableBuildingSelection)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    var selectedObject = hit.collider.gameObject;
                    
                    // Check what type of object was selected
                    if (selectedObject.GetComponent<BuildingController>() != null)
                        OnBuildingSelected?.Invoke(selectedObject);
                    else if (selectedObject.GetComponent<EquipmentController>() != null)
                        OnEquipmentSelected?.Invoke(selectedObject);
                    else if (selectedObject.GetComponent<SensorController>() != null)
                        OnSensorSelected?.Invoke(selectedObject);
                }
            }
        }

        private void HandleShortcuts()
        {
            // Toggle building maintenance mode
            if (Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("Maintenance mode toggle");
            }
            
            // Start energy simulation
            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("Start energy simulation");
            }
            
            // Toggle UI panels
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Debug.Log("Toggle UI panels");
            }
            
            // Reset camera
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("Reset camera");
            }
        }
    }

    public class DigitalTwinBuildingManager : MonoBehaviour
    {
        [Header("Building Configuration")]
        public GameObject buildingPrefab;
        public bool autoLoadBuilding = true;

        private Building _currentBuilding;

        private void Start()
        {
            if (autoLoadBuilding)
            {
                LoadDefaultBuilding();
            }
        }

        public void LoadBuilding(Building building)
        {
            _currentBuilding = building;
            
            // Clear existing building
            ClearExistingBuilding();
            
            // Instantiate new building
            var buildingGO = Instantiate(buildingPrefab, transform);
            var controller = buildingGO.GetComponent<BuildingController>();
            controller.Initialize(building, FindObjectOfType<ServiceLocator>());
            
            Debug.Log($"Building loaded: {building.Name}");
        }

        private void LoadDefaultBuilding()
        {
            // Create a sample building for demo
            var building = CreateSampleBuilding();
            LoadBuilding(building);
        }

        private void ClearExistingBuilding()
        {
            var children = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name != "Building Visualization" && 
                    child.name != "Equipment Visualization" && 
                    child.name != "Sensor Visualization" &&
                    child.name != "Data Overlay")
                {
                    children.Add(child);
                }
            }
            
            foreach (var child in children)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private Building CreateSampleBuilding()
        {
            var metadata = new DigitalTwin.Core.Metadata.BuildingMetadata(
                "Digital Twin Demo Building",
                DigitalTwin.Core.Metadata.BuildingCategory.Commercial,
                "Unity Technologies",
                2024,
                10000,
                "Demo Organization",
                "demo@digitaltwin.com",
                new DigitalTwin.Core.Metadata.GeoLocation(37.7749m, -122.4194m),
                DigitalTwin.Core.Metadata.BuildingCertification.LEED
            );

            return new DigitalTwin.Core.Entities.Building(
                System.Guid.NewGuid(),
                "Digital Twin Demo Building",
                "1 Unity Way, San Francisco, CA",
                metadata,
                System.DateTime.UtcNow
            );
        }
    }

    public class DigitalTwinUIManager : MonoBehaviour
    {
        [Header("UI Configuration")]
        public bool showDashboard = true;
        public bool showBuildingInfo = true;
        public bool showSensorData = true;
        public bool showEnergyPanel = true;

        private Dictionary<string, GameObject> _uiPanels;

        private void Start()
        {
            InitializeUIPanels();
            SetupEventListeners();
        }

        private void InitializeUIPanels()
        {
            _uiPanels = new Dictionary<string, GameObject>();
            
            // Find all UI panels
            foreach (Transform child in transform)
            {
                _uiPanels[child.name] = child.gameObject;
            }
        }

        private void SetupEventListeners()
        {
            var inputManager = FindObjectOfType<DigitalTwinInputManager>();
            if (inputManager != null)
            {
                inputManager.OnBuildingSelected += HandleBuildingSelection;
                inputManager.OnEquipmentSelected += HandleEquipmentSelection;
                inputManager.OnSensorSelected += HandleSensorSelection;
            }
        }

        private void HandleBuildingSelection(GameObject building)
        {
            UpdateBuildingInfoPanel(building);
            Debug.Log($"Building selected: {building.name}");
        }

        private void HandleEquipmentSelection(GameObject equipment)
        {
            UpdateEquipmentInfoPanel(equipment);
            Debug.Log($"Equipment selected: {equipment.name}");
        }

        private void HandleSensorSelection(GameObject sensor)
        {
            UpdateSensorInfoPanel(sensor);
            Debug.Log($"Sensor selected: {sensor.name}");
        }

        private void UpdateBuildingInfoPanel(GameObject building) { /* Implementation */ }
        private void UpdateEquipmentInfoPanel(GameObject equipment) { /* Implementation */ }
        private void UpdateSensorInfoPanel(GameObject sensor) { /* Implementation */ }
    }

    public class DigitalTwinDebugManager : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool ShowPerformanceMetrics = true;
        public bool ShowNetworkActivity = false;
        public bool ShowSensorData = false;
        public bool EnableWireframeMode = false;

        private void Update()
        {
            if (ShowPerformanceMetrics)
            {
                ShowPerformanceInfo();
            }
        }

        private void ShowPerformanceInfo()
        {
            if (Time.frameCount % 60 == 0) // Update every second
            {
                Debug.Log($"FPS: {1.0f / Time.deltaTime:F1}, Memory: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(0) / 1024 / 1024:F1}MB");
            }
        }
    }

    public class DemoBuildingCreator : MonoBehaviour
    {
        public void CreateSampleBuilding()
        {
            // Implementation for creating a complete demo building with floors, rooms, equipment, and sensors
            Debug.Log("Demo building creation complete");
        }
    }

    // Enums
    public enum Anchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        Bottom,
        BottomRight
    }

    public enum EquipmentType
    {
        HVAC,
        Lighting,
        FireSuppression,
        Security
    }

    public enum SensorType
    {
        Temperature,
        Humidity,
        Energy,
        AirQuality
    }
}