using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Services
{
    /// <summary>
    /// Report builder and scheduling service
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IBuildingRepository _buildingRepository;
        private readonly ISensorRepository _sensorRepository;
        private readonly IAnalyticsService _analyticsService;
        private readonly IDataCollectionService _dataCollectionService;

        public ReportService(
            IBuildingRepository buildingRepository,
            ISensorRepository sensorRepository,
            IAnalyticsService analyticsService,
            IDataCollectionService dataCollectionService)
        {
            _buildingRepository = buildingRepository;
            _sensorRepository = sensorRepository;
            _analyticsService = analyticsService;
            _dataCollectionService = dataCollectionService;
        }

        /// <summary>
        /// Creates a new report template
        /// </summary>
        public async Task<ReportTemplate> CreateReportTemplateAsync(ReportTemplate template)
        {
            template.Id = Guid.NewGuid();
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            
            // Validate template configuration
            ValidateReportTemplate(template);
            
            // In a real implementation, this would save to database
            return await Task.FromResult(template);
        }

        /// <summary>
        /// Gets all report templates
        /// </summary>
        public async Task<List<ReportTemplate>> GetReportTemplatesAsync()
        {
            // Mock data - in real implementation, this would query database
            return await Task.FromResult(new List<ReportTemplate>
            {
                new ReportTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Energy Consumption Report",
                    Description = "Monthly energy consumption analysis with trends",
                    Type = ReportType.Energy,
                    Schedule = new ReportSchedule
                    {
                        Frequency = ScheduleFrequency.Monthly,
                        DayOfMonth = 1,
                        Hour = 8,
                        Minute = 0
                    },
                    Configuration = new ReportConfiguration
                    {
                        IncludeCharts = true,
                        IncludeKPIs = true,
                        IncludeTrends = true,
                        IncludePredictions = false,
                        Format = ReportFormat.PDF,
                        Recipients = new List<string> { "manager@company.com" }
                    },
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new ReportTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Building Performance Summary",
                    Description = "Weekly performance summary with key metrics",
                    Type = ReportType.Performance,
                    Schedule = new ReportSchedule
                    {
                        Frequency = ScheduleFrequency.Weekly,
                        DayOfWeek = DayOfWeek.Monday,
                        Hour = 9,
                        Minute = 0
                    },
                    Configuration = new ReportConfiguration
                    {
                        IncludeCharts = true,
                        IncludeKPIs = true,
                        IncludeTrends = true,
                        IncludePredictions = true,
                        Format = ReportFormat.Excel,
                        Recipients = new List<string> { "ops@company.com", "facilities@company.com" }
                    },
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                }
            });
        }

        /// <summary>
        /// Gets a specific report template
        /// </summary>
        public async Task<ReportTemplate> GetReportTemplateAsync(Guid templateId)
        {
            var templates = await GetReportTemplatesAsync();
            return templates.FirstOrDefault(t => t.Id == templateId);
        }

        /// <summary>
        /// Updates a report template
        /// </summary>
        public async Task<ReportTemplate> UpdateReportTemplateAsync(ReportTemplate template)
        {
            template.UpdatedAt = DateTime.UtcNow;
            ValidateReportTemplate(template);
            
            // In a real implementation, this would update database
            return await Task.FromResult(template);
        }

        /// <summary>
        /// Deletes a report template
        /// </summary>
        public async Task<bool> DeleteReportTemplateAsync(Guid templateId)
        {
            // In a real implementation, this would delete from database
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Generates a report based on template
        /// </summary>
        public async Task<GeneratedReport> GenerateReportAsync(Guid templateId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var template = await GetReportTemplateAsync(templateId);
            if (template == null)
                throw new ArgumentException($"Report template with ID {templateId} not found");

            var report = new GeneratedReport
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                Name = template.Name,
                Type = template.Type,
                Format = template.Configuration.Format,
                GeneratedAt = DateTime.UtcNow,
                Status = ReportStatus.Generating
            };

            try
            {
                // Generate report data based on type
                report.Data = await GenerateReportDataAsync(template, startDate, endDate);
                report.Status = ReportStatus.Completed;
                report.FilePath = await SaveReportToFileAsync(report);
                report.FileSize = CalculateFileSize(report.Data);
            }
            catch (Exception ex)
            {
                report.Status = ReportStatus.Failed;
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// Gets generated reports
        /// </summary>
        public async Task<List<GeneratedReport>> GetGeneratedReportsAsync(int page = 1, int pageSize = 20)
        {
            // Mock data - in real implementation, this would query database with pagination
            var reports = new List<GeneratedReport>();
            
            for (int i = 0; i < pageSize; i++)
            {
                reports.Add(new GeneratedReport
                {
                    Id = Guid.NewGuid(),
                    Name = $"Report {i + 1}",
                    Type = (ReportType)((i % 4) + 1),
                    Format = (ReportFormat)((i % 3) + 1),
                    GeneratedAt = DateTime.UtcNow.AddHours(-i * 2),
                    Status = ReportStatus.Completed,
                    FileSize = 1024 * (i + 1) * 100
                });
            }

            return await Task.FromResult(reports);
        }

        /// <summary>
        /// Gets a specific generated report
        /// </summary>
        public async Task<GeneratedReport> GetGeneratedReportAsync(Guid reportId)
        {
            var reports = await GetGeneratedReportsAsync();
            return reports.FirstOrDefault(r => r.Id == reportId);
        }

        /// <summary>
        /// Schedules a report
        /// </summary>
        public async Task<ReportSchedule> ScheduleReportAsync(Guid templateId, ReportSchedule schedule)
        {
            schedule.Id = Guid.NewGuid();
            schedule.TemplateId = templateId;
            schedule.IsActive = true;
            schedule.CreatedAt = DateTime.UtcNow;
            schedule.NextRunTime = CalculateNextRunTime(schedule);

            // In a real implementation, this would save to database and set up scheduler
            return await Task.FromResult(schedule);
        }

        /// <summary>
        /// Gets scheduled reports
        /// </summary>
        public async Task<List<ReportSchedule>> GetScheduledReportsAsync()
        {
            // Mock data - in real implementation, this would query database
            return await Task.FromResult(new List<ReportSchedule>
            {
                new ReportSchedule
                {
                    Id = Guid.NewGuid(),
                    TemplateId = Guid.NewGuid(),
                    Frequency = ScheduleFrequency.Daily,
                    Hour = 8,
                    Minute = 0,
                    IsActive = true,
                    NextRunTime = DateTime.Today.AddHours(8),
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new ReportSchedule
                {
                    Id = Guid.NewGuid(),
                    TemplateId = Guid.NewGuid(),
                    Frequency = ScheduleFrequency.Weekly,
                    DayOfWeek = DayOfWeek.Friday,
                    Hour = 17,
                    Minute = 0,
                    IsActive = true,
                    NextRunTime = GetNextFriday(),
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                }
            });
        }

        /// <summary>
        /// Updates a report schedule
        /// </summary>
        public async Task<ReportSchedule> UpdateReportScheduleAsync(ReportSchedule schedule)
        {
            schedule.NextRunTime = CalculateNextRunTime(schedule);
            
            // In a real implementation, this would update database and reschedule
            return await Task.FromResult(schedule);
        }

        /// <summary>
        /// Deletes a report schedule
        /// </summary>
        public async Task<bool> DeleteReportScheduleAsync(Guid scheduleId)
        {
            // In a real implementation, this would delete from database and cancel scheduled job
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Gets available report types
        /// </summary>
        public async Task<List<ReportTypeInfo>> GetReportTypesAsync()
        {
            return await Task.FromResult(new List<ReportTypeInfo>
            {
                new ReportTypeInfo
                {
                    Type = ReportType.Energy,
                    Name = "Energy Report",
                    Description = "Energy consumption analysis and efficiency metrics",
                    Icon = "energy"
                },
                new ReportTypeInfo
                {
                    Type = ReportType.Environmental,
                    Name = "Environmental Report",
                    Description = "Temperature, humidity, and air quality metrics",
                    Icon = "environment"
                },
                new ReportTypeInfo
                {
                    Type = ReportType.Maintenance,
                    Name = "Maintenance Report",
                    Description = "Equipment maintenance and performance metrics",
                    Icon = "maintenance"
                },
                new ReportTypeInfo
                {
                    Type = ReportType.Performance,
                    Name = "Performance Report",
                    Description = "Overall building performance and KPIs",
                    Icon = "performance"
                },
                new ReportTypeInfo
                {
                    Type = ReportType.Occupancy,
                    Name = "Occupancy Report",
                    Description = "Space utilization and occupancy analysis",
                    Icon = "occupancy"
                },
                new ReportTypeInfo
                {
                    Type = ReportType.Custom,
                    Name = "Custom Report",
                    Description = "Custom report with user-defined metrics",
                    Icon = "custom"
                }
            });
        }

        private void ValidateReportTemplate(ReportTemplate template)
        {
            if (string.IsNullOrEmpty(template.Name))
                throw new ArgumentException("Report template name is required");

            if (template.Configuration == null)
                throw new ArgumentException("Report configuration is required");

            if (template.Configuration.Recipients == null || !template.Configuration.Recipients.Any())
                throw new ArgumentException("At least one recipient is required");

            // Validate email addresses
            foreach (var recipient in template.Configuration.Recipients)
            {
                if (!IsValidEmail(recipient))
                    throw new ArgumentException($"Invalid email address: {recipient}");
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private async Task<object> GenerateReportDataAsync(ReportTemplate template, DateTime? startDate, DateTime? endDate)
        {
            var dateRange = startDate.HasValue && endDate.HasValue
                ? new DateRange(startDate.Value, endDate.Value)
                : new DateRange(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

            switch (template.Type)
            {
                case ReportType.Energy:
                    return await GenerateEnergyReportDataAsync(dateRange);
                case ReportType.Environmental:
                    return await GenerateEnvironmentalReportDataAsync(dateRange);
                case ReportType.Maintenance:
                    return await GenerateMaintenanceReportDataAsync(dateRange);
                case ReportType.Performance:
                    return await GeneratePerformanceReportDataAsync(dateRange);
                case ReportType.Occupancy:
                    return await GenerateOccupancyReportDataAsync(dateRange);
                case ReportType.Custom:
                    return await GenerateCustomReportDataAsync(template, dateRange);
                default:
                    throw new ArgumentException($"Unsupported report type: {template.Type}");
            }
        }

        private async Task<EnergyReportData> GenerateEnergyReportDataAsync(DateRange dateRange)
        {
            // Get energy analytics
            var energyAnalytics = new EnergyReportData
            {
                Period = dateRange,
                GeneratedAt = DateTime.UtcNow
            };

            // Mock energy consumption data
            energyAnalytics.ConsumptionData = new List<EnergyConsumptionData>
            {
                new EnergyConsumptionData
                {
                    Timestamp = dateRange.Start,
                    Consumption = 1200.5,
                    Cost = 144.06,
                    Source = "HVAC"
                },
                new EnergyConsumptionData
                {
                    Timestamp = dateRange.Start.AddDays(7),
                    Consumption = 1150.2,
                    Cost = 138.02,
                    Source = "HVAC"
                }
            };

            energyAnalytics.CostBreakdown = new EnergyCostBreakdown
            {
                TotalCost = 282.08,
                EnergyCost = 250.00,
                DemandCost = 32.08,
                CostBySource = new Dictionary<string, double>
                {
                    { "HVAC", 150.00 },
                    { "Lighting", 80.00 },
                    { "Equipment", 52.08 }
                }
            };

            return await Task.FromResult(energyAnalytics);
        }

        private async Task<EnvironmentalReportData> GenerateEnvironmentalReportDataAsync(DateRange dateRange)
        {
            var environmentalData = new EnvironmentalReportData
            {
                Period = dateRange,
                GeneratedAt = DateTime.UtcNow
            };

            environmentalData.TemperatureData = new List<TemperatureData>
            {
                new TemperatureData
                {
                    Timestamp = dateRange.Start,
                    Temperature = 22.5,
                    Zone = "Office Floor 1",
                    SetPoint = 22.0
                }
            };

            environmentalData.AirQualityData = new List<AirQualityData>
            {
                new AirQualityData
                {
                    Timestamp = dateRange.Start,
                    CO2 = 450,
                    AirQualityIndex = 35,
                    Zone = "Office Floor 1"
                }
            };

            return await Task.FromResult(environmentalData);
        }

        private async Task<MaintenanceReportData> GenerateMaintenanceReportDataAsync(DateRange dateRange)
        {
            var maintenanceData = new MaintenanceReportData
            {
                Period = dateRange,
                GeneratedAt = DateTime.UtcNow
            };

            maintenanceData.WorkOrders = new List<MaintenanceWorkOrder>
            {
                new MaintenanceWorkOrder
                {
                    Id = "WO-001",
                    CreatedAt = dateRange.Start,
                    Type = "Preventive",
                    Priority = "Medium",
                    Equipment = "HVAC Unit 1",
                    Description = "Quarterly maintenance check",
                    Status = "Completed"
                }
            };

            return await Task.FromResult(maintenanceData);
        }

        private async Task<PerformanceReportData> GeneratePerformanceReportDataAsync(DateRange dateRange)
        {
            var performanceData = new PerformanceReportData
            {
                Period = dateRange,
                GeneratedAt = DateTime.UtcNow
            };

            performanceData.EquipmentPerformance = new List<EquipmentPerformance>
            {
                new EquipmentPerformance
                {
                    EquipmentId = "EQ-001",
                    EquipmentName = "HVAC System",
                    Type = "HVAC",
                    Uptime = 98.5,
                    Efficiency = 85.2
                }
            };

            return await Task.FromResult(performanceData);
        }

        private async Task<OccupancyReportData> GenerateOccupancyReportDataAsync(DateRange dateRange)
        {
            var occupancyData = new OccupancyReportData
            {
                Period = dateRange,
                GeneratedAt = DateTime.UtcNow
            };

            occupancyData.OccupancyData = new List<OccupancyData>
            {
                new OccupancyData
                {
                    Timestamp = dateRange.Start,
                    OccupancyCount = 150,
                    OccupancyRate = 75.0,
                    Zone = "Office Floor 1"
                }
            };

            return await Task.FromResult(occupancyData);
        }

        private async Task<CustomReportData> GenerateCustomReportDataAsync(ReportTemplate template, DateRange dateRange)
        {
            var customData = new CustomReportData
            {
                Period = dateRange,
                GeneratedAt = DateTime.UtcNow,
                Configuration = template.Configuration
            };

            // Generate custom data based on template configuration
            // This is a simplified implementation
            customData.Sections = new List<ReportSection>
            {
                new ReportSection
                {
                    Title = "Executive Summary",
                    Type = "Summary",
                    Data = new { Summary = "Custom report summary" }
                }
            };

            return await Task.FromResult(customData);
        }

        private async Task<string> SaveReportToFileAsync(GeneratedReport report)
        {
            var fileName = $"{report.Name}_{report.GeneratedAt:yyyyMMdd_HHmmss}.{report.Format.ToString().ToLower()}";
            var filePath = $"/reports/{fileName}";

            // In a real implementation, this would:
            // 1. Generate PDF/Excel/CSV file based on format
            // 2. Save to file storage or cloud storage
            // 3. Return the file path or URL

            return await Task.FromResult(filePath);
        }

        private long CalculateFileSize(object data)
        {
            // Simplified file size calculation
            return 1024 * 1024; // 1MB mock size
        }

        private DateTime CalculateNextRunTime(ReportSchedule schedule)
        {
            var now = DateTime.UtcNow;
            
            switch (schedule.Frequency)
            {
                case ScheduleFrequency.Daily:
                    return DateTime.UtcNow.Date.AddDays(1).AddHours(schedule.Hour).AddMinutes(schedule.Minute);
                
                case ScheduleFrequency.Weekly:
                    var nextWeek = now.AddDays((int)schedule.DayOfWeek - (int)now.DayOfWeek + 7) % 7);
                    return nextWeek.Date.AddHours(schedule.Hour).AddMinutes(schedule.Minute);
                
                case ScheduleFrequency.Monthly:
                    var nextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                    var dayInMonth = Math.Min(schedule.DayOfMonth, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                    return new DateTime(nextMonth.Year, nextMonth.Month, dayInMonth)
                        .AddHours(schedule.Hour).AddMinutes(schedule.Minute);
                
                case ScheduleFrequency.Quarterly:
                    var nextQuarter = new DateTime(now.Year, ((now.Month - 1) / 3 + 1) * 3 + 1, 1);
                    var dayInQuarter = Math.Min(schedule.DayOfMonth, DateTime.DaysInMonth(nextQuarter.Year, nextQuarter.Month));
                    return new DateTime(nextQuarter.Year, nextQuarter.Month, dayInQuarter)
                        .AddHours(schedule.Hour).AddMinutes(schedule.Minute);
                
                case ScheduleFrequency.Yearly:
                    var nextYear = new DateTime(now.Year + 1, 1, 1);
                    var dayInYear = Math.Min(schedule.DayOfMonth, DateTime.DaysInMonth(nextYear.Year, schedule.Month ?? 1));
                    return new DateTime(nextYear.Year, schedule.Month ?? 1, dayInYear)
                        .AddHours(schedule.Hour).AddMinutes(schedule.Minute);
                
                default:
                    return now.AddDays(1);
            }
        }

        private DateTime GetNextFriday()
        {
            var now = DateTime.UtcNow;
            var daysUntilFriday = ((int)DayOfWeek.Friday - (int)now.DayOfWeek + 7) % 7;
            return now.AddDays(daysUntilFriday).Date.AddHours(17);
        }
    }
}