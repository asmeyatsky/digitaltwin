using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.DTOs
{
    /// <summary>
    /// Comprehensive KPIs for a building
    /// </summary>
    public class BuildingKPIs
    {
        public Guid BuildingId { get; set; }
        public string BuildingName { get; set; }
        public DateRange Period { get; set; }
        public DateTime GeneratedAt { get; set; }
        
        public EnergyKPIs EnergyKPIs { get; set; }
        public EnvironmentalKPIs EnvironmentalKPIs { get; set; }
        public OperationalKPIs OperationalKPIs { get; set; }
        public MaintenanceKPIs MaintenanceKPIs { get; set; }
        public OccupancyKPIs OccupancyKPIs { get; set; }
    }

    /// <summary>
    /// Energy-related KPIs
    /// </summary>
    public class EnergyKPIs
    {
        public double TotalConsumption { get; set; } // kWh
        public double AverageConsumption { get; set; } // kWh
        public double PeakConsumption { get; set; } // kWh
        public double ConsumptionPerSqm { get; set; } // kWh/m²
        public double Cost { get; set; } // $
        public double CarbonFootprint { get; set; } // kg CO₂
        public double EfficiencyScore { get; set; } // 0-100
        public double RenewableEnergyPercentage { get; set; } // %
    }

    /// <summary>
    /// Environmental-related KPIs
    /// </summary>
    public class EnvironmentalKPIs
    {
        public double AverageTemperature { get; set; } // °C
        public double TemperatureVariance { get; set; } // °C²
        public double AverageHumidity { get; set; } // %
        public double HumidityVariance { get; set; } // %²
        public double AirQualityIndex { get; set; } // 0-500
        public double CO2Level { get; set; } // ppm
        public double LightLevel { get; set; } // lux
        public double NoiseLevel { get; set; } // dB
        public double ComfortScore { get; set; } // 0-100
        public double SustainabilityScore { get; set; } // 0-100
    }

    /// <summary>
    /// Operational-related KPIs
    /// </summary>
    public class OperationalKPIs
    {
        public int TotalEquipment { get; set; }
        public int ActiveEquipment { get; set; }
        public double EquipmentUptime { get; set; } // %
        public double SystemAvailability { get; set; } // %
        public double ResponseTime { get; set; } // minutes
        public double ResolutionTime { get; set; } // minutes
        public int ActiveAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        public double ServiceLevelAgreement { get; set; } // %
    }

    /// <summary>
    /// Maintenance-related KPIs
    /// </summary>
    public class MaintenanceKPIs
    {
        public int ScheduledMaintenance { get; set; }
        public int CompletedMaintenance { get; set; }
        public int EmergencyMaintenance { get; set; }
        public int PreventiveMaintenance { get; set; }
        public double MaintenanceCost { get; set; } // $
        public double MeanTimeBetweenFailures { get; set; } // hours
        public double MeanTimeToRepair { get; set; } // hours
        public double MaintenanceCompliance { get; set; } // %
        public double BacklogHours { get; set; }
        public int WorkOrderCount { get; set; }
    }

    /// <summary>
    /// Occupancy-related KPIs
    /// </summary>
    public class OccupancyKPIs
    {
        public double AverageOccupancy { get; set; } // people
        public double PeakOccupancy { get; set; } // people
        public double OccupancyRate { get; set; } // %
        public double SpaceUtilization { get; set; } // %
        public double PeakHoursUtilization { get; set; } // %
        public double OffHoursUtilization { get; set; } // %
        public int TotalCapacity { get; set; }
        public double OccupancyPerSqm { get; set; } // people/m²
    }

    /// <summary>
    /// Metric trend data for time series analysis
    /// </summary>
    public class MetricTrend
    {
        public DateTime Timestamp { get; set; }
        public string MetricType { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public double? ChangePercentage { get; set; }
        public double? MovingAverage { get; set; }
    }

    /// <summary>
    /// Comparative analytics between multiple buildings
    /// </summary>
    public class ComparativeAnalytics
    {
        public List<Guid> BuildingIds { get; set; }
        public DateRange Period { get; set; }
        public DateTime GeneratedAt { get; set; }
        
        public List<BuildingKPIs> BuildingKPIs { get; set; }
        public List<BuildingRanking> Rankings { get; set; }
        public AggregateKPIs Averages { get; set; }
        public List<string> Insights { get; set; }
    }

    /// <summary>
    /// Building ranking for comparative analysis
    /// </summary>
    public class BuildingRanking
    {
        public Guid BuildingId { get; set; }
        public string BuildingName { get; set; }
        public int EnergyEfficiencyRank { get; set; }
        public int ComfortRank { get; set; }
        public int MaintenanceRank { get; set; }
        public int OccupancyRank { get; set; }
        public double OverallRank { get; set; }
    }

    /// <summary>
    /// Aggregate KPIs across multiple buildings
    /// </summary>
    public class AggregateKPIs
    {
        public double AverageEnergyConsumption { get; set; }
        public double AverageTemperature { get; set; }
        public double AverageOccupancyRate { get; set; }
        public double AverageEfficiencyScore { get; set; }
        public double AverageComfortScore { get; set; }
        public double TotalCost { get; set; }
        public double TotalCarbonFootprint { get; set; }
    }

    /// <summary>
    /// Predictive insights using ML algorithms
    /// </summary>
    public class PredictiveInsights
    {
        public Guid BuildingId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public double Confidence { get; set; } // 0-1
        
        public EnergyPrediction EnergyPrediction { get; set; }
        public MaintenancePrediction MaintenancePrediction { get; set; }
        public OccupancyPrediction OccupancyPrediction { get; set; }
        public CostPrediction CostPrediction { get; set; }
        
        public List<string> Recommendations { get; set; }
        public List<string> RiskFactors { get; set; }
        public List<string> Opportunities { get; set; }
    }

    /// <summary>
    /// Energy consumption prediction
    /// </summary>
    public class EnergyPrediction
    {
        public double NextDayConsumption { get; set; } // kWh
        public double NextWeekConsumption { get; set; } // kWh
        public double NextMonthConsumption { get; set; } // kWh
        public string Trend { get; set; } // Increasing, Decreasing, Stable
        public double Confidence { get; set; }
        public List<string> Factors { get; set; }
        public List<string> OptimizationSuggestions { get; set; }
    }

    /// <summary>
    /// Maintenance needs prediction
    /// </summary>
    public class MaintenancePrediction
    {
        public List<MaintenanceItem> UpcomingMaintenance { get; set; }
        public double RiskScore { get; set; } // 0-1
        public List<string> Recommendations { get; set; }
        public List<string> HighRiskEquipment { get; set; }
        public DateTime NextCriticalMaintenance { get; set; }
    }

    /// <summary>
    /// Individual maintenance item
    /// </summary>
    public class MaintenanceItem
    {
        public string EquipmentName { get; set; }
        public string EquipmentId { get; set; }
        public string Priority { get; set; } // High, Medium, Low
        public DateTime EstimatedDate { get; set; }
        public double EstimatedCost { get; set; }
        public string Description { get; set; }
        public List<string> RequiredParts { get; set; }
    }

    /// <summary>
    /// Occupancy prediction
    /// </summary>
    public class OccupancyPrediction
    {
        public double NextDayPeakOccupancy { get; set; } // %
        public double NextWeekAverageOccupancy { get; set; } // %
        public double NextMonthOccupancyTrend { get; set; }
        public string SeasonalPattern { get; set; }
        public double Confidence { get; set; }
        public List<string> OptimizationOpportunities { get; set; }
    }

    /// <summary>
    /// Cost prediction
    /// </summary>
    public class CostPrediction
    {
        public double NextMonthEnergyCost { get; set; } // $
        public double NextQuarterMaintenanceCost { get; set; } // $
        public double NextYearOperationalCost { get; set; } // $
        public string CostTrend { get; set; }
        public List<string> SavingsOpportunities { get; set; }
        public double PotentialSavings { get; set; } // $
    }

    /// <summary>
    /// Dashboard configuration
    /// </summary>
    public class DashboardConfiguration
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<WidgetConfiguration> Widgets { get; set; }
        public string Layout { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Individual widget configuration
    /// </summary>
    public class WidgetConfiguration
    {
        public string Id { get; set; }
        public string Type { get; set; } // Chart, KPI, Table, Map
        public string Title { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public WidgetDataSource DataSource { get; set; }
        public WidgetDisplaySettings DisplaySettings { get; set; }
    }

    /// <summary>
    /// Widget data source configuration
    /// </summary>
    public class WidgetDataSource
    {
        public string MetricType { get; set; }
        public Guid? BuildingId { get; set; }
        public List<Guid> SensorIds { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string AggregationType { get; set; } // Average, Sum, Min, Max
        public string TimeGranularity { get; set; } // Hour, Day, Week, Month
    }

    /// <summary>
    /// Widget display settings
    /// </summary>
    public class WidgetDisplaySettings
    {
        public string ChartType { get; set; } // Line, Bar, Pie, Gauge
        public string ColorScheme { get; set; }
        public bool ShowLegend { get; set; }
        public bool ShowGrid { get; set; }
        public bool ShowDataLabels { get; set; }
        public int DecimalPlaces { get; set; }
        public string Unit { get; set; }
    }

    /// <summary>
    /// Date range for analytics queries
    /// </summary>
    public class DateRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public DateRange(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public int Days => (End - Start).Days;
    }
}