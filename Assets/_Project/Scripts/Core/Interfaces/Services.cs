using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Data Collection Service Interface
    /// 
    /// Architectural Intent:
    /// - Defines contract for collecting sensor and equipment data
    /// - Provides abstraction for various data sources (IoT, BMS, SCADA)
    /// - Enables real-time data streaming and historical data access
    /// - Supports data validation and quality assurance
    /// 
    /// Key Design Decisions:
    /// 1. Async operations to support real-time data collection
    /// 2. Generic methods for different sensor types
    /// 3. Batch operations for performance optimization
    /// 4. Event-driven architecture for real-time updates
    /// </summary>
    public interface IDataCollectionService
    {
        /// <summary>
        /// Collects real-time data from a specific sensor
        /// </summary>
        Task<SensorReading> CollectSensorDataAsync(Guid sensorId);

        /// <summary>
        /// Collects data from multiple sensors in parallel
        /// </summary>
        Task<IEnumerable<SensorReading>> CollectMultipleSensorDataAsync(IEnumerable<Guid> sensorIds);

        /// <summary>
        /// Collects operational metrics from equipment
        /// </summary>
        Task<OperationalMetrics> CollectEquipmentMetricsAsync(Guid equipmentId);

        /// <summary>
        /// Collects environmental conditions for a room
        /// </summary>
        Task<EnvironmentalConditions> CollectRoomConditionsAsync(Guid roomId);

        /// <summary>
        /// Starts real-time data streaming for specified sensors
        /// </summary>
        Task StartDataStreamAsync(IEnumerable<Guid> sensorIds, Action<SensorReading> onDataReceived);

        /// <summary>
        /// Stops data streaming for specified sensors
        /// </summary>
        Task StopDataStreamAsync(IEnumerable<Guid> sensorIds);

        /// <summary>
        /// Validates sensor data quality and accuracy
        /// </summary>
        Task<DataQualityReport> ValidateDataQualityAsync(Guid sensorId, TimeSpan timeWindow);

        /// <summary>
        /// Gets historical sensor data for analysis
        /// </summary>
        Task<IEnumerable<SensorReading>> GetHistoricalDataAsync(Guid sensorId, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Checks if a sensor is online and responsive
        /// </summary>
        Task<bool> IsSensorOnlineAsync(Guid sensorId);

        /// <summary>
        /// Event fired when new sensor data is received
        /// </summary>
        event Action<SensorReading> SensorDataReceived;

        /// <summary>
        /// Event fired when sensor status changes
        /// </summary>
        event Action<Guid, SensorStatus> SensorStatusChanged;
    }

    /// <summary>
    /// Simulation Service Interface
    /// 
    /// Architectural Intent:
    /// - Defines contract for building behavior simulation and prediction
    /// - Provides what-if analysis and scenario modeling
    /// - Enables energy consumption forecasting and optimization
    /// - Supports environmental condition simulation
    /// 
    /// Key Design Decisions:
    /// 1. Time-based simulation with configurable parameters
    /// 2. Multiple simulation scenarios support
    /// 3. Real-time and batch simulation modes
    /// 4. Integration with machine learning models
    /// </summary>
    public interface ISimulationService
    {
        /// <summary>
        /// Simulates building energy consumption over time period
        /// </summary>
        Task<EnergySimulationResult> SimulateEnergyConsumptionAsync(Guid buildingId, TimeSpan simulationPeriod, SimulationParameters parameters);

        /// <summary>
        /// Simulates environmental conditions in a room
        /// </summary>
        Task<EnvironmentalSimulationResult> SimulateRoomConditionsAsync(Guid roomId, TimeSpan simulationPeriod, EnvironmentalParameters parameters);

        /// <summary>
        /// Simulates equipment behavior and performance
        /// </summary>
        Task<EquipmentSimulationResult> SimulateEquipmentAsync(Guid equipmentId, TimeSpan simulationPeriod, EquipmentParameters parameters);

        /// <summary>
        /// Runs what-if scenario analysis
        /// </summary>
        Task<ScenarioAnalysisResult> RunScenarioAnalysisAsync(Guid buildingId, Scenario scenario);

        /// <summary>
        /// Predicts future energy consumption based on historical data
        /// </summary>
        Task<EnergyPredictionResult> PredictEnergyConsumptionAsync(Guid buildingId, TimeSpan predictionPeriod, PredictionParameters parameters);

        /// <summary>
        /// Simulates occupancy patterns and space utilization
        /// </summary>
        Task<OccupancySimulationResult> SimulateOccupancyAsync(Guid buildingId, TimeSpan simulationPeriod, OccupancyParameters parameters);

        /// <summary>
        /// Optimizes building operations for energy efficiency
        /// </summary>
        Task<OptimizationResult> OptimizeBuildingOperationsAsync(Guid buildingId, OptimizationParameters parameters);

        /// <summary>
        /// Runs real-time simulation with live data integration
        /// </summary>
        Task StartRealTimeSimulationAsync(Guid buildingId, SimulationParameters parameters);

        /// <summary>
        /// Stops real-time simulation
        /// </summary>
        Task StopRealTimeSimulationAsync(Guid buildingId);

        /// <summary>
        /// Event fired when simulation results are available
        /// </summary>
        event Action<SimulationResult> SimulationCompleted;

        /// <summary>
        /// Event fired when simulation state changes
        /// </summary>
        event Action<Guid, SimulationState> SimulationStateChanged;
    }

    /// <summary>
    /// Analytics Service Interface
    /// 
    /// Architectural Intent:
    /// - Defines contract for data analysis and insights generation
    /// - Provides statistical analysis and trend identification
    /// - Enables anomaly detection and alert generation
    /// - Supports performance metrics and KPI calculation
    /// 
    /// Key Design Decisions:
    /// 1. Configurable analysis time windows
    /// 2. Multiple analysis types (statistical, predictive, prescriptive)
    /// 3. Real-time and batch analysis modes
    /// 4. Integration with external analytics platforms
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Analyzes energy consumption patterns and trends
        /// </summary>
        Task<EnergyAnalysisResult> AnalyzeEnergyConsumptionAsync(Guid buildingId, DateTime startTime, DateTime endTime, AnalysisParameters parameters);

        /// <summary>
        /// Analyzes environmental conditions and comfort levels
        /// </summary>
        Task<EnvironmentalAnalysisResult> AnalyzeEnvironmentalConditionsAsync(Guid roomId, DateTime startTime, DateTime endTime, AnalysisParameters parameters);

        /// <summary>
        /// Analyzes equipment performance and reliability
        /// </summary>
        Task<EquipmentAnalysisResult> AnalyzeEquipmentPerformanceAsync(Guid equipmentId, DateTime startTime, DateTime endTime, AnalysisParameters parameters);

        /// <summary>
        /// Detects anomalies in sensor data or equipment behavior
        /// </summary>
        Task<AnomalyDetectionResult> DetectAnomaliesAsync(Guid entityId, EntityType entityType, DateTime startTime, DateTime endTime, AnomalyDetectionParameters parameters);

        /// <summary>
        /// Calculates building performance KPIs
        /// </summary>
        Task<KPIReport> CalculateKPIsAsync(Guid buildingId, DateTime startTime, DateTime endTime, KPIParameters parameters);

        /// <summary>
        /// Generates insights and recommendations
        /// </summary>
        Task<InsightsReport> GenerateInsightsAsync(Guid buildingId, InsightParameters parameters);

        /// <summary>
        /// Compares performance against benchmarks
        /// </summary>
        Task<BenchmarkingResult> BenchmarkPerformanceAsync(Guid buildingId, BenchmarkParameters parameters);

        /// <summary>
        /// Performs correlation analysis between different metrics
        /// </summary>
        Task<CorrelationAnalysisResult> PerformCorrelationAnalysisAsync(IEnumerable<Guid> entityIds, DateTime startTime, DateTime endTime, CorrelationParameters parameters);

        /// <summary>
        /// Starts real-time analytics monitoring
        /// </summary>
        Task StartRealTimeAnalyticsAsync(Guid buildingId, AnalyticsParameters parameters);

        /// <summary>
        /// Stops real-time analytics monitoring
        /// </summary>
        Task StopRealTimeAnalyticsAsync(Guid buildingId);

        /// <summary>
        /// Event fired when analytics results are available
        /// </summary>
        event Action<AnalyticsResult> AnalyticsCompleted;

        /// <summary>
        /// Event fired when anomalies are detected
        /// </summary>
        event Action<AnomalyAlert> AnomalyDetected;
    }

    /// <summary>
    /// Persistence Service Interface
    /// 
    /// Architectural Intent:
    /// - Defines contract for data storage and retrieval operations
    /// - Provides abstraction for different storage systems
    /// - Enables data backup, restore, and archiving
    /// - Supports transaction management and data consistency
    /// 
    /// Key Design Decisions:
    /// 1. Repository pattern for data access
    /// 2. Unit of work pattern for transaction management
    /// 3. Async operations for performance
    /// 4. Caching support for frequently accessed data
    /// </summary>
    public interface IPersistenceService
    {
        /// <summary>
        /// Saves building entity and all related entities
        /// </summary>
        Task SaveBuildingAsync(Building building);

        /// <summary>
        /// Retrieves building by ID with all related entities
        /// </summary>
        Task<Building> GetBuildingAsync(Guid buildingId);

        /// <summary>
        /// Saves sensor reading data
        /// </summary>
        Task SaveSensorReadingAsync(SensorReading reading);

        /// <summary>
        /// Saves multiple sensor readings in batch
        /// </summary>
        Task SaveSensorReadingsBatchAsync(IEnumerable<SensorReading> readings);

        /// <summary>
        /// Retrieves sensor readings for time period
        /// </summary>
        Task<IEnumerable<SensorReading>> GetSensorReadingsAsync(Guid sensorId, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Saves equipment metrics
        /// </summary>
        Task SaveEquipmentMetricsAsync(Guid equipmentId, OperationalMetrics metrics);

        /// <summary>
        /// Retrieves equipment metrics history
        /// </summary>
        Task<IEnumerable<OperationalMetrics>> GetEquipmentMetricsAsync(Guid equipmentId, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Saves simulation results
        /// </summary>
        Task SaveSimulationResultAsync(SimulationResult result);

        /// <summary>
        /// Retrieves simulation results
        /// </summary>
        Task<IEnumerable<SimulationResult>> GetSimulationResultsAsync(Guid buildingId, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Archives historical data
        /// </summary>
        Task ArchiveDataAsync(DateTime cutoffDate);

        /// <summary>
        /// Restores archived data
        /// </summary>
        Task RestoreDataAsync(DateTime restoreDate);

        /// <summary>
        /// Creates data backup
        /// </summary>
        Task<BackupResult> CreateBackupAsync(BackupParameters parameters);

        /// <summary>
        /// Restores data from backup
        /// </summary>
        Task RestoreBackupAsync(string backupId);

        /// <summary>
        /// Gets data storage statistics
        /// </summary>
        Task<StorageStatistics> GetStorageStatisticsAsync();

        /// <summary>
        /// Starts database transaction
        /// </summary>
        Task<IDbTransaction> BeginTransactionAsync();

        /// <summary>
        /// Event fired when data is saved
        /// </summary>
        event Action<string, DateTime> DataSaved;

        /// <summary>
        /// Event fired when data is restored
        /// </summary>
        event Action<string, DateTime> DataRestored;
    }

    /// <summary>
    /// Notification Service Interface
    /// 
    /// Architectural Intent:
    /// - Defines contract for sending notifications and alerts
    /// - Provides multi-channel notification support
    /// - Enables notification routing and escalation
    /// - Supports notification templates and personalization
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends notification to specified recipients
        /// </summary>
        Task SendNotificationAsync(NotificationMessage message);

        /// <summary>
        /// Sends alert with high priority
        /// </summary>
        Task SendAlertAsync(AlertMessage alert);

        /// <summary>
        /// Sends daily report
        /// </summary>
        Task SendDailyReportAsync(Guid buildingId, DateTime reportDate);

        /// <summary>
        /// Sends weekly summary
        /// </summary>
        Task SendWeeklySummaryAsync(Guid buildingId, DateTime weekStart);

        /// <summary>
        /// Subscribes user to notifications
        /// </summary>
        Task SubscribeUserAsync(string userId, NotificationSubscription subscription);

        /// <summary>
        /// Unsubscribes user from notifications
        /// </summary>
        Task UnsubscribeUserAsync(string userId, string subscriptionId);

        /// <summary>
        /// Gets notification history
        /// </summary>
        Task<IEnumerable<NotificationHistory>> GetNotificationHistoryAsync(string userId, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Event fired when notification is sent
        /// </summary>
        event Action<string, NotificationStatus> NotificationSent;
    }
}