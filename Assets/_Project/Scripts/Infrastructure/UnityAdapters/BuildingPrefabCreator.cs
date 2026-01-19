using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.ValueObjects;
using DigitalTwin.Core.Metadata;

namespace DigitalTwin.Infrastructure.UnityAdapters
{
    /// <summary>
    /// Building Prefab Creator
    /// 
    /// Architectural Intent:
    /// - Creates standardized Unity prefabs for digital twin visualization
    /// - Provides automated prefab generation based on domain models
    /// - Ensures consistent visual representation across the project
    /// - Supports customizable visual styles and materials
    /// 
    /// Key Design Decisions:
    /// 1. ScriptableObject-based configuration for visual settings
    /// 2. Automated mesh generation for buildings and floors
    /// 3. Component-based architecture for extensibility
    /// 4. Asset database management for prefabs
    /// </summary>
    public static class BuildingPrefabCreator
    {
        private const string PREFAB_PATH = "Assets/_Project/Prefabs/";
        private const string MATERIAL_PATH = "Assets/_Project/Materials/";
        private const string BUILDING_PREFAB_NAME = "BuildingPrefab";
        private const string FLOOR_PREFAB_NAME = "FloorPrefab";
        private const string ROOM_PREFAB_NAME = "RoomPrefab";
        private const string EQUIPMENT_PREFAB_NAME = "EquipmentPrefab";
        private const string SENSOR_PREFAB_NAME = "SensorPrefab";

        [MenuItem("Digital Twin/Create Building Prefabs")]
        public static void CreateAllBuildingPrefabs()
        {
            CreateDirectories();
            CreateMaterials();
            CreateBuildingPrefab();
            CreateFloorPrefab();
            CreateRoomPrefab();
            CreateEquipmentPrefabs();
            CreateSensorPrefabs();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("Building prefabs created successfully!");
        }

        private static void CreateDirectories()
        {
            if (!Directory.Exists(PREFAB_PATH))
                Directory.CreateDirectory(PREFAB_PATH);
            
            if (!Directory.Exists(MATERIAL_PATH))
                Directory.CreateDirectory(MATERIAL_PATH);
        }

        private static void CreateMaterials()
        {
            // Building materials
            CreateMaterial("BuildingMaterial", new Color(0.7f, 0.7f, 0.8f, 1.0f));
            CreateMaterial("FloorMaterial", new Color(0.9f, 0.9f, 0.85f, 1.0f));
            CreateMaterial("RoomMaterial", new Color(0.8f, 0.8f, 0.75f, 1.0f));
            CreateMaterial("EquipmentMaterial", new Color(0.4f, 0.4f, 0.5f, 1.0f));
            CreateMaterial("SensorMaterial", new Color(0.2f, 0.8f, 0.2f, 1.0f));
            CreateMaterial("GlassMaterial", new Color(0.8f, 0.9f, 1.0f, 0.7f));
            
            // Status materials
            CreateMaterial("StatusGood", new Color(0.2f, 0.8f, 0.2f, 1.0f));
            CreateMaterial("StatusWarning", new Color(1.0f, 0.8f, 0.2f, 1.0f));
            CreateMaterial("StatusError", new Color(0.8f, 0.2f, 0.2f, 1.0f));
        }

        private static void CreateMaterial(string name, Color color)
        {
            var path = $"{MATERIAL_PATH}{name}.mat";
            
            if (!File.Exists(path))
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                AssetDatabase.CreateAsset(material, path);
            }
        }

        private static void CreateBuildingPrefab()
        {
            var buildingGO = new GameObject("Building");
            
            // Add mesh filter and renderer for building shell
            var meshFilter = buildingGO.AddComponent<MeshFilter>();
            var meshRenderer = buildingGO.AddComponent<MeshRenderer>();
            meshRenderer.material = AssetDatabase.LoadAsset<Material>($"{MATERIAL_PATH}BuildingMaterial.mat");
            
            // Generate building mesh
            meshFilter.sharedMesh = GenerateBuildingMesh(50f, 30f, 20f); // Default 50x30x20m
            
            // Add collider
            var boxCollider = buildingGO.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(50f, 30f, 20f);
            
            // Add BuildingController
            var controller = buildingGO.AddComponent<BuildingController>();
            
            // Add BuildingView
            var view = buildingGO.AddComponent<BuildingView>();
            view.Initialize(CreateSampleBuilding());
            
            // Create prefab
            var prefabPath = $"{PREFAB_PATH}{BUILDING_PREFAB_NAME}.prefab";
            PrefabUtility.SaveAsPrefabAsset(buildingGO, prefabPath);
            
            Object.DestroyImmediate(buildingGO);
        }

