using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Result Types for Service Operations
    /// 
    /// Architectural Intent:
    /// - Provides standardized result objects for service operations
    /// - Encapsulates success/failure status and error information
    /// - Enables consistent error handling and response formatting
    /// - Supports detailed operation metadata and diagnostics
    /// </summary>

    // Base Result Types
    public abstract class OperationResult
    {
        public bool IsSuccess { get; protected set; }
        public string Message { get; protected set; }
        public DateTime Timestamp { get; protected set; }
        public Dictionary<string, object> Metadata { get; protected set; }

        protected OperationResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
            Timestamp = DateTime.UtcNow;
            Metadata = new Dictionary<string, object>();
        }
    }

    public class SuccessResult : OperationResult
    {
        public SuccessResult(string message = "Operation completed successfully") 
            : base(true, message) { }
    }

    public class ErrorResult : OperationResult
    {
        public Exception Exception { get; }
        public string ErrorCode { get; }

        public ErrorResult(string message, string errorCode = null, Exception exception = null) 
            : base(false, message)
        {
            ErrorCode = errorCode;
            Exception = exception;
        }
    }

    // Data Collection Results
    public class DataQualityReport : OperationResult
    {
        public Guid SensorId { get; }
        public decimal AccuracyScore { get; }
        public int TotalReadings { get; }
        public int ValidReadings { get; }
        public int InvalidReadings { get; }
        public List<string> QualityIssues { get; }

        public DataQualityReport(Guid sensorId, decimal accuracyScore, int totalReadings, int validReadings, 
                                int invalidReadings, List<string> qualityIssues) 
            : base(true, "Data quality analysis completed")
        {
            SensorId = sensorId;
            AccuracyScore = accuracyScore;
            TotalReadings = totalReadings;
            ValidReadings = validReadings;
            InvalidReadings = invalidReadings;
            QualityIssues = qualityIssues ?? new List<string>();
        }
    }

    // Simulation Results
    public abstract class SimulationResult : OperationResult
    {
        public Guid BuildingId { get; }
        public Guid SimulationId { get; }
        public SimulationType Type { get; }
        public TimeSpan SimulationPeriod { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }

        protected SimulationResult(Guid buildingId, SimulationType type, TimeSpan period, bool isSuccess, string message) 
            : base(isSuccess, message)
        {
            BuildingId = buildingId;
            SimulationId = Guid.NewGuid();
            Type = type;
            SimulationPeriod = period;
            StartTime = DateTime.UtcNow;
            EndTime = StartTime + period;
        }
    }

    public class EnergySimulationResult : SimulationResult
    {
        public Core.ValueObjects.EnergyConsumption TotalConsumption { get; }
        public Core.ValueObjects.EnergyConsumption AverageConsumption { get; }
        public Core.ValueObjects.EnergyConsumption PeakConsumption { get; }
        public decimal TotalCost { get; }
        public decimal CarbonFootprint { get; }
        public Dictionary<string, Core.ValueObjects.EnergyConsumption> ConsumptionByFloor { get; }
        public Dictionary<string, Core.ValueObjects.EnergyConsumption> ConsumptionByEquipmentType { get; }

        public EnergySimulationResult(Guid buildingId, TimeSpan period, 
                                    Core.ValueObjects.EnergyConsumption total, 
                                    Core.ValueObjects.EnergyConsumption average, 
                                    Core.ValueObjects.EnergyConsumption peak,
                                    decimal cost, decimal carbon,
                                    Dictionary<string, Core.ValueObjects.EnergyConsumption> byFloor,
                                    Dictionary<string, Core.ValueObjects.EnergyConsumption> byEquipment) 
            : base(buildingId, SimulationType.Energy, period, true, "Energy simulation completed")
        {
            TotalConsumption = total;
            AverageConsumption = average;
            PeakConsumption = peak;
            TotalCost = cost;
            CarbonFootprint = carbon;
            ConsumptionByFloor = byFloor ?? new Dictionary<string, Core.ValueObjects.EnergyConsumption>();
            ConsumptionByEquipmentType = byEquipment ?? new Dictionary<string, Core.ValueObjects.EnergyConsumption>();
        }
    }

    public class EnvironmentalSimulationResult : SimulationResult
    {
        public Core.ValueObjects.EnvironmentalConditions AverageConditions { get; }
        public Core.ValueObjects.EnvironmentalConditions PeakConditions { get; }
        public Core.ValueObjects.ComfortLevel AverageComfortLevel { get; }
        public TimeSpan ComfortableTimePercentage { get; }
        public Dictionary<string, Core.ValueObjects.EnvironmentalConditions> ConditionsByHour { get; }

        public EnvironmentalSimulationResult(Guid roomId, TimeSpan period,
                                           Core.ValueObjects.EnvironmentalConditions average,
                                           Core.ValueObjects.EnvironmentalConditions peak,
                                           Core.ValueObjects.ComfortLevel comfortLevel,
                                           TimeSpan comfortablePercentage,
                                           Dictionary<string, Core.ValueObjects.EnvironmentalConditions> hourlyConditions) 
            : base(roomId, SimulationType.Environmental, period, true, "Environmental simulation completed")
        {
            AverageConditions = average;
            PeakConditions = peak;
            AverageComfortLevel = comfortLevel;
            ComfortableTimePercentage = comfortablePercentage;
            ConditionsByHour = hourlyConditions ?? new Dictionary<string, Core.ValueObjects.EnvironmentalConditions>();
        }
    }

    public class EquipmentSimulationResult : SimulationResult
    {
        public Core.ValueObjects.OperationalMetrics AverageMetrics { get; }
        public Core.ValueObjects.PerformanceRating PerformanceRating { get; }
        public TimeSpan UptimePercentage { get; }
        public int PredictedFailures { get; }
        public List<MaintenanceRecommendation> MaintenanceRecommendations { get; }

        public EquipmentSimulationResult(Guid equipmentId, TimeSpan period,
                                       Core.ValueObjects.OperationalMetrics metrics,
                                       Core.ValueObjects.PerformanceRating rating,
                                       TimeSpan uptime, int failures,
                                       List<MaintenanceRecommendation> recommendations) 
            : base(equipmentId, SimulationType.Equipment, period, true, "Equipment simulation completed")
        {
            AverageMetrics = metrics;
            PerformanceRating = rating;
            UptimePercentage = uptime;
            PredictedFailures = failures;
            MaintenanceRecommendations = recommendations ?? new List<MaintenanceRecommendation>();
        }
    }

    // Analytics Results
    public abstract class AnalyticsResult : OperationResult
    {
        public Guid EntityId { get; }
        public EntityType EntityType { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
        public AnalysisType AnalysisType { get; }

        protected AnalyticsResult(Guid entityId, EntityType entityType, DateTime startTime, DateTime endTime, 
                                AnalysisType analysisType, bool isSuccess, string message) 
            : base(isSuccess, message)
        {
            EntityId = entityId;
            EntityType = entityType;
            StartTime = startTime;
            EndTime = endTime;
            AnalysisType = analysisType;
        }
    }

    public class EnergyAnalysisResult : AnalyticsResult
    {
        public decimal TotalConsumption { get; }
        public decimal AverageConsumption { get; }
        public decimal PeakConsumption { get; }
        public decimal ConsumptionTrend { get; } // Percentage change
        public List<ConsumptionAnomaly> Anomalies { get; }
        public List<EfficiencyRecommendation> Recommendations { get; }

        public EnergyAnalysisResult(Guid buildingId, DateTime startTime, DateTime endTime,
                                   decimal total, decimal average, decimal peak, decimal trend,
                                   List<ConsumptionAnomaly> anomalies, List<EfficiencyRecommendation> recommendations) 
            : base(buildingId, EntityType.Building, startTime, endTime, AnalysisType.Energy, true, "Energy analysis completed")
        {
            TotalConsumption = total;
            AverageConsumption = average;
            PeakConsumption = peak;
            ConsumptionTrend = trend;
            Anomalies = anomalies ?? new List<ConsumptionAnomaly>();
            Recommendations = recommendations ?? new List<EfficiencyRecommendation>();
        }
    }

    public class AnomalyDetectionResult : AnalyticsResult
    {
        public List<Anomaly> DetectedAnomalies { get; }
        public AnomalySeverity MaxSeverity { get; }
        public int TotalAnomalies { get; }
        public Dictionary<string, int> AnomaliesByType { get; }

        public AnomalyDetectionResult(Guid entityId, EntityType entityType, DateTime startTime, DateTime endTime,
                                    List<Anomaly> anomalies) 
            : base(entityId, entityType, startTime, endTime, AnalysisType.AnomalyDetection, true, "Anomaly detection completed")
        {
            DetectedAnomalies = anomalies ?? new List<Anomaly>();
            TotalAnomalies = DetectedAnomalies.Count;
            MaxSeverity = CalculateMaxSeverity();
            AnomaliesByType = GroupAnomaliesByType();
        }

        private AnomalySeverity CalculateMaxSeverity()
        {
            var maxSeverity = AnomalySeverity.Low;
            foreach (var anomaly in DetectedAnomalies)
            {
                if (anomaly.Severity > maxSeverity)
                    maxSeverity = anomaly.Severity;
            }
            return maxSeverity;
        }

        private Dictionary<string, int> GroupAnomaliesByType()
        {
            var grouped = new Dictionary<string, int>();
            foreach (var anomaly in DetectedAnomalies)
            {
                if (grouped.ContainsKey(anomaly.Type))
                    grouped[anomaly.Type]++;
                else
                    grouped[anomaly.Type] = 1;
            }
            return grouped;
        }
    }

    // Supporting Classes
    public class MaintenanceRecommendation
    {
        public Guid EquipmentId { get; }
        public string Recommendation { get; }
        public MaintenancePriority Priority { get; }
        public DateTime RecommendedDate { get; }
        public decimal EstimatedCost { get; }

        public MaintenanceRecommendation(Guid equipmentId, string recommendation, 
                                       MaintenancePriority priority, DateTime date, decimal cost)
        {
            EquipmentId = equipmentId;
            Recommendation = recommendation;
            Priority = priority;
            RecommendedDate = date;
            EstimatedCost = cost;
        }
    }

    public class ConsumptionAnomaly
    {
        public DateTime Timestamp { get; }
        public decimal ExpectedValue { get; }
        public decimal ActualValue { get; }
        public decimal DeviationPercentage { get; }
        public string Description { get; }

        public ConsumptionAnomaly(DateTime timestamp, decimal expected, decimal actual, 
                                 decimal deviation, string description)
        {
            Timestamp = timestamp;
            ExpectedValue = expected;
            ActualValue = actual;
            DeviationPercentage = deviation;
            Description = description;
        }
    }

    public class EfficiencyRecommendation
    {
        public string Recommendation { get; }
        public decimal PotentialSavings { get; }
        public decimal ImplementationCost { get; }
        public int PaybackPeriodMonths { get; }
        public RecommendationCategory Category { get; }

        public EfficiencyRecommendation(string recommendation, decimal savings, decimal cost, 
                                       int payback, RecommendationCategory category)
        {
            Recommendation = recommendation;
            PotentialSavings = savings;
            ImplementationCost = cost;
            PaybackPeriodMonths = payback;
            Category = category;
        }
    }

    public class Anomaly
    {
        public DateTime Timestamp { get; }
        public string Type { get; }
        public AnomalySeverity Severity { get; }
        public string Description { get; }
        public decimal Confidence { get; }
        public Dictionary<string, object> Context { get; }

        public Anomaly(DateTime timestamp, string type, AnomalySeverity severity, 
                      string description, decimal confidence, Dictionary<string, object> context = null)
        {
            Timestamp = timestamp;
            Type = type;
            Severity = severity;
            Description = description;
            Confidence = confidence;
            Context = context ?? new Dictionary<string, object>();
        }
    }

    // Enums
    public enum SimulationType
    {
        Energy,
        Environmental,
        Equipment,
        Occupancy,
        Scenario,
        Optimization
    }

    public enum SimulationState
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public enum EntityType
    {
        Building,
        Floor,
        Room,
        Equipment,
        Sensor
    }

    public enum AnalysisType
    {
        Energy,
        Environmental,
        Equipment,
        AnomalyDetection,
        Correlation,
        Benchmarking,
        KPI,
        Insights
    }

    public enum MaintenancePriority
    {
        Low,
        Medium,
        High,
        Critical,
        Emergency
    }

    public enum RecommendationCategory
    {
        Equipment,
        Operational,
        Behavioral,
        Structural,
        System
    }

    public enum AnomalySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}