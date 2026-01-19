using System;
using System.Collections.Generic;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Additional Supporting Classes and Enums
    /// 
    /// Architectural Intent:
    /// - Completes the type system for the digital twin interface layer
    /// - Provides supporting data structures for service operations
    /// - Enables comprehensive parameter and result handling
    /// - Supports extensibility and type safety
    /// </summary>

    // Sensor Status Enum (referenced in BuildingController)
    public enum SensorStatus
    {
        Active,
        Inactive,
        Error,
        Maintenance,
        Calibrating
    }

    // Additional Interface Extensions
    public interface IBuildingSimulationService : ISimulationService
    {
        // Extended building-specific simulation methods
    }

    public interface IDataAnalyticsService : IAnalyticsService
    {
        // Extended analytics methods
    }

    public interface IDataCollectionService : INotificationService
    {
        // Extended data collection methods
    }

    // Result Property Classes (referenced in service implementations)
    public class EnergySimulationResult : SimulationResult
    {
        public decimal TotalEnergyConsumption { get; set; }
        public Dictionary<string, decimal> EnergyConsumptionByType { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal CarbonFootprint { get; set; }
        public SimulationParameters Parameters { get; set; }
        public Guid SimulationId { get; set; }

        public EnergySimulationResult() : base(Guid.Empty, SimulationType.Energy, TimeSpan.Zero, true, "")
        {
        }

        public EnergySimulationResult(Guid buildingId, TimeSpan period, EnergyConsumption total, 
                                   EnergyConsumption average, EnergyConsumption peak, decimal cost, 
                                   decimal carbon, Dictionary<string, decimal> consumptionByType,
                                   Dictionary<string, EnergyConsumption> consumptionByFloor)
            : base(buildingId, SimulationType.Energy, period, true, "Energy simulation completed")
        {
            TotalEnergyConsumption = total.Amount;
            EnergyConsumptionByType = consumptionByType ?? new Dictionary<string, decimal>();
            EstimatedCost = cost;
            CarbonFootprint = carbon;
        }
    }

    public class EnvironmentalSimulationResult : SimulationResult
    {
        public Guid RoomId { get; set; }
        public List<EnvironmentalConditions> Conditions { get; set; }
        public decimal AverageTemperature { get; set; }
        public decimal AverageHumidity { get; set; }
        public decimal AverageAirQuality { get; set; }
        public decimal ComfortScore { get; set; }
        public string ErrorMessage { get; set; }

        public EnvironmentalSimulationResult() : base(Guid.Empty, SimulationType.Environmental, TimeSpan.Zero, true, "")
        {
        }

        public EnvironmentalSimulationResult(Guid roomId, TimeSpan period, List<EnvironmentalConditions> conditions,
                                          decimal avgTemp, decimal avgHumidity, decimal avgAirQuality, decimal comfortScore)
            : base(roomId, SimulationType.Environmental, period, true, "Environmental simulation completed")
        {
            RoomId = roomId;
            Conditions = conditions ?? new List<EnvironmentalConditions>();
            AverageTemperature = avgTemp;
            AverageHumidity = avgHumidity;
            AverageAirQuality = avgAirQuality;
            ComfortScore = comfortScore;
        }
    }

    public class EquipmentSimulationResult : SimulationResult
    {
        public Guid EquipmentId { get; set; }
        public List<OperationalMetrics> Metrics { get; set; }
        public decimal AverageEfficiency { get; set; }
        public decimal TotalEnergyConsumption { get; set; }
        public decimal TotalRuntimeHours { get; set; }
        public DateTime PredictedMaintenanceDate { get; set; }
        public string ErrorMessage { get; set; }

        public EquipmentSimulationResult() : base(Guid.Empty, SimulationType.Equipment, TimeSpan.Zero, true, "")
        {
        }

        public EquipmentSimulationResult(Guid equipmentId, TimeSpan period, List<OperationalMetrics> metrics,
                                      decimal avgEfficiency, decimal energyConsumption, decimal runtimeHours,
                                      DateTime maintenanceDate)
            : base(equipmentId, SimulationType.Equipment, period, true, "Equipment simulation completed")
        {
            EquipmentId = equipmentId;
            Metrics = metrics ?? new List<OperationalMetrics>();
            AverageEfficiency = avgEfficiency;
            TotalEnergyConsumption = energyConsumption;
            TotalRuntimeHours = runtimeHours;
            PredictedMaintenanceDate = maintenanceDate;
        }
    }

    public class ScenarioAnalysisResult : SimulationResult
    {
        public Scenario Scenario { get; set; }
        public List<SimulationResult> Results { get; set; }
        public decimal BaselineEnergyConsumption { get; set; }
        public decimal BestCaseEnergySavings { get; set; }
        public List<string> RecommendedActions { get; set; }
        public string ErrorMessage { get; set; }

        public ScenarioAnalysisResult() : base(Guid.Empty, SimulationType.Scenario, TimeSpan.Zero, true, "")
        {
        }

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
        public TimeSpan PredictionPeriod { get; set; }
        public List<EnergyPrediction> Predictions { get; set; }
        public decimal TotalPredictedConsumption { get; set; }
        public decimal ModelAccuracy { get; set; }
        public string ErrorMessage { get; set; }

        public EnergyPredictionResult() : base(Guid.Empty, SimulationType.Energy, TimeSpan.Zero, true, "")
        {
        }

        public EnergyPredictionResult(Guid buildingId, TimeSpan predictionPeriod, List<EnergyPrediction> predictions,
                                  decimal totalConsumption, decimal accuracy)
            : base(buildingId, SimulationType.Energy, predictionPeriod, true, "Energy prediction completed")
        {
            PredictionPeriod = predictionPeriod;
            Predictions = predictions ?? new List<EnergyPrediction>();
            TotalPredictedConsumption = totalConsumption;
            ModelAccuracy = accuracy;
        }
    }

    public class OccupancySimulationResult : SimulationResult
    {
        public Dictionary<Guid, RoomOccupancyStats> OccupancyByRoom { get; set; }
        public decimal TotalOccupancyRate { get; set; }
        public TimeSpan PeakOccupancyTime { get; set; }
        public Dictionary<string, decimal> OccupancyByHour { get; set; }
        public string ErrorMessage { get; set; }

        public OccupancySimulationResult() : base(Guid.Empty, SimulationType.Occupancy, TimeSpan.Zero, true, "")
        {
        }

        public OccupancySimulationResult(Guid buildingId, TimeSpan period, Dictionary<Guid, RoomOccupancyStats> occupancyByRoom)
            : base(buildingId, SimulationType.Occupancy, period, true, "Occupancy simulation completed")
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
        public decimal CurrentEnergyConsumption { get; set; }
        public List<OptimizationStrategy> OptimizationStrategies { get; set; }
        public decimal TotalPotentialSavings { get; set; }
        public List<OptimizationStrategy> RecommendedActions { get; set; }
        public string ErrorMessage { get; set; }

        public OptimizationResult() : base(Guid.Empty, SimulationType.Optimization, TimeSpan.Zero, true, "")
        {
        }

        public OptimizationResult(Guid buildingId, decimal currentConsumption, List<OptimizationStrategy> strategies)
            : base(buildingId, SimulationType.Optimization, TimeSpan.FromDays(30), true, "Optimization analysis completed")
        {
            CurrentEnergyConsumption = currentConsumption;
            OptimizationStrategies = strategies ?? new List<OptimizationStrategy>();
            TotalPotentialSavings = strategies?.Sum(s => s.PotentialEnergySavings) ?? 0;
            RecommendedActions = strategies?.Where(s => s.Priority > 0.7m && s.Feasibility > 0.6m).ToList() 
                               ?? new List<OptimizationStrategy>();
        }
    }

    // Supporting Data Classes
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
}