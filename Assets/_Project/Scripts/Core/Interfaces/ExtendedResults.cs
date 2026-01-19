using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Extended Simulation Result Classes
    /// 
    /// Architectural Intent:
    /// - Completes the result hierarchy for all simulation types
    /// - Provides standardized data structures for simulation outputs
    /// - Enables consistent API responses across all service operations
    /// - Supports detailed simulation analysis and reporting
    /// </summary>

    // Additional Simulation Results
    public class ScenarioAnalysisResult : SimulationResult
    {
        public Scenario Scenario { get; }
        public List<SimulationResult> Results { get; }
        public decimal BaselineEnergyConsumption { get; }
        public decimal BestCaseEnergySavings { get; }
        public List<string> RecommendedActions { get; }

        public ScenarioAnalysisResult(Guid buildingId, Scenario scenario, List<SimulationResult> results,
                                   decimal baselineConsumption, decimal bestCaseSavings, List<string> recommendations)
            : base(buildingId, SimulationType.Scenario, scenario.TimePeriod, true, "Scenario analysis completed")
        {
            Scenario = scenario;
            Results = results ?? new List<SimulationResult>();
            BaselineEnergyConsumption = baselineConsumption;
            BestCaseEnergySavings = bestCaseSavings;
            RecommendedActions = recommendations ?? new List<string>();
        }
    }

    public class EnergyPredictionResult : SimulationResult
    {
        public List<EnergyPrediction> Predictions { get; }
        public decimal TotalPredictedConsumption { get; }
        public decimal ModelAccuracy { get; }
        public Dictionary<string, object> ModelMetadata { get; }

        public EnergyPredictionResult(Guid buildingId, TimeSpan predictionPeriod, List<EnergyPrediction> predictions,
                                  decimal totalConsumption, decimal accuracy, bool isSuccess = true, string errorMessage = null)
            : base(buildingId, SimulationType.Energy, predictionPeriod, isSuccess, 
                   isSuccess ? "Energy prediction completed" : errorMessage)
        {
            Predictions = predictions ?? new List<EnergyPrediction>();
            TotalPredictedConsumption = totalConsumption;
            ModelAccuracy = accuracy;
            ModelMetadata = new Dictionary<string, object>();
        }
    }

    public class OccupancySimulationResult : SimulationResult
    {
        public Dictionary<Guid, RoomOccupancyStats> OccupancyByRoom { get; }
        public decimal TotalOccupancyRate { get; }
        public TimeSpan PeakOccupancyTime { get; }
        public Dictionary<string, decimal> OccupancyByHour { get; }

        public OccupancySimulationResult(Guid buildingId, TimeSpan period, Dictionary<Guid, RoomOccupancyStats> occupancyByRoom,
                                       bool isSuccess = true, string errorMessage = null)
            : base(buildingId, SimulationType.Occupancy, period, isSuccess, 
                   isSuccess ? "Occupancy simulation completed" : errorMessage)
        {
            OccupancyByRoom = occupancyByRoom ?? new Dictionary<Guid, RoomOccupancyStats>();
            TotalOccupancyRate = CalculateOverallOccupancyRate();
            PeakOccupancyTime = TimeSpan.FromHours(14); // Default 2 PM peak
            OccupancyByHour = new Dictionary<string, decimal>();
        }

        private decimal CalculateOverallOccupancyRate()
        {
            if (OccupancyByRoom.Count == 0) return 0;
            decimal totalRate = 0;
            foreach (var stats in OccupancyByRoom.Values)
            {
                totalRate += stats.UtilizationRate;
            }
            return totalRate / OccupancyByRoom.Count;
        }
    }

    public class OptimizationResult : SimulationResult
    {
        public decimal CurrentEnergyConsumption { get; }
        public List<OptimizationStrategy> OptimizationStrategies { get; }
        public decimal TotalPotentialSavings { get; }
        public List<OptimizationStrategy> RecommendedActions { get; }

        public OptimizationResult(Guid buildingId, decimal currentConsumption, List<OptimizationStrategy> strategies,
                               bool isSuccess = true, string errorMessage = null)
            : base(buildingId, SimulationType.Optimization, TimeSpan.FromDays(30), isSuccess,
                   isSuccess ? "Optimization analysis completed" : errorMessage)
        {
            CurrentEnergyConsumption = currentConsumption;
            OptimizationStrategies = strategies ?? new List<OptimizationStrategy>();
            TotalPotentialSavings = strategies?.Sum(s => s.PotentialEnergySavings) ?? 0;
            RecommendedActions = strategies?.Where(s => s.Priority > 0.7m && s.Feasibility > 0.6m).ToList() 
                               ?? new List<OptimizationStrategy>();
        }
    }

    // Supporting Classes
    public class EnergyPrediction
    {
        public DateTime Date { get; set; }
        public decimal PredictedConsumption { get; set; }
        public decimal ConfidenceLevel { get; set; }
        public Dictionary<string, object> Factors { get; set; }

        public EnergyPrediction(DateTime date, decimal consumption, decimal confidence, Dictionary<string, object> factors = null)
        {
            Date = date;
            PredictedConsumption = consumption;
            ConfidenceLevel = confidence;
            Factors = factors ?? new Dictionary<string, object>();
        }
    }

    public class RoomOccupancyStats
    {
        public Guid RoomId { get; set; }
        public decimal AverageOccupancy { get; set; }
        public decimal PeakOccupancy { get; set; }
        public decimal TotalOccupancyHours { get; set; }
        public decimal UtilizationRate { get; set; }

        public RoomOccupancyStats(Guid roomId, decimal average, decimal peak, decimal totalHours, decimal utilization)
        {
            RoomId = roomId;
            AverageOccupancy = average;
            PeakOccupancy = peak;
            TotalOccupancyHours = totalHours;
            UtilizationRate = utilization;
        }
    }

    public class OptimizationStrategy
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal PotentialEnergySavings { get; set; }
        public decimal ImplementationCost { get; set; }
        public decimal PaybackPeriodDays { get; set; }
        public decimal Priority { get; set; }
        public decimal Feasibility { get; set; }

        public OptimizationStrategy(string name, string description, decimal savings, decimal cost, 
                                decimal paybackPeriod, decimal priority, decimal feasibility)
        {
            Name = name;
            Description = description;
            PotentialEnergySavings = savings;
            ImplementationCost = cost;
            PaybackPeriodDays = paybackPeriod;
            Priority = priority;
            Feasibility = feasibility;
        }
    }

    public class OccupancyData
    {
        public Guid RoomId { get; set; }
        public int Count { get; set; }
        public TimeSpan Duration { get; set; }

        public OccupancyData(Guid roomId, int count, TimeSpan duration)
        {
            RoomId = roomId;
            Count = count;
            Duration = duration;
        }
    }

    // Additional Analytics Results
    public class EnvironmentalAnalysisResult : AnalyticsResult
    {
        public decimal AverageTemperature { get; }
        public decimal AverageHumidity { get; }
        public decimal AverageAirQuality { get; }
        public ComfortLevel ComfortScore { get; }
        public TimeSpan ComfortableTimePercentage { get; }
        public List<EnvironmentalAnomaly> EnvironmentalAnomalies { get; }

        public EnvironmentalAnalysisResult(Guid roomId, DateTime startTime, DateTime endTime,
                                       decimal avgTemp, decimal avgHumidity, decimal avgAirQuality,
                                       ComfortLevel comfortScore, TimeSpan comfortPercentage,
                                       List<EnvironmentalAnomaly> anomalies)
            : base(roomId, EntityType.Room, startTime, endTime, AnalysisType.Environmental, true, "Environmental analysis completed")
        {
            AverageTemperature = avgTemp;
            AverageHumidity = avgHumidity;
            AverageAirQuality = avgAirQuality;
            ComfortScore = comfortScore;
            ComfortableTimePercentage = comfortPercentage;
            EnvironmentalAnomalies = anomalies ?? new List<EnvironmentalAnomaly>();
        }
    }

    public class EquipmentAnalysisResult : AnalyticsResult
    {
        public decimal AverageEfficiency { get; }
        public decimal TotalRuntimeHours { get; }
        public PerformanceRating OverallPerformance { get; }
        public int MaintenanceEvents { get; }
        public List<EquipmentAnomaly> EquipmentAnomalies { get; }

        public EquipmentAnalysisResult(Guid equipmentId, DateTime startTime, DateTime endTime,
                                    decimal avgEfficiency, decimal runtime, PerformanceRating performance,
                                    int maintenanceEvents, List<EquipmentAnomaly> anomalies)
            : base(equipmentId, EntityType.Equipment, startTime, endTime, AnalysisType.Equipment, true, "Equipment analysis completed")
        {
            AverageEfficiency = avgEfficiency;
            TotalRuntimeHours = runtime;
            OverallPerformance = performance;
            MaintenanceEvents = maintenanceEvents;
            EquipmentAnomalies = anomalies ?? new List<EquipmentAnomaly>();
        }
    }

    public class KPIReport : AnalyticsResult
    {
        public Dictionary<string, decimal> KPIValues { get; }
        public Dictionary<string, decimal> TargetValues { get; }
        public Dictionary<string, decimal> AchievementRates { get; }
        public List<KPIAlert> Alerts { get; }

        public KPIReport(Guid buildingId, DateTime startTime, DateTime endTime,
                        Dictionary<string, decimal> kpiValues, Dictionary<string, decimal> targets,
                        Dictionary<string, decimal> achievements, List<KPIAlert> alerts)
            : base(buildingId, EntityType.Building, startTime, endTime, AnalysisType.KPI, true, "KPI report generated")
        {
            KPIValues = kpiValues ?? new Dictionary<string, decimal>();
            TargetValues = targets ?? new Dictionary<string, decimal>();
            AchievementRates = achievements ?? new Dictionary<string, decimal>();
            Alerts = alerts ?? new List<KPIAlert>();
        }
    }

    public class InsightsReport : AnalyticsResult
    {
        public List<Insight> Insights { get; }
        public InsightCategory Categories { get; }
        public decimal OverallScore { get; }
        public List<RecommendationAction> RecommendedActions { get; }

        public InsightsReport(Guid buildingId, DateTime startTime, DateTime endTime,
                           List<Insight> insights, InsightCategory categories, decimal overallScore,
                           List<RecommendationAction> recommendedActions)
            : base(buildingId, EntityType.Building, startTime, endTime, AnalysisType.Insights, true, "Insights generated")
        {
            Insights = insights ?? new List<Insight>();
            Categories = categories;
            OverallScore = overallScore;
            RecommendedActions = recommendedActions ?? new List<RecommendationAction>();
        }
    }

    public class BenchmarkingResult : AnalyticsResult
    {
        public Dictionary<string, decimal> CurrentPerformance { get; }
        public Dictionary<string, decimal> BenchmarkValues { get; }
        public Dictionary<string, decimal> PerformanceGaps { get; }
        public BenchmarkRanking Ranking { get; }

        public BenchmarkingResult(Guid buildingId, DateTime startTime, DateTime endTime,
                                Dictionary<string, decimal> current, Dictionary<string, decimal> benchmarks,
                                Dictionary<string, decimal> gaps, BenchmarkRanking ranking)
            : base(buildingId, EntityType.Building, startTime, endTime, AnalysisType.Benchmarking, true, "Benchmarking completed")
        {
            CurrentPerformance = current ?? new Dictionary<string, decimal>();
            BenchmarkValues = benchmarks ?? new Dictionary<string, decimal>();
            PerformanceGaps = gaps ?? new Dictionary<string, decimal>();
            Ranking = ranking;
        }
    }

    public class CorrelationAnalysisResult : AnalyticsResult
    {
        public Dictionary<string, CorrelationPair> Correlations { get; }
        public List<SignificantCorrelation> SignificantCorrelations { get; }
        public decimal MaxCorrelation { get; }
        public decimal MinCorrelation { get; }

        public CorrelationAnalysisResult(IEnumerable<Guid> entityIds, DateTime startTime, DateTime endTime,
                                      Dictionary<string, CorrelationPair> correlations, List<SignificantCorrelation> significant)
            : base(entityIds.First(), EntityType.Building, startTime, endTime, AnalysisType.Correlation, true, "Correlation analysis completed")
        {
            Correlations = correlations ?? new Dictionary<string, CorrelationPair>();
            SignificantCorrelations = significant ?? new List<SignificantCorrelation>();
            MaxCorrelation = CalculateMaxCorrelation();
            MinCorrelation = CalculateMinCorrelation();
        }

        private decimal CalculateMaxCorrelation()
        {
            decimal max = 0;
            foreach (var corr in Correlations.Values)
            {
                if (Math.Abs(corr.CorrelationCoefficient) > Math.Abs(max))
                    max = corr.CorrelationCoefficient;
            }
            return max;
        }

        private decimal CalculateMinCorrelation()
        {
            decimal min = 0;
            foreach (var corr in Correlations.Values)
            {
                if (Math.Abs(corr.CorrelationCoefficient) < Math.Abs(min))
                    min = corr.CorrelationCoefficient;
            }
            return min;
        }
    }

    // Supporting Classes for Analytics Results
    public class EnvironmentalAnomaly : Anomaly
    {
        public string MetricType { get; }
        public decimal MeasuredValue { get; }
        public decimal ExpectedValue { get; }

        public EnvironmentalAnomaly(DateTime timestamp, string metricType, decimal measured, decimal expected,
                                  AnomalySeverity severity, string description, decimal confidence)
            : base(timestamp, $"Environmental_{metricType}", severity, description, confidence)
        {
            MetricType = metricType;
            MeasuredValue = measured;
            ExpectedValue = expected;
        }
    }

    public class EquipmentAnomaly : Anomaly
    {
        public Guid EquipmentId { get; }
        public string EquipmentType { get; }
        public string FailureMode { get; }

        public EquipmentAnomaly(DateTime timestamp, Guid equipmentId, string equipmentType, string failureMode,
                              AnomalySeverity severity, string description, decimal confidence)
            : base(timestamp, $"Equipment_{failureMode}", severity, description, confidence)
        {
            EquipmentId = equipmentId;
            EquipmentType = equipmentType;
            FailureMode = failureMode;
        }
    }

    public class KPIAlert
    {
        public string KPIName { get; }
        public decimal CurrentValue { get; }
        public decimal TargetValue { get; }
        public decimal Deviation { get; }
        public AlertSeverity Severity { get; }
        public string Message { get; }

        public KPIAlert(string kpiName, decimal current, decimal target, decimal deviation, AlertSeverity severity, string message)
        {
            KPIName = kpiName;
            CurrentValue = current;
            TargetValue = target;
            Deviation = deviation;
            Severity = severity;
            Message = message;
        }
    }

    public class Insight
    {
        public string Title { get; }
        public string Description { get; }
        public InsightCategory Category { get; }
        public decimal Impact { get; }
        public decimal Confidence { get; }
        public Dictionary<string, object> SupportingData { get; }

        public Insight(string title, string description, InsightCategory category, decimal impact, decimal confidence, Dictionary<string, object> data = null)
        {
            Title = title;
            Description = description;
            Category = category;
            Impact = impact;
            Confidence = confidence;
            SupportingData = data ?? new Dictionary<string, object>();
        }
    }

    public class RecommendationAction
    {
        public string Action { get; }
        public decimal Priority { get; }
        public decimal EstimatedEffort { get; }
        public decimal ExpectedBenefit { get; }
        public List<string> Requirements { get; }

        public RecommendationAction(string action, decimal priority, decimal effort, decimal benefit, List<string> requirements)
        {
            Action = action;
            Priority = priority;
            EstimatedEffort = effort;
            ExpectedBenefit = benefit;
            Requirements = requirements ?? new List<string>();
        }
    }

    public class BenchmarkRanking
    {
        public int OverallRank { get; }
        public int TotalBuildings { get; }
        public decimal Percentile { get; }
        public Dictionary<string, int> CategoryRanks { get; }

        public BenchmarkRanking(int rank, int total, decimal percentile, Dictionary<string, int> categoryRanks)
        {
            OverallRank = rank;
            TotalBuildings = total;
            Percentile = percentile;
            CategoryRanks = categoryRanks ?? new Dictionary<string, int>();
        }
    }

    public class CorrelationPair
    {
        public string Variable1 { get; }
        public string Variable2 { get; }
        public decimal CorrelationCoefficient { get; }
        public decimal PValue { get; }
        public bool IsSignificant { get; }

        public CorrelationPair(string var1, string var2, decimal coefficient, decimal pValue, bool significant)
        {
            Variable1 = var1;
            Variable2 = var2;
            CorrelationCoefficient = coefficient;
            PValue = pValue;
            IsSignificant = significant;
        }
    }

    public class SignificantCorrelation
    {
        public string Variable1 { get; }
        public string Variable2 { get; }
        public decimal Correlation { get; }
        public CorrelationType Type { get; }
        public string Interpretation { get; }

        public SignificantCorrelation(string var1, string var2, decimal correlation, string interpretation)
        {
            Variable1 = var1;
            Variable2 = var2;
            Correlation = correlation;
            Type = correlation > 0 ? CorrelationType.Positive : CorrelationType.Negative;
            Interpretation = interpretation;
        }
    }

    // Additional Enums
    public enum PerformanceRating
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Critical
    }

    public enum InsightCategory
    {
        Energy,
        Comfort,
        Maintenance,
        Occupancy,
        Operational
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical
    }

    public enum CorrelationType
    {
        Positive,
        Negative
    }

    [Flags]
    public enum BenchmarkCategories
    {
        None = 0,
        Energy = 1,
        Comfort = 2,
        Maintenance = 4,
        Occupancy = 8,
        Operational = 16,
        All = 31
    }
}