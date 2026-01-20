using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.DTOs
{
    /// <summary>
    /// Report template definition
    /// </summary>
    public class ReportTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ReportType Type { get; set; }
        public ReportConfiguration Configuration { get; set; }
        public ReportSchedule Schedule { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public List<string> Tags { get; set; }
    }

    /// <summary>
    /// Report configuration
    /// </summary>
    public class ReportConfiguration
    {
        public ReportFormat Format { get; set; }
        public List<Guid> BuildingIds { get; set; }
        public List<string> Recipients { get; set; }
        public List<string> CcRecipients { get; set; }
        public List<string> BccRecipients { get; set; }
        public bool IncludeCharts { get; set; }
        public bool IncludeKPIs { get; set; }
        public bool IncludeTrends { get; set; }
        public bool IncludePredictions { get; set; }
        public bool IncludeRawData { get; set; }
        public string Theme { get; set; }
        public string LogoUrl { get; set; }
        public Dictionary<string, object> CustomSettings { get; set; }
        public List<ReportSection> Sections { get; set; }
    }

    /// <summary>
    /// Report schedule configuration
    /// </summary>
    public class ReportSchedule
    {
        public Guid Id { get; set; }
        public Guid TemplateId { get; set; }
        public ScheduleFrequency Frequency { get; set; }
        public int? DayOfMonth { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public int? Month { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? NextRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string TimeZone { get; set; }
        public Dictionary<string, object> RecurrencePattern { get; set; }
    }

    /// <summary>
    /// Generated report
    /// </summary>
    public class GeneratedReport
    {
        public Guid Id { get; set; }
        public Guid TemplateId { get; set; }
        public string Name { get; set; }
        public ReportType Type { get; set; }
        public ReportFormat Format { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ReportStatus Status { get; set; }
        public string FilePath { get; set; }
        public string FileUrl { get; set; }
        public long FileSize { get; set; }
        public object Data { get; set; }
        public string ErrorMessage { get; set; }
        public string GeneratedBy { get; set; }
        public List<string> Recipients { get; set; }
        public DateTime? SentAt { get; set; }
        public DeliveryStatus DeliveryStatus { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Report section definition
    /// </summary>
    public class ReportSection
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public object Data { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public List<ReportSection> SubSections { get; set; }
    }

    /// <summary>
    /// Report type information
    /// </summary>
    public class ReportTypeInfo
    {
        public ReportType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public List<string> AvailableFormats { get; set; }
        public List<string> RequiredData { get; set; }
        public bool RequiresBuildings { get; set; }
        public Dictionary<string, object> DefaultConfiguration { get; set; }
    }

    /// <summary>
    /// Report data structures
    /// </summary>
    public class EnergyReportData
    {
        public DateRange Period { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<EnergyConsumptionData> ConsumptionData { get; set; }
        public EnergyCostBreakdown CostBreakdown { get; set; }
        public EnergyEfficiencyMetrics EfficiencyMetrics { get; set; }
        public List<EnergyAnomaly> Anomalies { get; set; }
        public EnergyBenchmarking Benchmarking { get; set; }
    }

    public class EnvironmentalReportData
    {
        public DateRange Period { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<TemperatureData> TemperatureData { get; set; }
        public List<HumidityData> HumidityData { get; set; }
        public List<AirQualityData> AirQualityData { get; set; }
        public ComfortAnalytics ComfortAnalytics { get; set; }
        public EnvironmentalCompliance Compliance { get; set; }
    }

    public class MaintenanceReportData
    {
        public DateRange Period { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<MaintenanceWorkOrder> WorkOrders { get; set; }
        public MaintenanceMetrics Metrics { get; set; }
        public List<MaintenanceCost> Costs { get; set; }
        public PredictiveMaintenance PredictiveMaintenance { get; set; }
    }

    public class PerformanceReportData
    {
        public DateRange Period { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<EquipmentPerformance> EquipmentPerformance { get; set; }
        public SystemAvailability Availability { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; }
    }

    public class OccupancyReportData
    {
        public DateRange Period { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<OccupancyData> OccupancyData { get; set; }
        public SpaceUtilization Utilization { get; set; }
        public OccupancyPatterns Patterns { get; set; }
        public OccupancyForecast Forecast { get; set; }
    }

    public class CustomReportData
    {
        public DateRange Period { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ReportConfiguration Configuration { get; set; }
        public List<ReportSection> Sections { get; set; }
        public Dictionary<string, object> CustomData { get; set; }
    }

    /// <summary>
    /// Report execution log
    /// </summary>
    public class ReportExecutionLog
    {
        public Guid Id { get; set; }
        public Guid ScheduleId { get; set; }
        public Guid TemplateId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public ReportExecutionStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public Guid? GeneratedReportId { get; set; }
        public long ExecutionTimeMs { get; set; }
        public int RecordsProcessed { get; set; }
        public Dictionary<string, object> ExecutionMetrics { get; set; }
    }

    /// <summary>
    /// Report template builder configuration
    /// </summary>
    public class ReportTemplateBuilder
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ReportType Type { get; set; }
        public List<ReportSectionTemplate> AvailableSections { get; set; }
        public List<ReportFieldTemplate> AvailableFields { get; set; }
        public List<ReportFilterTemplate> AvailableFilters { get; set; }
        public Dictionary<string, object> DefaultSettings { get; set; }
    }

    /// <summary>
    /// Report section template
    /// </summary>
    public class ReportSectionTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public List<ReportFieldTemplate> Fields { get; set; }
        public Dictionary<string, object> DefaultConfiguration { get; set; }
    }

    /// <summary>
    /// Report field template
    /// </summary>
    public class ReportFieldTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string DataType { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public object DefaultValue { get; set; }
        public Dictionary<string, object> ValidationRules { get; set; }
        public List<string> AllowedValues { get; set; }
    }

    /// <summary>
    /// Report filter template
    /// </summary>
    public class ReportFilterTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public object DefaultValue { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public bool IsRequired { get; set; }
    }

    /// <summary>
    /// Report subscription
    /// </summary>
    public class ReportSubscription
    {
        public Guid Id { get; set; }
        public Guid TemplateId { get; set; }
        public Guid UserId { get; set; }
        public List<string> EmailAddresses { get; set; }
        public List<NotificationChannel> NotificationChannels { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Dictionary<string, object> Preferences { get; set; }
    }

    /// <summary>
    /// Notification channel for report delivery
    /// </summary>
    public class NotificationChannel
    {
        public string Type { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
    }

    /// <summary>
    /// Enums for report system
    /// </summary>
    public enum ReportType
    {
        Energy,
        Environmental,
        Maintenance,
        Performance,
        Occupancy,
        Custom
    }

    public enum ReportFormat
    {
        PDF,
        Excel,
        CSV,
        JSON,
        HTML
    }

    public enum ReportStatus
    {
        Pending,
        Generating,
        Completed,
        Failed,
        Cancelled
    }

    public enum DeliveryStatus
    {
        Pending,
        Sent,
        Delivered,
        Failed,
        Bounced
    }

    public enum ScheduleFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Yearly
    }

    public enum ReportExecutionStatus
    {
        Scheduled,
        Running,
        Completed,
        Failed,
        Cancelled
    }
}