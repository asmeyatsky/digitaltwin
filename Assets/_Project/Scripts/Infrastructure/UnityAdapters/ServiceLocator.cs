using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalTwin.Infrastructure.UnityAdapters
{
    /// <summary>
    /// Service Locator for Unity Dependency Injection
    /// 
    /// Architectural Intent:
    /// - Provides dependency injection for Unity components
    /// - Maintains service registry throughout application lifecycle
    /// - Enables loose coupling between Unity components and domain services
    /// - Supports both interface and concrete service registration
    /// 
    /// Key Design Decisions:
    /// 1. Singleton pattern for global access
    /// 2. Generic methods for type-safe service registration
    /// 3. Service lifetime management (singleton vs transient)
    /// 4. Validation of service dependencies
    /// </summary>
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator _instance;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        private readonly HashSet<Type> _singletons = new HashSet<Type>();

        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ServiceLocator>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ServiceLocator");
                        _instance = go.AddComponent<ServiceLocator>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Register core services
            RegisterCoreServices();
        }

        /// <summary>
        /// Registers a singleton service instance
        /// </summary>
        public void RegisterSingleton<TInterface, TImplementation>(TImplementation instance)
            where TImplementation : class, TInterface
        {
            var interfaceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);

            if (_services.ContainsKey(interfaceType))
            {
                Debug.LogWarning($"Service {interfaceType.Name} is already registered");
                return;
            }

            _services[interfaceType] = instance;
            _singletons.Add(interfaceType);
            
            Debug.Log($"Registered singleton service: {interfaceType.Name} -> {implementationType.Name}");
        }

        /// <summary>
        /// Registers a singleton service with factory
        /// </summary>
        public void RegisterSingleton<TInterface>(Func<TInterface> factory) where TInterface : class
        {
            var interfaceType = typeof(TInterface);

            if (_services.ContainsKey(interfaceType))
            {
                Debug.LogWarning($"Service {interfaceType.Name} is already registered");
                return;
            }

            _factories[interfaceType] = () => factory();
            _singletons.Add(interfaceType);
            
            Debug.Log($"Registered singleton factory for: {interfaceType.Name}");
        }

        /// <summary>
        /// Registers a transient service with factory
        /// </summary>
        public void RegisterTransient<TInterface>(Func<TInterface> factory) where TInterface : class
        {
            var interfaceType = typeof(TInterface);

            if (_services.ContainsKey(interfaceType))
            {
                Debug.LogWarning($"Service {interfaceType.Name} is already registered");
                return;
            }

            _factories[interfaceType] = () => factory();
            
            Debug.Log($"Registered transient factory for: {interfaceType.Name}");
        }

        /// <summary>
        /// Registers a singleton service instance directly
        /// </summary>
        public void RegisterService<TInterface>(TInterface instance) where TInterface : class
        {
            RegisterSingleton<TInterface, TInterface>(instance);
        }

        /// <summary>
        /// Gets a registered service
        /// </summary>
        public TInterface GetService<TInterface>() where TInterface : class
        {
            var interfaceType = typeof(TInterface);

            // Return existing instance if available
            if (_services.TryGetValue(interfaceType, out var service))
            {
                return service as TInterface;
            }

            // Create new instance if factory is registered
            if (_factories.TryGetValue(interfaceType, out var factory))
            {
                var newInstance = factory();
                
                // Store as singleton if required
                if (_singletons.Contains(interfaceType))
                {
                    _services[interfaceType] = newInstance;
                }

                return newInstance as TInterface;
            }

            Debug.LogError($"Service {interfaceType.Name} is not registered");
            return null;
        }

        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        public bool IsRegistered<TInterface>() where TInterface : class
        {
            var interfaceType = typeof(TInterface);
            return _services.ContainsKey(interfaceType) || _factories.ContainsKey(interfaceType);
        }

        /// <summary>
        /// Unregisters a service
        /// </summary>
        public void Unregister<TInterface>() where TInterface : class
        {
            var interfaceType = typeof(TInterface);

            _services.Remove(interfaceType);
            _factories.Remove(interfaceType);
            _singletons.Remove(interfaceType);
            
            Debug.Log($"Unregistered service: {interfaceType.Name}");
        }

        /// <summary>
        /// Clears all registered services
        /// </summary>
        public void ClearAllServices()
        {
            _services.Clear();
            _factories.Clear();
            _singletons.Clear();
            
            Debug.Log("Cleared all registered services");
        }

        /// <summary>
        /// Gets all registered service types
        /// </summary>
        public Type[] GetRegisteredServices()
        {
            var services = new HashSet<Type>();
            
            foreach (var type in _services.Keys)
                services.Add(type);
            
            foreach (var type in _factories.Keys)
                services.Add(type);

            return services.ToArray();
        }

        /// <summary>
        /// Validates service dependencies
        /// </summary>
        public bool ValidateDependencies()
        {
            var isValid = true;

            foreach (var serviceType in GetRegisteredServices())
            {
                var service = GetService<object>();
                if (service == null)
                {
                    Debug.LogError($"Failed to resolve service: {serviceType.Name}");
                    isValid = false;
                }
            }

            return isValid;
        }

        private void RegisterCoreServices()
        {
            // This would register the actual domain services
            // For now, we'll register mock services
            
            Debug.Log("Registering core services...");
            
            // Register mock data collection service
            RegisterSingleton<IDataCollectionService, MockDataCollectionService>(
                new MockDataCollectionService());
            
            // Register mock analytics service
            RegisterSingleton<IDataAnalyticsService, MockAnalyticsService>(
                new MockAnalyticsService());
            
            // Register mock building simulation service
            RegisterSingleton<IBuildingSimulationService, MockBuildingSimulationService>(
                new MockBuildingSimulationService());
            
            Debug.Log("Core services registered successfully");
        }
    }

    // Mock service implementations for testing
    public class MockDataCollectionService : IDataCollectionService
    {
        public event Action<DigitalTwin.Core.ValueObjects.SensorReading> SensorDataReceived;
        public event Action<Guid, DigitalTwin.Core.Entities.SensorStatus> SensorStatusChanged;

        public async Task<DigitalTwin.Core.ValueObjects.SensorReading> CollectSensorDataAsync(Guid sensorId)
        {
            await Task.Delay(10);
            return new DigitalTwin.Core.ValueObjects.SensorReading(
                sensorId, 
                DigitalTwin.Core.Entities.SensorType.Temperature,
                22.5m, 
                DateTime.UtcNow, 
                95m);
        }

        public async Task<IEnumerable<DigitalTwin.Core.ValueObjects.SensorReading>> CollectMultipleSensorDataAsync(IEnumerable<Guid> sensorIds)
        {
            var readings = new List<DigitalTwin.Core.ValueObjects.SensorReading>();
            foreach (var id in sensorIds)
            {
                readings.Add(await CollectSensorDataAsync(id));
            }
            return readings;
        }

        public async Task<DigitalTwin.Core.ValueObjects.OperationalMetrics> CollectEquipmentMetricsAsync(Guid equipmentId)
        {
            await Task.Delay(10);
            return DigitalTwin.Core.ValueObjects.OperationalMetrics.Default;
        }

        public async Task<DigitalTwin.Core.ValueObjects.EnvironmentalConditions> CollectRoomConditionsAsync(Guid roomId)
        {
            await Task.Delay(10);
            return DigitalTwin.Core.ValueObjects.EnvironmentalConditions.Default;
        }

        public async Task StartDataStreamAsync(IEnumerable<Guid> sensorIds, Action<DigitalTwin.Core.ValueObjects.SensorReading> onDataReceived)
        {
            // Mock implementation
            await Task.CompletedTask;
        }

        public async Task StopDataStreamAsync(IEnumerable<Guid> sensorIds)
        {
            await Task.CompletedTask;
        }

        public async Task<DigitalTwin.Core.Interfaces.DataQualityReport> ValidateDataQualityAsync(Guid sensorId, TimeSpan timeWindow)
        {
            await Task.Delay(10);
            return new DigitalTwin.Core.Interfaces.DataQualityReport(sensorId, 95m, 100, 95, 5, new List<string>());
        }

        public async Task<IEnumerable<DigitalTwin.Core.ValueObjects.SensorReading>> GetHistoricalDataAsync(Guid sensorId, DateTime startTime, DateTime endTime)
        {
            await Task.Delay(10);
            return new List<DigitalTwin.Core.ValueObjects.SensorReading>();
        }

        public async Task<bool> IsSensorOnlineAsync(Guid sensorId)
        {
            await Task.Delay(10);
            return true;
        }
    }

    public class MockAnalyticsService : IDataAnalyticsService
    {
        public event Action<DigitalTwin.Core.Interfaces.AnalyticsResult> AnalyticsCompleted;
        public event Action<DigitalTwin.Core.Interfaces.AnomalyAlert> AnomalyDetected;

        public async Task<DigitalTwin.Core.Interfaces.EnergyAnalysisResult> AnalyzeEnergyConsumptionAsync(Guid buildingId, DateTime startTime, DateTime endTime, DigitalTwin.Core.Interfaces.AnalysisParameters parameters)
        {
            await Task.Delay(100);
            return new DigitalTwin.Core.Interfaces.EnergyAnalysisResult(
                buildingId, startTime, endTime, 1000m, 50m, 80m, 5m,
                new List<DigitalTwin.Core.Interfaces.ConsumptionAnomaly>(),
                new List<DigitalTwin.Core.Interfaces.EfficiencyRecommendation>());
        }

        public async Task<DigitalTwin.Core.Interfaces.EnvironmentalAnalysisResult> AnalyzeEnvironmentalConditionsAsync(Guid roomId, DateTime startTime, DateTime endTime, DigitalTwin.Core.Interfaces.AnalysisParameters parameters)
        {
            await Task.Delay(100);
            return new DigitalTwin.Core.Interfaces.EnvironmentalAnalysisResult(
                roomId, startTime, endTime,
                DigitalTwin.Core.ValueObjects.EnvironmentalConditions.Default,
                DigitalTwin.Core.ValueObjects.EnvironmentalConditions.Default,
                DigitalTwin.Core.ValueObjects.ComfortLevel.Good,
                TimeSpan.FromHours(8),
                new Dictionary<string, DigitalTwin.Core.ValueObjects.EnvironmentalConditions>());
        }

        public async Task<DigitalTwin.Core.Interfaces.EquipmentAnalysisResult> AnalyzeEquipmentPerformanceAsync(Guid equipmentId, DateTime startTime, DateTime endTime, DigitalTwin.Core.Interfaces.AnalysisParameters parameters)
        {
            await Task.Delay(100);
            return new DigitalTwin.Core.Interfaces.EquipmentAnalysisResult(
                equipmentId, startTime, endTime,
                DigitalTwin.Core.ValueObjects.OperationalMetrics.Default,
                DigitalTwin.Core.ValueObjects.PerformanceRating.Good,
                TimeSpan.FromHours(23), 0, new List<DigitalTwin.Core.Interfaces.MaintenanceRecommendation>());
        }

        public async Task<DigitalTwin.Core.Interfaces.AnomalyDetectionResult> DetectAnomaliesAsync(Guid entityId, DigitalTwin.Core.Interfaces.EntityType entityType, DateTime startTime, DateTime endTime, DigitalTwin.Core.Interfaces.AnomalyDetectionParameters parameters)
        {
            await Task.Delay(100);
            return new DigitalTwin.Core.Interfaces.AnomalyDetectionResult(
                entityId, entityType, startTime, endTime, new List<DigitalTwin.Core.Interfaces.Anomaly>());
        }

        public async Task<DigitalTwin.Core.Interfaces.KPIReport> CalculateKPIsAsync(Guid buildingId, DateTime startTime, DateTime endTime, DigitalTwin.Core.Interfaces.KPIParameters parameters)
        {
            await Task.Delay(100);
            return new DigitalTwin.Core.Interfaces.KPIReport(
                buildingId, startTime, endTime,
                new Dictionary<string, decimal>(), 85m,
                new Dictionary<string, decimal>());
        }

        public async Task<DigitalTwin.Core.Interfaces.InsightsReport> GenerateInsightsAsync(Guid buildingId, DigitalTwin.Core.Interfaces.InsightParameters parameters)
        {
            await Task.Delay(100);
            return new DigitalTwin.Core.Interfaces.InsightsReport(buildingId, DateTime.UtcNow, new List<DigitalTwin.Core.Interfaces.Insight>());
        }

        public async Task<DigitalTwin.Core.Interfaces.BenchmarkingResult> BenchmarkPerformanceAsync(Guid buildingId, DigitalTwin.Core.Interfaces.BenchmarkParameters parameters)
        {
            await Task.Delay(100);
            return new DigitalTwin.Core.Interfaces.BenchmarkingResult(
                buildingId, new Dictionary<string, decimal>(),
                new Dictionary<string, decimal>(),
                new Dictionary<string, decimal>());
        }

        public async Task<DigitalTwin.Core.Interfaces.CorrelationAnalysisResult> PerformCorrelationAnalysisAsync(IEnumerable<Guid> entityIds, DateTime startTime, DateTime endTime, DigitalTwin.Core.Interfaces.CorrelationParameters parameters)
        {
            await Task.Delay(100);
            return new DigitalTwin.Core.Interfaces.CorrelationAnalysisResult(
                new Dictionary<string, decimal>());
        }

        public async Task StartRealTimeAnalyticsAsync(Guid buildingId, DigitalTwin.Core.Interfaces.AnalyticsParameters parameters)
        {
            await Task.CompletedTask;
        }

        public async Task StopRealTimeAnalyticsAsync(Guid buildingId)
        {
            await Task.CompletedTask;
        }
    }

    public class MockBuildingSimulationService : IBuildingSimulationService
    {
        public event Action<DigitalTwin.Core.Interfaces.SimulationResult> SimulationCompleted;
        public event Action<Guid, DigitalTwin.Core.Interfaces.SimulationState> SimulationStateChanged;

        public async Task<DigitalTwin.Core.Interfaces.EnergySimulationResult> SimulateEnergyConsumptionAsync(DigitalTwin.Core.Entities.Building building, TimeSpan simulationPeriod, DigitalTwin.Core.Interfaces.SimulationParameters parameters)
        {
            await Task.Delay(200);
            return new DigitalTwin.Core.Interfaces.EnergySimulationResult(
                building.Id, simulationPeriod,
                DigitalTwin.Core.ValueObjects.EnergyConsumption.FromKilowattHours(1200, simulationPeriod),
                DigitalTwin.Core.ValueObjects.EnergyConsumption.FromKilowattHours(50, simulationPeriod),
                DigitalTwin.Core.ValueObjects.EnergyConsumption.FromKilowattHours(100, simulationPeriod),
                144m, 276m, new Dictionary<string, DigitalTwin.Core.ValueObjects.EnergyConsumption>(),
                new Dictionary<string, DigitalTwin.Core.ValueObjects.EnergyConsumption>());
        }

        public async Task<DigitalTwin.Core.Interfaces.EnvironmentalSimulationResult> SimulateRoomConditionsAsync(DigitalTwin.Core.Entities.Room room, TimeSpan simulationPeriod, DigitalTwin.Core.Interfaces.EnvironmentalParameters parameters)
        {
            await Task.Delay(200);
            return new DigitalTwin.Core.Interfaces.EnvironmentalSimulationResult(
                room.Id, simulationPeriod,
                DigitalTwin.Core.ValueObjects.EnvironmentalConditions.Default,
                DigitalTwin.Core.ValueObjects.EnvironmentalConditions.Default,
                DigitalTwin.Core.ValueObjects.ComfortLevel.Good,
                TimeSpan.FromHours(8),
                new Dictionary<string, DigitalTwin.Core.ValueObjects.EnvironmentalConditions>());
        }

        public async Task<DigitalTwin.Core.Interfaces.EquipmentSimulationResult> SimulateEquipmentAsync(DigitalTwin.Core.Entities.Equipment equipment, TimeSpan simulationPeriod, DigitalTwin.Core.Interfaces.EquipmentParameters parameters)
        {
            await Task.Delay(200);
            return new DigitalTwin.Core.Interfaces.EquipmentSimulationResult(
                equipment.Id, simulationPeriod,
                DigitalTwin.Core.ValueObjects.OperationalMetrics.Default,
                DigitalTwin.Core.ValueObjects.PerformanceRating.Good,
                TimeSpan.FromHours(23), 0, new List<DigitalTwin.Core.Interfaces.MaintenanceRecommendation>());
        }

        public async Task<DigitalTwin.Core.Interfaces.OccupancySimulationResult> SimulateOccupancyAsync(DigitalTwin.Core.Entities.Building building, TimeSpan simulationPeriod, DigitalTwin.Core.Interfaces.OccupancyParameters parameters)
        {
            await Task.Delay(200);
            return new DigitalTwin.Core.Interfaces.OccupancySimulationResult(
                building.Id, simulationPeriod, 75, 120,
                new DigitalTwin.Application.Services.OccupancyPattern(),
                new Dictionary<string, decimal>());
        }

        public async Task<DigitalTwin.Core.Interfaces.ScenarioAnalysisResult> RunScenarioAnalysisAsync(Guid buildingId, DigitalTwin.Core.Interfaces.Scenario scenario)
        {
            await Task.Delay(200);
            return new DigitalTwin.Core.Interfaces.ScenarioAnalysisResult(
                buildingId, scenario, new List<DigitalTwin.Core.Interfaces.SimulationResult>());
        }

        public async Task<DigitalTwin.Core.Interfaces.EnergyPredictionResult> PredictEnergyConsumptionAsync(Guid buildingId, TimeSpan predictionPeriod, DigitalTwin.Core.Interfaces.PredictionParameters parameters)
        {
            await Task.Delay(200);
            return new DigitalTwin.Application.Services.EnergyPredictionResult(
                buildingId, predictionPeriod, new List<DigitalTwin.Application.Services.EnergyPrediction>(),
                DigitalTwin.Application.Services.PredictionAccuracy.High, "Prediction completed");
        }

        public async Task<DigitalTwin.Core.Interfaces.OptimizationResult> OptimizeBuildingOperationsAsync(Guid buildingId, DigitalTwin.Core.Interfaces.OptimizationParameters parameters)
        {
            await Task.Duration(200);
            return new DigitalTwin.Core.Interfaces.OptimizationResult(
                buildingId, new List<DigitalTwin.Core.Interfaces.OptimizationRecommendation>(),
                15m);
        }

        public async Task StartRealTimeSimulationAsync(Guid buildingId, DigitalTwin.Core.Interfaces.SimulationParameters parameters)
        {
            await Task.CompletedTask;
        }

        public async Task StopRealTimeSimulationAsync(Guid buildingId)
        {
            await Task.CompletedTask;
        }
    }
}