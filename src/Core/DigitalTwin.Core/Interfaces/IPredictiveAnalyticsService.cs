using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Predictive analytics service interface
    /// </summary>
    public interface IPredictiveAnalyticsService
    {
        /// <summary>
        /// Gets comprehensive predictive insights for a building
        /// </summary>
        Task<PredictiveInsights> GetPredictiveInsightsAsync(Guid buildingId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Predicts energy consumption using time series forecasting
        /// </summary>
        Task<EnergyPrediction> PredictEnergyConsumptionAsync(List<SensorReading> historicalData);

        /// <summary>
        /// Predicts maintenance needs using reliability models
        /// </summary>
        Task<MaintenancePrediction> PredictMaintenanceNeedsAsync(Guid buildingId, List<SensorReading> historicalData);

        /// <summary>
        /// Predicts occupancy patterns using ML clustering
        /// </summary>
        Task<OccupancyPrediction> PredictOccupancyAsync(List<SensorReading> historicalData);

        /// <summary>
        /// Predicts costs using regression models
        /// </summary>
        Task<CostPrediction> PredictCostsAsync(List<SensorReading> historicalData);

        /// <summary>
        /// Predicts environmental conditions
        /// </summary>
        Task<EnvironmentalPrediction> PredictEnvironmentalConditionsAsync(List<SensorReading> historicalData);

        /// <summary>
        /// Predicts equipment health using vibration and performance data
        /// </summary>
        Task<EquipmentHealthPrediction> PredictEquipmentHealthAsync(Guid buildingId, List<SensorReading> historicalData);

        /// <summary>
        /// Gets anomaly detection results
        /// </summary>
        Task<List<AnomalyDetection>> DetectAnomaliesAsync(Guid buildingId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets forecasting model performance metrics
        /// </summary>
        Task<ModelPerformanceMetrics> GetModelPerformanceMetricsAsync();

        /// <summary>
        /// Retrains ML models with new data
        /// </summary>
        Task<TrainingResult> RetrainModelsAsync(ModelRetrainingRequest request);

        /// <summary>
        /// Gets feature importance for predictions
        /// </summary>
        Task<FeatureImportance> GetFeatureImportanceAsync(string modelName);

        /// <summary>
        /// Gets prediction confidence intervals
        /// </summary>
        Task<PredictionConfidenceIntervals> GetPredictionConfidenceIntervalsAsync(Guid buildingId, string predictionType, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets scenario analysis results
        /// </summary>
        Task<ScenarioAnalysis> GetScenarioAnalysisAsync(Guid buildingId, List<ScenarioDefinition> scenarios);

        /// <summary>
        /// Gets optimization recommendations
        /// </summary>
        Task<List<OptimizationRecommendation>> GetOptimizationRecommendationsAsync(Guid buildingId, List<string> objectiveFunctions);

        /// <summary>
        /// Evaluates what-if scenarios
        /// </summary>
        Task<List<WhatIfResult>> EvaluateWhatIfScenariosAsync(Guid buildingId, List<WhatIfScenario> scenarios);

        /// <summary>
        /// Gets predictive model explainability
        /// </summary>
        Task<ModelExplainability> GetModelExplainabilityAsync(string modelName, object predictionInput);

        /// <summary>
        /// Gets forecast accuracy analysis
        /// </summary>
        Task<ForecastAccuracyAnalysis> GetForecastAccuracyAnalysisAsync(Guid buildingId, string predictionType, DateTime periodStart, DateTime periodEnd);

        /// <summary>
        /// Gets automated insights from data
        /// </summary>
        Task<List<AutomatedInsight>> GetAutomatedInsightsAsync(Guid buildingId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Creates custom prediction model
        /// </summary>
        Task<CustomModelResult> CreateCustomModelAsync(CustomModelDefinition modelDefinition);

        /// <summary>
        /// Deploys predictive model to production
        /// </summary>
        Task<ModelDeploymentResult> DeployModelAsync(string modelId, DeploymentConfiguration deploymentConfig);

        /// <summary>
        /// Monitors model performance and drift
        /// </summary>
        Task<ModelMonitoringResult> MonitorModelPerformanceAsync(string modelId, DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Environmental prediction
    /// </summary>
    public class EnvironmentalPrediction
    {
        public TemperatureRange NextDayTemperatureRange { get; set; }
        public HumidityRange NextWeekHumidityRange { get; set; }
        public AirQualityForecast AirQualityForecast { get; set; }
        public double ComfortIndexPrediction { get; set; }
        public List<string> EnvironmentalRisks { get; set; }
        public List<string> MitigationStrategies { get; set; }
        public double Confidence { get; set; }
    }

    /// <summary>
    /// Temperature range prediction
    /// </summary>
    public class TemperatureRange
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
    }

    /// <summary>
    /// Humidity range prediction
    /// </summary>
    public class HumidityRange
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
    }

    /// <summary>
    /// Air quality forecast
    /// </summary>
    public class AirQualityForecast
    {
        public double AverageAQI { get; set; }
        public double CO2Level { get; set; } // ppm
        public double ParticulateMatter { get; set; } // μg/m³
        public double VOCLevel { get; set; } // ppb
        public string OverallQuality { get; set; } // Good, Moderate, Poor, etc.
        public List<string> HealthRecommendations { get; set; }
    }

    /// <summary>
    /// Equipment health prediction
    /// </summary>
    public class EquipmentHealthPrediction
    {
        public List<EquipmentHealth> EquipmentHealth { get; set; }
        public double OverallHealthScore { get; set; }
        public List<EquipmentHealth> CriticalEquipmentAlerts { get; set; }
        public List<string> MaintenanceRecommendations { get; set; }
        public DateTime NextCriticalMaintenance { get; set; }
        public double Confidence { get; set; }
    }

    /// <summary>
    /// Individual equipment health
    /// </summary>
    public class EquipmentHealth
    {
        public string EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public double HealthScore { get; set; } // 0-100
        public string Status { get; set; } // Healthy, Warning, Critical
        public List<HealthIndicator> Indicators { get; set; }
        public DateTime NextMaintenance { get; set; }
        public DateTime? PredictedFailure { get; set; }
        public List<string> RiskFactors { get; set; }
    }

    /// <summary>
    /// Health indicator for equipment
    /// </summary>
    public class HealthIndicator
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public double Threshold { get; set; }
        public string Status { get; set; }
        public string Unit { get; set; }
        public double Trend { get; set; } // Positive, Negative, Stable
        public DateTime LastReading { get; set; }
    }

    /// <summary>
    /// Anomaly detection result
    /// </summary>
    public class AnomalyDetection
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public DateTime DetectedAt { get; set; }
        public AnomalySeverity Severity { get; set; }
        public string Description { get; set; }
        public string Equipment { get; set; }
        public string Location { get; set; }
        public double ExpectedValue { get; set; }
        public double ActualValue { get; set; }
        public double DeviationPercentage { get; set; }
        public double Confidence { get; set; }
        public string Category { get; set; }
        public List<string> RecommendedActions { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string AcknowledgedBy { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ResolvedBy { get; set; }
    }

    /// <summary>
    /// Model performance metrics
    /// </summary>
    public class ModelPerformanceMetrics
    {
        public ModelMetrics EnergyConsumptionModel { get; set; }
        public ModelMetrics MaintenancePredictionModel { get; set; }
        public ModelMetrics OccupancyPredictionModel { get; set; }
        public ModelMetrics CostPredictionModel { get; set; }
        public ModelMetrics AnomalyDetectionModel { get; set; }
        public double OverallAccuracy { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, double> CustomModelMetrics { get; set; }
    }

    /// <summary>
    /// Individual model metrics
    /// </summary>
    public class ModelMetrics
    {
        public double Accuracy { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double F1Score { get; set; }
        public double MAE { get; set; } // Mean Absolute Error
        public double RMSE { get; set; } // Root Mean Square Error
        public double R2Score { get; set; }
        public DateTime LastTrained { get; set; }
        public int TrainingDataSize { get; set; }
        public double TrainingTime { get; set; } // seconds
        public string ModelVersion { get; set; }
    }

    /// <summary>
    /// Model retraining request
    /// </summary>
    public class ModelRetrainingRequest
    {
        public List<string> Models { get; set; }
        public object TrainingData { get; set; }
        public DateTime TrainingDataStartDate { get; set; }
        public DateTime TrainingDataEndDate { get; set; }
        public Dictionary<string, object> TrainingParameters { get; set; }
        public bool RetainExistingModel { get; set; }
        public string RequestedBy { get; set; }
        public bool ValidateBeforeDeployment { get; set; }
    }

    /// <summary>
    /// Training result
    /// </summary>
    public class TrainingResult
    {
        public Guid Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TrainingStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public TrainingResult EnergyModelResult { get; set; }
        public TrainingResult MaintenanceModelResult { get; set; }
        public TrainingResult OccupancyModelResult { get; set; }
        public TrainingResult CostModelResult { get; set; }
        public TimeSpan TrainingDuration { get; set; }
        public List<string> Warnings { get; set; }
        public Dictionary<string, object> TrainingMetrics { get; set; }
    }

    /// <summary>
    /// Individual training result
    /// </summary>
    public class TrainingResult
    {
        public bool Success { get; set; }
        public double Accuracy { get; set; }
        public double ValidationScore { get; set; }
        public string ModelVersion { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, double> Metrics { get; set; }
    }

    /// <summary>
    /// Feature importance
    /// </summary>
    public class FeatureImportance
    {
        public string ModelName { get; set; }
        public List<Feature> Features { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Explanation { get; set; }
    }

    /// <summary>
    /// Individual feature importance
    /// </summary>
    public class Feature
    {
        public string Name { get; set; }
        public double Importance { get; set; }
        public string Description { get; set; }
        public string DataType { get; set; }
        public double Correlation { get; set; }
        public double PValue { get; set; }
    }

    /// <summary>
    /// Prediction confidence intervals
    /// </summary>
    public class PredictionConfidenceIntervals
    {
        public string PredictionType { get; set; }
        public List<ConfidenceInterval> Intervals { get; set; }
        public double ConfidenceLevel { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Confidence interval
    /// </summary>
    public class ConfidenceInterval
    {
        public DateTime Timestamp { get; set; }
        public double PredictedValue { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public double IntervalWidth { get; set; }
        public double Probability { get; set; }
    }

    /// <summary>
    /// Scenario analysis
    /// </summary>
    public class ScenarioAnalysis
    {
        public Guid BuildingId { get; set; }
        public List<ScenarioResult> Results { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<string> Recommendations { get; set; }
        public ScenarioComparison BestCaseScenario { get; set; }
        public ScenarioComparison WorstCaseScenario { get; set; }
        public ScenarioComparison MostLikelyScenario { get; set; }
    }

    /// <summary>
    /// Scenario definition
    /// </summary>
    public class ScenarioDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> AffectedSystems { get; set; }
    }

    /// <summary>
    /// Scenario result
    /// </summary>
    public class ScenarioResult
    {
        public string ScenarioName { get; set; }
        public Dictionary<string, double> PredictedOutcomes { get; set; }
        public double Confidence { get; set; }
        public List<string> KeyFindings { get; set; }
        public List<string> RiskFactors { get; set; }
        public EconomicImpact EconomicImpact { get; set; }
    }

    /// <summary>
    /// Scenario comparison
    /// </summary>
    public class ScenarioComparison
    {
        public string ScenarioName { get; set; }
        public double TotalCostImpact { get; set; }
        public double EnergyImpact { get; set; }
        public double ComfortImpact { get; set; }
        public double MaintenanceImpact { get; set; }
        public double OverallScore { get; set; }
        public List<string> KeyAdvantages { get; set; }
        public List<string> KeyDisadvantages { get; set; }
    }

    /// <summary>
    /// Economic impact
    /// </summary>
    public class EconomicImpact
    {
        public double InitialInvestment { get; set; }
        public double AnnualSavings { get; set; }
        public double PaybackPeriod { get; set; } // months
        public double ROI { get; set; } // percentage
        public double NPV { get; set; } // Net Present Value
        public double IRR { get; set; } // Internal Rate of Return
    }

    /// <summary>
    /// Optimization recommendation
    /// </summary>
    public class OptimizationRecommendation
    {
        public Guid Id { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public double Priority { get; set; }
        public double PotentialSavings { get; set; }
        public double ImplementationCost { get; set; }
        public double PaybackPeriod { get; set; }
        public string ObjectiveFunction { get; set; }
        public List<string> RequiredActions { get; set; }
        public List<string> Dependencies { get; set; }
        public DateTime RecommendedImplementationDate { get; set; }
        public double Confidence { get; set; }
        public List<string> SupportingData { get; set; }
    }

    /// <summary>
    /// What-if scenario
    /// </summary>
    public class WhatIfScenario
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Changes { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> MetricsToEvaluate { get; set; }
    }

    /// <summary>
    /// What-if result
    /// </summary>
    public class WhatIfResult
    {
        public string ScenarioName { get; set; }
        public Dictionary<string, MetricComparison> MetricComparisons { get; set; }
        public double OverallImpact { get; set; }
        public List<string> KeyInsights { get; set; }
        public List<string> Recommendations { get; set; }
        public double Confidence { get; set; }
    }

    /// <summary>
    /// Metric comparison
    /// </summary>
    public class MetricComparison
    {
        public string MetricName { get; set; }
        public double BaselineValue { get; set; }
        public double ScenarioValue { get; set; }
        public double ChangePercentage { get; set; }
        public double AbsoluteChange { get; set; }
        public string Unit { get; set; }
        public TrendDirection Trend { get; set; }
    }

    /// <summary>
    /// Model explainability
    /// </summary>
    public class ModelExplainability
    {
        public string ModelName { get; set; }
        public double Prediction { get; set; }
        public List<ExplanatoryFactor> Factors { get; set; }
        public string Explanation { get; set; }
        public double Confidence { get; set; }
        public DateTime GeneratedAt { get; set; }
        public Dictionary<string, object> RawInput { get; set; }
    }

    /// <summary>
    /// Explanatory factor
    /// </summary>
    public class ExplanatoryFactor
    {
        public string FeatureName { get; set; }
        public double Value { get; set; }
        public double Impact { get; set; }
        public string Description { get; set; }
        public double Importance { get; set; }
        public string Unit { get; set; }
    }

    /// <summary>
    /// Forecast accuracy analysis
    /// </summary>
    public class ForecastAccuracyAnalysis
    {
        public string PredictionType { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int ForecastCount { get; set; }
        public double MeanAbsoluteError { get; set; }
        public double MeanAbsolutePercentageError { get; set; }
        public double RootMeanSquareError { get; set; }
        public double Bias { get; set; }
        public List<ForecastErrorDetail> ErrorDetails { get; set; }
        public List<AccuracyTrend> AccuracyTrends { get; set; }
        public List<string> Recommendations { get; set; }
    }

    /// <summary>
    /// Forecast error detail
    /// </summary>
    public class ForecastErrorDetail
    {
        public DateTime ForecastDate { get; set; }
        public double ForecastedValue { get; set; }
        public double ActualValue { get; set; }
        public double Error { get; set; }
        public double PercentageError { get; set; }
        public string ForecastingMethod { get; set; }
    }

    /// <summary>
    /// Accuracy trend
    /// </summary>
    public class AccuracyTrend
    {
        public DateTime Period { get; set; }
        public double Accuracy { get; set; }
        public double Error { get; set; }
        public string Metric { get; set; }
    }

    /// <summary>
    /// Automated insight
    /// </summary>
    public class AutomatedInsight
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DetectedAt { get; set; }
        public double Confidence { get; set; }
        public string Category { get; set; }
        public List<string> SupportingData { get; set; }
        public List<string> Recommendations { get; set; }
        public InsightSeverity Severity { get; set; }
        public bool IsActionable { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string AcknowledgedBy { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ResolvedBy { get; set; }
    }

    /// <summary>
    /// Custom model definition
    /// </summary>
    public class CustomModelDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; } // Regression, Classification, Clustering, etc.
        public List<string> Features { get; set; }
        public string TargetVariable { get; set; }
        public Dictionary<string, object> Hyperparameters { get; set; }
        public string Algorithm { get; set; }
        public Dictionary<string, object> TrainingConfiguration { get; set; }
        public List<string> ValidationMetrics { get; set; }
    }

    /// <summary>
    /// Custom model result
    /// </summary>
    public class CustomModelResult
    {
        public string ModelId { get; set; }
        public bool Success { get; set; }
        public double Accuracy { get; set; }
        public string ModelVersion { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, double> Metrics { get; set; }
        public List<string> Warnings { get; set; }
    }

    /// <summary>
    /// Deployment configuration
    /// </summary>
    public class DeploymentConfiguration
    {
        public string Environment { get; set; } // Production, Staging, Development
        public Dictionary<string, object> EnvironmentVariables { get; set; }
        public int Replicas { get; set; }
        public Dictionary<string, object> ResourceRequirements { get; set; }
        public bool EnableMonitoring { get; set; }
        public bool EnableDriftDetection { get; set; }
        public Dictionary<string, object> ScalingConfiguration { get; set; }
    }

    /// <summary>
    /// Model deployment result
    /// </summary>
    public class ModelDeploymentResult
    {
        public string DeploymentId { get; set; }
        public bool Success { get; set; }
        public string EndpointUrl { get; set; }
        public DateTime DeployedAt { get; set; }
        public string ModelVersion { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> ValidationResults { get; set; }
    }

    /// <summary>
    /// Model monitoring result
    /// </summary>
    public class ModelMonitoringResult
    {
        public string ModelId { get; set; }
        public DateTime MonitoringPeriodStart { get; set; }
        public DateTime MonitoringPeriodEnd { get; set; }
        public double CurrentAccuracy { get; set; }
        public double BaselineAccuracy { get; set; }
        public double AccuracyDrift { get; set; }
        public bool DriftDetected { get; set; }
        public List<DataDriftAlert> DriftAlerts { get; set; }
        public List<PerformanceMetric> PerformanceMetrics { get; set; }
        public List<string> Recommendations { get; set; }
    }

    /// <summary>
    /// Data drift alert
    /// </summary>
    public class DataDriftAlert
    {
        public string Feature { get; set; }
        public double DriftScore { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public List<string> RecommendedActions { get; set; }
    }

    /// <summary>
    /// Performance metric
    /// </summary>
    public class PerformanceMetric
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public double BaselineValue { get; set; }
        public double Change { get; set; }
        public string Unit { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Enums for predictive analytics
    /// </summary>
    public enum AnomalySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum TrainingStatus
    {
        Queued,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    public enum TrendDirection
    {
        Increasing,
        Decreasing,
        Stable
    }

    public enum InsightSeverity
    {
        Informational,
        Low,
        Medium,
        High,
        Critical
    }
}