        private static void CreateFloorPrefab()
        {
            var floorGO = new GameObject("Floor");
            
            // Add mesh for floor
            var meshFilter = floorGO.AddComponent<MeshFilter>();
            var meshRenderer = floorGO.AddComponent<MeshRenderer>();
            meshRenderer.material = AssetDatabase.LoadAsset<Material>($"{MATERIAL_PATH}FloorMaterial.mat");
            
            // Generate floor mesh
            meshFilter.sharedMesh = GenerateFloorMesh(50f, 20f, 0.1f); // 50x20x0.1m
            
            // Add FloorController
            var controller = floorGO.AddComponent<FloorController>();
            
            // Create prefab
            var prefabPath = $"{PREFAB_PATH}{FLOOR_PREFAB_NAME}.prefab";
            PrefabUtility.SaveAsPrefabAsset(floorGO, prefabPath);
            
            Object.DestroyImmediate(floorGO);
        }

        private static void CreateRoomPrefab()
        {
            var roomGO = new GameObject("Room");
            
            // Add mesh for room
            var meshFilter = roomGO.AddComponent<MeshFilter>();
            var meshRenderer = roomGO.AddComponent<MeshRenderer>();
            meshRenderer.material = AssetDatabase.LoadAsset<Material>($"{MATERIAL_PATH}RoomMaterial.mat");
            
            // Generate room mesh
            meshFilter.sharedMesh = GenerateRoomMesh(10f, 8f, 3f); // 10x8x3m
            
            // Add RoomController
            var controller = roomGO.AddComponent<RoomController>();
            
            // Create prefab
            var prefabPath = $"{PREFAB_PATH}{ROOM_PREFAB_NAME}.prefab";
            PrefabUtility.SaveAsPrefabAsset(roomGO, prefabPath);
            
            Object.DestroyImmediate(roomGO);
        }

        private static void CreateEquipmentPrefabs()
        {
            CreateEquipmentPrefab("HVAC_Unit", new Vector3(2f, 1f, 1f), EquipmentType.HVAC);
            CreateEquipmentPrefab("Lighting_System", new Vector3(0.5f, 0.2f, 0.5f), EquipmentType.Lighting);
            CreateEquipmentPrefab("Fire_Suppression", new Vector3(1f, 0.5f, 1f), EquipmentType.FireSuppression);
            CreateEquipmentPrefab("Security_Camera", new Vector3(0.3f, 0.3f, 0.3f), EquipmentType.Security);
        }

        private static void CreateEquipmentPrefab(string name, Vector3 size, EquipmentType type)
        {
            var equipmentGO = new GameObject(name);
            
            // Add mesh
            var meshFilter = equipmentGO.AddComponent<MeshFilter>();
            var meshRenderer = equipmentGO.AddComponent<MeshRenderer>();
            meshRenderer.material = AssetDatabase.LoadAsset<Material>($"{MATERIAL_PATH}EquipmentMaterial.mat");
            
            // Generate equipment mesh
            meshFilter.sharedMesh = GenerateBoxMesh(size);
            
            // Add EquipmentController
            var controller = equipmentGO.AddComponent<EquipmentController>();
            
            // Create prefab
            var prefabPath = $"{PREFAB_PATH}{EQUIPMENT_PREFAB_NAME}_{type}.prefab";
            PrefabUtility.SaveAsPrefabAsset(equipmentGO, prefabPath);
            
            Object.DestroyImmediate(equipmentGO);
        }

        private static void CreateSensorPrefabs()
        {
            CreateSensorPrefab("Temperature_Sensor", new Vector3(0.1f, 0.1f, 0.1f), SensorType.Temperature);
            CreateSensorPrefab("Humidity_Sensor", new Vector3(0.1f, 0.1f, 0.1f), SensorType.Humidity);
            CreateSensorPrefab("Energy_Meter", new Vector3(0.15f, 0.1f, 0.1f), SensorType.Energy);
            CreateSensorPrefab("Air_Quality_Sensor", new Vector3(0.12f, 0.12f, 0.05f), SensorType.AirQuality);
        }

