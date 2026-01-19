using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Parameter Classes for Service Operations
    /// 
    /// Architectural Intent:
    /// - Provides configurable parameters for service operations
    /// - Encapsulates validation and default value logic
    /// - Enables flexible and extensible operation configuration
    /// - Supports parameter inheritance and composition
    /// </summary>

    // Base Parameter Classes
    public abstract class Parameters
    {
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();

        public virtual void Validate()
        {
            // Base validation logic
        }
    }

    // Simulation Parameters
    public class SimulationParameters : Parameters
    {
        public TimeSpan TimeStep { get; set; } = TimeSpan.FromMinutes(15);
        public bool EnableRealTimeData { get; set; } = true;
        public bool UseHistoricalData { get; set; } = true;
        public int RandomSeed { get; set; } = 0;
        public bool EnableLogging { get; set; } = true;
        
        // Energy simulation specific
        public decimal EnergyRate { get; set; } = 0.12m; // $/kWh
        public bool EnableHVAC { get; set; } = true;
        public bool EnableLighting { get; set; } = true;
        public bool EnableEquipment { get; set; } = true;

        public override void Validate()
        {
            base.Validate();
            
            if (TimeStep <= TimeSpan.Zero)
                throw new ArgumentException("Time step must be positive");
            
            if (TimeStep.TotalHours > 24)
                throw new ArgumentException("Time step cannot exceed 24 hours");
            
            if (EnergyRate < 0)
                throw new ArgumentException("Energy rate cannot be negative");
        }
    }

    public class EnvironmentalParameters : SimulationParameters
    {
        public Core.ValueObjects.Temperature TargetTemperature { get; set; } = Core.ValueObjects.Temperature.FromCelsius(22);
        public decimal TargetHumidity { get; set; } = 45;
        public decimal TargetLightLevel { get; set; } = 500;
        public bool EnableHVACSimulation { get; set; } = true;
        public bool EnableLightingSimulation { get; set; } = true;
        public bool EnableOccupancyEffects { get; set; } = true;

        public override void Validate()
        {
            base.Validate();
            
            if (TargetHumidity < 0 || TargetHumidity > 100)
                throw new ArgumentException("Target humidity must be between 0 and 100%");
            
            if (TargetLightLevel < 0)
                throw new ArgumentException("Target light level cannot be negative");
        }
    }

    public class EquipmentParameters : SimulationParameters
    {
        public bool EnableFailureSimulation { get; set; } = true;
        public decimal FailureProbability { get; set; } = 0.01m;
        public bool EnableMaintenanceSimulation { get; set; } = true;
        public bool EnablePerformanceDegradation { get; set; } = true;
        public decimal DegradationRate { get; set; } = 0.001m;

        public override void Validate()
        {
            base.Validate();
            
            if (FailureProbability < 0 || FailureProbability > 1)
                throw new ArgumentException("Failure probability must be between 0 and 1");
            
            if (DegradationRate < 0)
                throw new ArgumentException("Degradation rate cannot be negative");
        }
    }

    public class OccupancyParameters : SimulationParameters
    {
        public int MaxOccupancy { get; set; } = 100;
        public decimal BaseOccupancyRate { get; set; } = 0.7m;
        public bool EnableTimeBasedPatterns { get; set; } = true;
        public bool EnableSeasonalVariation { get; set; } = true;
        public Dictionary<DayOfWeek, decimal> DailyPatterns { get; set; } = new Dictionary<DayOfWeek, decimal>();

        public override void Validate()
        {
            base.Validate();
            
            if (MaxOccupancy <= 0)
                throw new ArgumentException("Max occupancy must be positive");
            
            if (BaseOccupancyRate < 0 || BaseOccupancyRate > 1)
                throw new ArgumentException("Base occupancy rate must be between 0 and 1");
        }
    }

    // Analytics Parameters
    public class AnalysisParameters : Parameters
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan AggregationInterval { get; set; } = TimeSpan.FromHours(1);
        public bool EnableOutlierDetection { get; set; } = true;
        public decimal OutlierThreshold { get; set; } = 2.0m;

        public override void Validate()
        {
            base.Validate();
            
            if (StartTime >= EndTime)
                throw new ArgumentException("Start time must be before end time");
            
            if (AggregationInterval <= TimeSpan.Zero)
                throw new ArgumentException("Aggregation interval must be positive");
        }
    }

    public class AnomalyDetectionParameters : AnalysisParameters
    {
        public AnomalyDetectionMethod Method { get; set; } = AnomalyDetectionMethod.Statistical;
        public decimal Sensitivity { get; set; } = 0.8m;
        public int MinimumDataPoints { get; set; } = 30;
        public bool EnableSeasonalAdjustment { get; set; } = true;

        public override void Validate()
        {
            base.Validate();
            
            if (Sensitivity < 0 || Sensitivity > 1)
                throw new ArgumentException("Sensitivity must be between 0 and 1");
            
            if (MinimumDataPoints < 10)
                throw new ArgumentException("Minimum data points must be at least 10");
        }
    }

    public class KPIParameters : AnalysisParameters
    {
        public List<string> KPIs { get; set; } = new List<string>();
        public bool EnableBenchmarking { get; set; } = true;
        public string BenchmarkCategory { get; set; } = "Default";
        public bool EnableTrendAnalysis { get; set; } = true;

        public override void Validate()
        {
            base.Validate();
            
            if (KPIs == null || KPIs.Count == 0)
                throw new ArgumentException("At least one KPI must be specified");
        }
    }

    public class CorrelationParameters : AnalysisParameters
    {
        public CorrelationMethod Method { get; set; } = CorrelationMethod.Pearson;
        public decimal MinimumCorrelation { get; set; } = 0.5m;
        public bool EnableCausalityAnalysis { get; set; } = false;
        public int LagPeriods { get; set; } = 5;

        public override void Validate()
        {
            base.Validate();
            
            if (MinimumCorrelation < -1 || MinimumCorrelation > 1)
                throw new ArgumentException("Minimum correlation must be between -1 and 1");
            
            if (LagPeriods < 0)
                throw new ArgumentException("Lag periods cannot be negative");
        }
    }

    // Prediction Parameters
    public class PredictionParameters : Parameters
    {
        public PredictionModel Model { get; set; } = PredictionModel.LinearRegression;
        public int TrainingDataDays { get; set; } = 30;
        public bool EnableFeatureSelection { get; set; } = true;
        public decimal ConfidenceInterval { get; set; } = 0.95m;

        public override void Validate()
        {
            base.Validate();
            
            if (TrainingDataDays < 7)
                throw new ArgumentException("Training data must be at least 7 days");
            
            if (ConfidenceInterval < 0.5 || ConfidenceInterval > 0.99)
                throw new ArgumentException("Confidence interval must be between 0.5 and 0.99");
        }
    }

    // Optimization Parameters
    public class OptimizationParameters : Parameters
    {
        public OptimizationObjective Objective { get; set; } = OptimizationObjective.EnergyEfficiency;
        public List<OptimizationConstraint> Constraints { get; set; } = new List<OptimizationConstraint>();
        public int MaxIterations { get; set; } = 1000;
        public decimal Tolerance { get; set; } = 0.001m;

        public override void Validate()
        {
            base.Validate();
            
            if (MaxIterations <= 0)
                throw new ArgumentException("Max iterations must be positive");
            
            if (Tolerance <= 0)
                throw new ArgumentException("Tolerance must be positive");
        }
    }

    // Scenario Parameters
    public class ScenarioParameters : Parameters
    {
        public string ScenarioName { get; set; }
        public ScenarioType Type { get; set; }
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
        public bool EnableComparativeAnalysis { get; set; } = true;

        public override void Validate()
        {
            base.Validate();
            
            if (string.IsNullOrEmpty(ScenarioName))
                throw new ArgumentException("Scenario name is required");
        }
    }

    // Notification Parameters
    public class NotificationParameters : Parameters
    {
        public List<string> Recipients { get; set; } = new List<string>();
        public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public bool EnableRetry { get; set; } = true;
        public int MaxRetries { get; set; } = 3;

        public override void Validate()
        {
            base.Validate();
            
            if (Recipients == null || Recipients.Count == 0)
                throw new ArgumentException("At least one recipient is required");
            
            if (MaxRetries < 0)
                throw new ArgumentException("Max retries cannot be negative");
        }
    }

    // Backup Parameters
    public class BackupParameters : Parameters
    {
        public BackupType Type { get; set; } = BackupType.Full;
        public string DestinationPath { get; set; }
        public bool EnableCompression { get; set; } = true;
        public bool EnableEncryption { get; set; } = false;
        public int RetentionDays { get; set; } = 30;

        public override void Validate()
        {
            base.Validate();
            
            if (string.IsNullOrEmpty(DestinationPath))
                throw new ArgumentException("Destination path is required");
            
            if (RetentionDays <= 0)
                throw new ArgumentException("Retention days must be positive");
        }
    }

    // Supporting Classes
    public class OptimizationConstraint
    {
        public string Name { get; set; }
        public ConstraintType Type { get; set; }
        public decimal MinValue { get; set; }
        public decimal MaxValue { get; set; }
        public string Description { get; set; }
    }

    // Enums
    public enum AnomalyDetectionMethod
    {
        Statistical,
        MachineLearning,
        RuleBased,
        Hybrid
    }

    public enum CorrelationMethod
    {
        Pearson,
        Spearman,
        Kendall,
        MutualInformation
    }

    public enum PredictionModel
    {
        LinearRegression,
        PolynomialRegression,
        NeuralNetwork,
        RandomForest,
        ARIMA,
        LSTM
    }

    public enum OptimizationObjective
    {
        EnergyEfficiency,
        CostMinimization,
        ComfortMaximization,
        Sustainability,
        Reliability
    }

    public enum ScenarioType
    {
        WhatIf,
        SensitivityAnalysis,
        StressTest,
        Planning,
        Emergency
    }

    public enum NotificationChannel
    {
        Email,
        SMS,
        Push,
        Webhook,
        Slack,
        Teams
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public enum BackupType
    {
        Full,
        Incremental,
        Differential
    }

    public enum ConstraintType
    {
        LessThan,
        GreaterThan,
        Equal,
        Range,
        Percentage
    }

    // Extended Parameter Classes
    public class InsightParameters : Parameters
    {
        public List<InsightCategory> Categories { get; set; } = new List<InsightCategory>();
        public decimal MinConfidence { get; set; } = 0.7m;
        public int MaxInsights { get; set; } = 50;
        public bool EnableRecommendations { get; set; } = true;

        public override void Validate()
        {
            base.Validate();
            
            if (MinConfidence < 0 || MinConfidence > 1)
                throw new ArgumentException("Min confidence must be between 0 and 1");
            
            if (MaxInsights <= 0)
                throw new ArgumentException("Max insights must be positive");
        }
    }

    public class BenchmarkParameters : Parameters
    {
        public string BenchmarkGroup { get; set; } = "Default";
        public List<string> Metrics { get; set; } = new List<string>();
        public bool EnablePercentileRanking { get; set; } = true;
        public bool EnableOutlierIdentification { get; set; } = true;

        public override void Validate()
        {
            base.Validate();
            
            if (string.IsNullOrEmpty(BenchmarkGroup))
                throw new ArgumentException("Benchmark group is required");
            
            if (Metrics == null || Metrics.Count == 0)
                throw new ArgumentException("At least one metric must be specified");
        }
    }

    // Supporting Scenario Classes
    public class Scenario
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public TimeSpan TimePeriod { get; set; }
        public List<ScenarioVariation> Variations { get; set; } = new List<ScenarioVariation>();
        public Dictionary<string, object> BaseParameters { get; set; } = new Dictionary<string, object>();
    }

    public class ScenarioVariation
    {
        public string Name { get; set; }
        public Dictionary<string, object> ParameterChanges { get; set; } = new Dictionary<string, object>();
        public decimal ExpectedImpact { get; set; }
        public string Rationale { get; set; }
    }

    // Equipment Schedule Classes
    public class EquipmentSchedule
    {
        public List<OperatingWindow> OperatingWindows { get; set; } = new List<OperatingWindow>();
        public List<ExceptionPeriod> Exceptions { get; set; } = new List<ExceptionPeriod>();
        public bool EnableWeekendOperation { get; set; } = false;
    }

    public class OperatingWindow
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal LoadFactor { get; set; } = 1.0m;
    }

    public class ExceptionPeriod
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsShutdown { get; set; } = false;
        public decimal LoadFactor { get; set; } = 0.5m;
        public string Reason { get; set; }
    }

    // Additional Supporting Classes
    public class OptimizationStrategyDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal PotentialSavings { get; set; }
        public decimal ImplementationCost { get; set; }
        public int Priority { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}