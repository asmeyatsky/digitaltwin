using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Analytics service interface for business intelligence and KPI calculations
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Gets comprehensive KPIs for a building
        /// </summary>
        Task<BuildingKPIs> GetBuildingKPIsAsync(Guid buildingId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets trend data for specific metrics
        /// </summary>
        Task<List<MetricTrend>> GetMetricTrendsAsync(Guid buildingId, string metricType, DateTime startDate, DateTime endDate, TimeSpan interval);

        /// <summary>
        /// Gets comparative analytics between multiple buildings
        /// </summary>
        Task<ComparativeAnalytics> GetComparativeAnalyticsAsync(List<Guid> buildingIds, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets predictive insights using ML algorithms
        /// </summary>
        Task<PredictiveInsights> GetPredictiveInsightsAsync(Guid buildingId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets real-time dashboard data
        /// </summary>
        Task<DashboardData> GetDashboardDataAsync(Guid buildingId, DashboardConfiguration config);

        /// <summary>
        /// Gets energy analytics
        /// </summary>
        Task<EnergyAnalytics> GetEnergyAnalyticsAsync(Guid buildingId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets environmental analytics
        /// </summary>
        Task<EnvironmentalAnalytics> GetEnvironmentalAnalyticsAsync(Guid buildingId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets operational analytics
        /// </summary>
        Task<OperationalAnalytics> GetOperationalAnalyticsAsync(Guid buildingId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets maintenance analytics
        /// </summary>
        Task<MaintenanceAnalytics> GetMaintenanceAnalyticsAsync(Guid buildingId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets occupancy analytics
        /// </summary>
        Task<OccupancyAnalytics> GetOccupancyAnalyticsAsync(Guid buildingId, DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Dashboard data structure
    /// </summary>
    public class DashboardData
    {
        public Guid BuildingId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<WidgetData> Widgets { get; set; }
        public List<Alert> ActiveAlerts { get; set; }
        public SummaryKPIs SummaryKPIs { get; set; }
    }

    /// <summary>
    /// Individual widget data
    /// </summary>
    public class WidgetData
    {
        public string WidgetId { get; set; }
        public string Type { get; set; }
        public object Data { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Summary KPIs for dashboard overview
    /// </summary>
    public class SummaryKPIs
    {
        public double CurrentEnergyConsumption { get; set; }
        public double CurrentTemperature { get; set; }
        public double CurrentOccupancy { get; set; }
        public int ActiveAlerts { get; set; }
        public double SystemEfficiency { get; set; }
        public double ComfortScore { get; set; }
    }

    /// <summary>
    /// Energy analytics data
    /// </summary>
    public class EnergyAnalytics
    {
        public List<EnergyConsumptionData> ConsumptionData { get; set; }
        public EnergyCostBreakdown CostBreakdown { get; set; }
        public EnergyEfficiencyMetrics EfficiencyMetrics { get; set; }
        public List<EnergyAnomaly> Anomalies { get; set; }
        public EnergyBenchmarking Benchmarking { get; set; }
    }

    /// <summary>
    /// Energy consumption data point
    /// </summary>
    public class EnergyConsumptionData
    {
        public DateTime Timestamp { get; set; }
        public double Consumption { get; set; } // kWh
        public double Cost { get; set; } // $
        public string Source { get; set; } // HVAC, Lighting, Equipment
        public double PeakDemand { get; set; } // kW
    }

    /// <summary>
    /// Energy cost breakdown
    /// </summary>
    public class EnergyCostBreakdown
    {
        public double TotalCost { get; set; }
        public double EnergyCost { get; set; }
        public double DemandCost { get; set; }
        public double FixedCost { get; set; }
        public Dictionary<string, double> CostBySource { get; set; }
        public Dictionary<string, double> CostByTime { get; set; }
    }

    /// <summary>
    /// Energy efficiency metrics
    /// </summary>
    public class EnergyEfficiencyMetrics
    {
        public double EnergyUseIntensity { get; set; } // kWh/m²
        public double Co2Emissions { get; set; } // kg CO₂
        public double RenewablePercentage { get; set; } // %
        public double EfficiencyScore { get; set; } // 0-100
        public List<EfficiencyRecommendation> Recommendations { get; set; }
    }

    /// <summary>
    /// Energy efficiency recommendation
    /// </summary>
    public class EfficiencyRecommendation
    {
        public string Category { get; set; }
        public string Description { get; set; }
        public double PotentialSavings { get; set; } // $
        public double ImplementationCost { get; set; } // $
        public double PaybackPeriod { get; set; } // months
        public int Priority { get; set; } // 1-5
    }

    /// <summary>
    /// Energy anomaly detection
    /// </summary>
    public class EnergyAnomaly
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } // Spike, Drop, Pattern
        public double ExpectedValue { get; set; }
        public double ActualValue { get; set; }
        public double DeviationPercentage { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; } // Low, Medium, High, Critical
    }

    /// <summary>
    /// Energy benchmarking data
    /// </summary>
    public class EnergyBenchmarking
    {
        public double BuildingScore { get; set; }
        public double IndustryAverage { get; set; }
        public double BestInClass { get; set; }
        public double PercentileRank { get; set; }
        public List<BenchmarkCategory> Categories { get; set; }
    }

    /// <summary>
    /// Benchmark category
    /// </summary>
    public class BenchmarkCategory
    {
        public string Name { get; set; }
        public double BuildingValue { get; set; }
        public double BenchmarkValue { get; set; }
        public double Performance { get; set; } // %
    }

    /// <summary>
    /// Environmental analytics data
    /// </summary>
    public class EnvironmentalAnalytics
    {
        public List<TemperatureData> TemperatureData { get; set; }
        public List<HumidityData> HumidityData { get; set; }
        public List<AirQualityData> AirQualityData { get; set; }
        public ComfortAnalytics ComfortAnalytics { get; set; }
        public EnvironmentalCompliance Compliance { get; set; }
    }

    /// <summary>
    /// Temperature data point
    /// </summary>
    public class TemperatureData
    {
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; } // °C
        public string Zone { get; set; }
        public double SetPoint { get; set; }
        public double Deviation { get; set; }
    }

    /// <summary>
    /// Humidity data point
    /// </summary>
    public class HumidityData
    {
        public DateTime Timestamp { get; set; }
        public double Humidity { get; set; } // %
        public string Zone { get; set; }
        public double SetPoint { get; set; }
        public double Deviation { get; set; }
    }

    /// <summary>
    /// Air quality data point
    /// </summary>
    public class AirQualityData
    {
        public DateTime Timestamp { get; set; }
        public double CO2 { get; set; } // ppm
        public double VOC { get; set; } // ppb
        public double ParticulateMatter { get; set; } // μg/m³
        public double AirQualityIndex { get; set; }
        public string Zone { get; set; }
    }

    /// <summary>
    /// Comfort analytics
    /// </summary>
    public class ComfortAnalytics
    {
        public double OverallComfortScore { get; set; } // 0-100
        public double TemperatureComfort { get; set; } // 0-100
        public double HumidityComfort { get; set; } // 0-100
        public double AirQualityComfort { get; set; } // 0-100
        public List<ComfortZone> ComfortZones { get; set; }
        public List<ComfortIssue> Issues { get; set; }
    }

    /// <summary>
    /// Comfort zone data
    /// </summary>
    public class ComfortZone
    {
        public string ZoneName { get; set; }
        public double ComfortScore { get; set; }
        public double OccupancyPercentage { get; set; }
        public List<string> Issues { get; set; }
    }

    /// <summary>
    /// Comfort issue detection
    /// </summary>
    public class ComfortIssue
    {
        public string Type { get; set; } // Temperature, Humidity, AirQuality
        public string Zone { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Recommendation { get; set; }
    }

    /// <summary>
    /// Environmental compliance data
    /// </summary>
    public class EnvironmentalCompliance
    {
        public bool IsCompliant { get; set; }
        public double ComplianceScore { get; set; } // 0-100
        public List<ComplianceStandard> Standards { get; set; }
        public List<ComplianceViolation> Violations { get; set; }
    }

    /// <summary>
    /// Compliance standard
    /// </summary>
    public class ComplianceStandard
    {
        public string Name { get; set; }
        public bool IsCompliant { get; set; }
        public double Score { get; set; }
        public List<ComplianceMetric> Metrics { get; set; }
    }

    /// <summary>
    /// Compliance metric
    /// </summary>
    public class ComplianceMetric
    {
        public string Name { get; set; }
        public double ActualValue { get; set; }
        public double RequiredValue { get; set; }
        public bool IsCompliant { get; set; }
        public string Unit { get; set; }
    }

    /// <summary>
    /// Compliance violation
    /// </summary>
    public class ComplianceViolation
    {
        public string Standard { get; set; }
        public string Metric { get; set; }
        public double ActualValue { get; set; }
        public double RequiredValue { get; set; }
        public string Severity { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Operational analytics data
    /// </summary>
    public class OperationalAnalytics
    {
        public List<EquipmentPerformance> EquipmentPerformance { get; set; }
        public SystemAvailability Availability { get; set; }
        public List<OperationalAlert> Alerts { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; }
    }

    /// <summary>
    /// Equipment performance data
    /// </summary>
    public class EquipmentPerformance
    {
        public string EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public string Type { get; set; }
        public double Uptime { get; set; } // %
        public double Efficiency { get; set; } // %
        public int AlertCount { get; set; }
        public double EnergyConsumption { get; set; } // kWh
        public List<PerformanceMetric> Metrics { get; set; }
    }

    /// <summary>
    /// Performance metric
    /// </summary>
    public class PerformanceMetric
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public double Target { get; set; }
        public double Performance { get; set; } // %
    }

    /// <summary>
    /// System availability data
    /// </summary>
    public class SystemAvailability
    {
        public double OverallAvailability { get; set; } // %
        public double PlannedDowntime { get; set; } // %
        public double UnplannedDowntime { get; set; } // %
        public double MeanTimeBetweenFailures { get; set; } // hours
        public double MeanTimeToRepair { get; set; } // hours
        public List<SystemDowntime> DowntimeEvents { get; set; }
    }

    /// <summary>
    /// System downtime event
    /// </summary>
    public class SystemDowntime
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double Duration { get; set; } // hours
        public string System { get; set; }
        public string Cause { get; set; }
        public bool WasPlanned { get; set; }
        public string Impact { get; set; }
    }

    /// <summary>
    /// Operational alert
    /// </summary>
    public class OperationalAlert
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public string Severity { get; set; }
        public string Equipment { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } // Active, Acknowledged, Resolved
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    /// <summary>
    /// Performance metrics summary
    /// </summary>
    public class PerformanceMetrics
    {
        public double OverallEfficiency { get; set; } // %
        public double ResponseTime { get; set; } // minutes
        public double ResolutionTime { get; set; } // minutes
        public double FirstCallResolution { get; set; } // %
        public double CustomerSatisfaction { get; set; } // 0-5
        public List<PerformanceTrend> Trends { get; set; }
    }

    /// <summary>
    /// Performance trend data
    /// </summary>
    public class PerformanceTrend
    {
        public string Metric { get; set; }
        public double CurrentValue { get; set; }
        public double PreviousValue { get; set; }
        public double ChangePercentage { get; set; }
        public string Trend { get; set; } // Improving, Declining, Stable
    }

    /// <summary>
    /// Maintenance analytics data
    /// </summary>
    public class MaintenanceAnalytics
    {
        public List<MaintenanceWorkOrder> WorkOrders { get; set; }
        public MaintenanceMetrics Metrics { get; set; }
        public List<MaintenanceCost> Costs { get; set; }
        public PredictiveMaintenance PredictiveMaintenance { get; set; }
    }

    /// <summary>
    /// Maintenance work order
    /// </summary>
    public class MaintenanceWorkOrder
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; } // Preventive, Corrective, Emergency
        public string Priority { get; set; }
        public string Equipment { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double Duration { get; set; } // hours
        public double Cost { get; set; } // $
    }

    /// <summary>
    /// Maintenance metrics
    /// </summary>
    public class MaintenanceMetrics
    {
        public int TotalWorkOrders { get; set; }
        public int CompletedWorkOrders { get; set; }
        public int PendingWorkOrders { get; set; }
        public double CompletionRate { get; set; } // %
        public double AverageResponseTime { get; set; } // hours
        public double AverageResolutionTime { get; set; } // hours
        public double PreventiveMaintenancePercentage { get; set; } // %
        public double EmergencyMaintenancePercentage { get; set; } // %
    }

    /// <summary>
    /// Maintenance cost data
    /// </summary>
    public class MaintenanceCost
    {
        public string Category { get; set; }
        public double Cost { get; set; } // $
        public double Budget { get; set; } // $
        public double Variance { get; set; } // %
        public List<CostBreakdownItem> Breakdown { get; set; }
    }

    /// <summary>
    /// Cost breakdown item
    /// </summary>
    public class CostBreakdownItem
    {
        public string Item { get; set; }
        public double Cost { get; set; } // $
        public double Percentage { get; set; } // %
    }

    /// <summary>
    /// Predictive maintenance data
    /// </summary>
    public class PredictiveMaintenance
    {
        public List<EquipmentHealth> EquipmentHealth { get; set; }
        public List<MaintenancePrediction> Predictions { get; set; }
        public double OverallHealthScore { get; set; } // 0-100
        public List<string> Recommendations { get; set; }
    }

    /// <summary>
    /// Equipment health data
    /// </summary>
    public class EquipmentHealth
    {
        public string EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public double HealthScore { get; set; } // 0-100
        public string Status { get; set; } // Healthy, Warning, Critical
        public List<HealthIndicator> Indicators { get; set; }
        public DateTime NextMaintenance { get; set; }
    }

    /// <summary>
    /// Health indicator
    /// </summary>
    public class HealthIndicator
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public double Threshold { get; set; }
        public string Status { get; set; }
        public string Unit { get; set; }
    }

    /// <summary>
    /// Occupancy analytics data
    /// </summary>
    public class OccupancyAnalytics
    {
        public List<OccupancyData> OccupancyData { get; set; }
        public SpaceUtilization Utilization { get; set; }
        public OccupancyPatterns Patterns { get; set; }
        public OccupancyForecast Forecast { get; set; }
    }

    /// <summary>
    /// Occupancy data point
    /// </summary>
    public class OccupancyData
    {
        public DateTime Timestamp { get; set; }
        public double OccupancyCount { get; set; }
        public double OccupancyRate { get; set; } // %
        public string Zone { get; set; }
        public double Capacity { get; set; }
    }

    /// <summary>
    /// Space utilization data
    /// </summary>
    public class SpaceUtilization
    {
        public double OverallUtilization { get; set; } // %
        public double PeakUtilization { get; set; } // %
        public double AverageUtilization { get; set; } // %
        public List<ZoneUtilization> ZoneUtilization { get; set; }
        public List<UnderutilizedSpace> UnderutilizedSpaces { get; set; }
    }

    /// <summary>
    /// Zone utilization data
    /// </summary>
    public class ZoneUtilization
    {
        public string ZoneName { get; set; }
        public double UtilizationRate { get; set; } // %
        public double Capacity { get; set; }
        public double AverageOccupancy { get; set; }
        public double PeakOccupancy { get; set; }
        public List<UtilizationTrend> Trends { get; set; }
    }

    /// <summary>
    /// Underutilized space
    /// </summary>
    public class UnderutilizedSpace
    {
        public string SpaceName { get; set; }
        public double UtilizationRate { get; set; } // %
        public double Capacity { get; set; }
        public double PotentialSavings { get; set; } // $
        public List<string> Recommendations { get; set; }
    }

    /// <summary>
    /// Utilization trend
    /// </summary>
    public class UtilizationTrend
    {
        public string Period { get; set; }
        public double UtilizationRate { get; set; } // %
        public double ChangePercentage { get; set; }
        public string Trend { get; set; }
    }

    /// <summary>
    /// Occupancy patterns
    /// </summary>
    public class OccupancyPatterns
    {
        public List<DailyPattern> DailyPatterns { get; set; }
        public List<WeeklyPattern> WeeklyPatterns { get; set; }
        public List<MonthlyPattern> MonthlyPatterns { get; set; }
        public List<SeasonalPattern> SeasonalPatterns { get; set; }
    }

    /// <summary>
    /// Daily occupancy pattern
    /// </summary>
    public class DailyPattern
    {
        public int Hour { get; set; }
        public double AverageOccupancy { get; set; }
        public double PeakOccupancy { get; set; }
        public double OccupancyRate { get; set; } // %
    }

    /// <summary>
    /// Weekly occupancy pattern
    /// </summary>
    public class WeeklyPattern
    {
        public DayOfWeek DayOfWeek { get; set; }
        public double AverageOccupancy { get; set; }
        public double PeakOccupancy { get; set; }
        public double OccupancyRate { get; set; } // %
    }

    /// <summary>
    /// Monthly occupancy pattern
    /// </summary>
    public class MonthlyPattern
    {
        public int Month { get; set; }
        public double AverageOccupancy { get; set; }
        public double PeakOccupancy { get; set; }
        public double OccupancyRate { get; set; } // %
    }

    /// <summary>
    /// Seasonal occupancy pattern
    /// </summary>
    public class SeasonalPattern
    {
        public string Season { get; set; }
        public double AverageOccupancy { get; set; }
        public double PeakOccupancy { get; set; }
        public double OccupancyRate { get; set; } // %
        public List<string> Characteristics { get; set; }
    }

    /// <summary>
    /// Occupancy forecast
    /// </summary>
    public class OccupancyForecast
    {
        public List<ForecastData> Forecasts { get; set; }
        public double Confidence { get; set; } // 0-1
        public List<string> Factors { get; set; }
        public List<string> Recommendations { get; set; }
    }

    /// <summary>
    /// Forecast data point
    /// </summary>
    public class ForecastData
    {
        public DateTime Date { get; set; }
        public double PredictedOccupancy { get; set; }
        public double ConfidenceInterval { get; set; } // +/-
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
    }

    /// <summary>
    /// Alert data structure
    /// </summary>
    public class Alert
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Severity { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }
        public string Status { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string AcknowledgedBy { get; set; }
    }
}