        private static void CreateSensorPrefab(string name, Vector3 size, SensorType type)
        {
            var sensorGO = new GameObject(name);
            
            // Add mesh
            var meshFilter = sensorGO.AddComponent<MeshFilter>();
            var meshRenderer = sensorGO.AddComponent<MeshRenderer>();
            meshRenderer.material = AssetDatabase.LoadAsset<Material>($"{MATERIAL_PATH}SensorMaterial.mat");
            
            // Generate sensor mesh
            meshFilter.sharedMesh = GenerateSphereMesh(size.x);
            
            // Add sensor-specific components
            var controller = sensorGO.AddComponent<SensorController>();
            
            // Create prefab
            var prefabPath = $"{PREFAB_PATH}{SENSOR_PREFAB_NAME}_{type}.prefab";
            PrefabUtility.SaveAsPrefabAsset(sensorGO, prefabPath);
            
            Object.DestroyImmediate(sensorGO);
        }

        // Mesh Generation Methods
        private static Mesh GenerateBuildingMesh(float width, float length, float height)
        {
            var mesh = new Mesh();
            mesh.name = "BuildingMesh";
            
            // Vertices for a simple building
            var vertices = new Vector3[]
            {
                // Bottom face
                new Vector3(-width/2, 0, -length/2),
                new Vector3(width/2, 0, -length/2),
                new Vector3(width/2, 0, length/2),
                new Vector3(-width/2, 0, length/2),
                
                // Top face
                new Vector3(-width/2, height, -length/2),
                new Vector3(width/2, height, -length/2),
                new Vector3(width/2, height, length/2),
                new Vector3(-width/2, height, length/2)
            };
            
            var triangles = new int[]
            {
                // Bottom face
                0, 2, 1, 0, 3, 2,
                // Top face
                4, 5, 6, 4, 6, 7,
                // Front face
                0, 1, 5, 0, 5, 4,
                // Back face
                2, 3, 7, 2, 7, 6,
                // Left face
                3, 0, 4, 3, 4, 7,
                // Right face
                1, 2, 6, 1, 6, 5
            };
            
            var normals = new Vector3[]
            {
                Vector3.down, Vector3.down, Vector3.down, Vector3.down,
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                Vector3.left, Vector3.left, Vector3.left, Vector3.left,
                Vector3.right, Vector3.right, Vector3.right, Vector3.right
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            
            return mesh;
        }

        private static Mesh GenerateFloorMesh(float width, float length, float thickness)
        {
            var mesh = new Mesh();
            mesh.name = "FloorMesh";
            
            var vertices = new Vector3[]
            {
                new Vector3(-width/2, 0, -length/2),
                new Vector3(width/2, 0, -length/2),
                new Vector3(width/2, 0, length/2),
                new Vector3(-width/2, 0, length/2),
                
                new Vector3(-width/2, -thickness, -length/2),
                new Vector3(width/2, -thickness, -length/2),
                new Vector3(width/2, -thickness, length/2),
                new Vector3(-width/2, -thickness, length/2)
            };
            
            var triangles = new int[]
            {
                // Top face
                0, 2, 1, 0, 3, 2,
                // Bottom face
                5, 4, 6, 6, 4, 7,
                // Side faces
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }

        private static Mesh GenerateRoomMesh(float width, float length, float height)
        {
            return GenerateBuildingMesh(width, length, height);
        }

        private static Mesh GenerateBoxMesh(Vector3 size)
        {
            var mesh = new Mesh();
            mesh.name = "BoxMesh";
            
            var x = size.x / 2;
            var y = size.y / 2;
            var z = size.z / 2;
            
            var vertices = new Vector3[]
            {
                new Vector3(-x, -y, -z), new Vector3(x, -y, -z), new Vector3(x, y, -z), new Vector3(-x, y, -z),
                new Vector3(-x, -y, z), new Vector3(x, -y, z), new Vector3(x, y, z), new Vector3(-x, y, z)
            };
            
            var triangles = new int[]
            {
                0, 2, 1, 0, 3, 2, // Front
                4, 5, 6, 4, 6, 7, // Back
                0, 4, 7, 0, 7, 3, // Left
                1, 2, 6, 1, 6, 5, // Right
                3, 7, 6, 3, 6, 2, // Top
                0, 1, 5, 0, 5, 4  // Bottom
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }

        private static Mesh GenerateSphereMesh(float radius)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(gameObject);
            return mesh;
        }

        private static Building CreateSampleBuilding()
        {
            var metadata = new BuildingMetadata(
                "Sample Building",
                BuildingCategory.Commercial,
                "Digital Twin Architect",
                DateTime.UtcNow.Year,
                5000,
                "Building Owner",
                "owner@building.com",
                new GeoLocation(40.7128m, -74.0060m),
                BuildingCertification.LEED
            );

            return new Building(
                Guid.NewGuid(),
                "Sample Building",
                "123 Main St",
                metadata,
                DateTime.UtcNow
            );
        }
    }
}