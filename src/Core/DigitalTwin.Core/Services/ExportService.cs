using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;

namespace DigitalTwin.Core.Services
{
    /// <summary>
    /// Export service for PDF, Excel, and CSV generation
    /// </summary>
    public class ExportService : IExportService
    {
        public ExportService()
        {
            // Initialize export libraries and configurations
        }

        /// <summary>
        /// Exports data to PDF format
        /// </summary>
        public async Task<ExportResult> ExportToPDFAsync(ExportRequest request)
        {
            try
            {
                var pdfDocument = await GeneratePDFDocumentAsync(request);
                var filePath = await SavePDFDocumentAsync(pdfDocument, request.FileName);
                
                return new ExportResult
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = request.FileName,
                    FileSize = new FileInfo(filePath).Length,
                    Format = ReportFormat.PDF,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Format = ReportFormat.PDF,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Exports data to Excel format
        /// </summary>
        public async Task<ExportResult> ExportToExcelAsync(ExportRequest request)
        {
            try
            {
                var excelWorkbook = await GenerateExcelWorkbookAsync(request);
                var filePath = await SaveExcelWorkbookAsync(excelWorkbook, request.FileName);
                
                return new ExportResult
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = request.FileName,
                    FileSize = new FileInfo(filePath).Length,
                    Format = ReportFormat.Excel,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Format = ReportFormat.Excel,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Exports data to CSV format
        /// </summary>
        public async Task<ExportResult> ExportToCSVAsync(ExportRequest request)
        {
            try
            {
                var csvContent = await GenerateCSVContentAsync(request);
                var filePath = await SaveCSVFileAsync(csvContent, request.FileName);
                
                return new ExportResult
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = request.FileName,
                    FileSize = new FileInfo(filePath).Length,
                    Format = ReportFormat.CSV,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Format = ReportFormat.CSV,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Exports data to JSON format
        /// </summary>
        public async Task<ExportResult> ExportToJSONAsync(ExportRequest request)
        {
            try
            {
                var jsonContent = await GenerateJSONContentAsync(request);
                var filePath = await SaveJSONFileAsync(jsonContent, request.FileName);
                
                return new ExportResult
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = request.FileName,
                    FileSize = new FileInfo(filePath).Length,
                    Format = ReportFormat.JSON,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Format = ReportFormat.JSON,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Exports data to HTML format
        /// </summary>
        public async Task<ExportResult> ExportToHTMLAsync(ExportRequest request)
        {
            try
            {
                var htmlContent = await GenerateHTMLContentAsync(request);
                var filePath = await SaveHTMLFileAsync(htmlContent, request.FileName);
                
                return new ExportResult
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = request.FileName,
                    FileSize = new FileInfo(filePath).Length,
                    Format = ReportFormat.HTML,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Format = ReportFormat.HTML,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Gets supported export formats
        /// </summary>
        public async Task<List<ExportFormatInfo>> GetSupportedFormatsAsync()
        {
            return await Task.FromResult(new List<ExportFormatInfo>
            {
                new ExportFormatInfo
                {
                    Format = ReportFormat.PDF,
                    Name = "PDF Document",
                    Description = "Portable Document Format for reports and documents",
                    Extension = ".pdf",
                    MimeType = "application/pdf",
                    MaxSize = 50 * 1024 * 1024, // 50MB
                    SupportedDataTypes = new List<string> { "report", "dashboard", "analytics", "charts" }
                },
                new ExportFormatInfo
                {
                    Format = ReportFormat.Excel,
                    Name = "Excel Spreadsheet",
                    Description = "Microsoft Excel format for data analysis",
                    Extension = ".xlsx",
                    MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    MaxSize = 100 * 1024 * 1024, // 100MB
                    SupportedDataTypes = new List<string> { "data", "analytics", "tables", "metrics" }
                },
                new ExportFormatInfo
                {
                    Format = ReportFormat.CSV,
                    Name = "CSV File",
                    Description = "Comma-separated values for data import",
                    Extension = ".csv",
                    MimeType = "text/csv",
                    MaxSize = 20 * 1024 * 1024, // 20MB
                    SupportedDataTypes = new List<string> { "data", "tables", "metrics" }
                },
                new ExportFormatInfo
                {
                    Format = ReportFormat.JSON,
                    Name = "JSON Data",
                    Description = "JavaScript Object Notation for API integration",
                    Extension = ".json",
                    MimeType = "application/json",
                    MaxSize = 10 * 1024 * 1024, // 10MB
                    SupportedDataTypes = new List<string> { "data", "analytics", "configuration" }
                },
                new ExportFormatInfo
                {
                    Format = ReportFormat.HTML,
                    Name = "HTML Page",
                    Description = "HTML format for web viewing",
                    Extension = ".html",
                    MimeType = "text/html",
                    MaxSize = 30 * 1024 * 1024, // 30MB
                    SupportedDataTypes = new List<string> { "report", "dashboard", "analytics" }
                }
            });
        }

        /// <summary>
        /// Exports multiple formats in parallel
        /// </summary>
        public async Task<List<ExportResult>> ExportToMultipleFormatsAsync(MultiExportRequest request)
        {
            var tasks = new List<Task<ExportResult>>();
            
            foreach (var format in request.Formats)
            {
                var formatRequest = new ExportRequest
                {
                    Data = request.Data,
                    FileName = $"{Path.GetFileNameWithoutExtension(request.FileName)}_{format.ToString().ToLower()}",
                    Format = format,
                    Options = request.Options,
                    Template = request.Template
                };
                
                tasks.Add(format switch
                {
                    ReportFormat.PDF => ExportToPDFAsync(formatRequest),
                    ReportFormat.Excel => ExportToExcelAsync(formatRequest),
                    ReportFormat.CSV => ExportToCSVAsync(formatRequest),
                    ReportFormat.JSON => ExportToJSONAsync(formatRequest),
                    ReportFormat.HTML => ExportToHTMLAsync(formatRequest),
                    _ => Task.FromResult(new ExportResult { Success = false, ErrorMessage = $"Unsupported format: {format}" })
                });
            }
            
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        /// <summary>
        /// Gets export history
        /// </summary>
        public async Task<List<ExportHistory>> GetExportHistoryAsync(int page = 1, int pageSize = 20)
        {
            // Mock data - in real implementation, this would query database
            var history = new List<ExportHistory>();
            
            for (int i = 0; i < pageSize; i++)
            {
                history.Add(new ExportHistory
                {
                    Id = Guid.NewGuid(),
                    FileName = $"export_{i + 1}.pdf",
                    Format = ReportFormat.PDF,
                    GeneratedAt = DateTime.UtcNow.AddHours(-i * 2),
                    FileSize = 1024 * (i + 1) * 500,
                    GeneratedBy = "user@example.com",
                    Status = ExportStatus.Completed,
                    DownloadCount = i % 5,
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                });
            }
            
            return await Task.FromResult(history);
        }

        /// <summary>
        /// Validates export request
        /// </summary>
        public async Task<ExportValidationResult> ValidateExportRequestAsync(ExportRequest request)
        {
            var result = new ExportValidationResult { IsValid = true };
            
            // Validate file name
            if (string.IsNullOrEmpty(request.FileName))
            {
                result.Errors.Add(new ValidationError { Property = "FileName", Message = "File name is required" });
                result.IsValid = false;
            }
            
            // Validate data
            if (request.Data == null)
            {
                result.Errors.Add(new ValidationError { Property = "Data", Message = "Data is required" });
                result.IsValid = false;
            }
            
            // Validate format
            var supportedFormats = await GetSupportedFormatsAsync();
            if (!supportedFormats.Any(f => f.Format == request.Format))
            {
                result.Errors.Add(new ValidationError { Property = "Format", Message = "Unsupported export format" });
                result.IsValid = false;
            }
            
            // Check size limits
            var formatInfo = supportedFormats.FirstOrDefault(f => f.Format == request.Format);
            if (formatInfo != null && request.EstimatedSize > formatInfo.MaxSize)
            {
                result.Errors.Add(new ValidationError 
                { 
                    Property = "Data", 
                    Message = $"Data size exceeds maximum allowed size of {formatInfo.MaxSize / (1024 * 1024)}MB" 
                });
                result.IsValid = false;
            }
            
            return result;
        }

        private async Task<byte[]> GeneratePDFDocumentAsync(ExportRequest request)
        {
            // In a real implementation, this would use a PDF library like iTextSharp or PdfSharp
            // For now, we'll create a mock PDF content
            var pdfContent = $"PDF Export - {request.FileName}\n\n";
            
            if (request.Data is EnergyReportData energyData)
            {
                pdfContent += await GenerateEnergyPDFContentAsync(energyData);
            }
            else if (request.Data is BuildingKPIs kpis)
            {
                pdfContent += await GenerateKPIsPDFContentAsync(kpis);
            }
            else
            {
                pdfContent += "Generic PDF content";
            }
            
            return System.Text.Encoding.UTF8.GetBytes(pdfContent);
        }

        private async Task<byte[]> GenerateExcelWorkbookAsync(ExportRequest request)
        {
            // In a real implementation, this would use an Excel library like EPPlus or ClosedXML
            // For now, we'll create a mock Excel content
            var excelContent = "Excel Export\n";
            
            if (request.Data is List<MetricTrend> trends)
            {
                excelContent += await GenerateTrendsExcelContentAsync(trends);
            }
            else if (request.Data is BuildingKPIs kpis)
            {
                excelContent += await GenerateKPIsExcelContentAsync(kpis);
            }
            else
            {
                excelContent += "Generic Excel content";
            }
            
            return System.Text.Encoding.UTF8.GetBytes(excelContent);
        }

        private async Task<string> GenerateCSVContentAsync(ExportRequest request)
        {
            var csvContent = "";
            
            if (request.Data is List<MetricTrend> trends)
            {
                csvContent += await GenerateTrendsCSVContentAsync(trends);
            }
            else if (request.Data is EnergyReportData energyData)
            {
                csvContent += await GenerateEnergyCSVContentAsync(energyData);
            }
            else
            {
                csvContent += "Generic CSV content";
            }
            
            return csvContent;
        }

        private async Task<string> GenerateJSONContentAsync(ExportRequest request)
        {
            // In a real implementation, this would use a JSON serializer with proper formatting
            return System.Text.Json.JsonSerializer.Serialize(request.Data, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        private async Task<string> GenerateHTMLContentAsync(ExportRequest request)
        {
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{request.FileName}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        table {{ border-collapse: collapse; width: 100%; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        .kpi {{ margin: 10px 0; padding: 10px; border: 1px solid #ccc; }}
    </style>
</head>
<body>
    <h1>{request.FileName}</h1>
    <p>Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</p>
";
            
            if (request.Data is BuildingKPIs kpis)
            {
                htmlContent += await GenerateKPIsHTMLContentAsync(kpis);
            }
            else if (request.Data is List<MetricTrend> trends)
            {
                htmlContent += await GenerateTrendsHTMLContentAsync(trends);
            }
            
            htmlContent += @"
</body>
</html>";
            
            return htmlContent;
        }

        private async Task<string> SavePDFDocumentAsync(byte[] pdfData, string fileName)
        {
            var filePath = Path.Combine("/exports", fileName);
            await File.WriteAllBytesAsync(filePath, pdfData);
            return filePath;
        }

        private async Task<string> SaveExcelWorkbookAsync(byte[] excelData, string fileName)
        {
            var filePath = Path.Combine("/exports", fileName);
            await File.WriteAllBytesAsync(filePath, excelData);
            return filePath;
        }

        private async Task<string> SaveCSVFileAsync(string csvContent, string fileName)
        {
            var filePath = Path.Combine("/exports", fileName);
            await File.WriteAllTextAsync(filePath, csvContent);
            return filePath;
        }

        private async Task<string> SaveJSONFileAsync(string jsonContent, string fileName)
        {
            var filePath = Path.Combine("/exports", fileName);
            await File.WriteAllTextAsync(filePath, jsonContent);
            return filePath;
        }

        private async Task<string> SaveHTMLFileAsync(string htmlContent, string fileName)
        {
            var filePath = Path.Combine("/exports", fileName);
            await File.WriteAllTextAsync(filePath, htmlContent);
            return filePath;
        }

        // Helper methods for generating specific content types
        private async Task<string> GenerateEnergyPDFContentAsync(EnergyReportData data)
        {
            var content = $"Energy Report for {data.Period.Start:yyyy-MM-dd} to {data.Period.End:yyyy-MM-dd}\n\n";
            content += $"Total Energy Consumption: {data.ConsumptionData?.Sum(c => c.Consumption) ?? 0} kWh\n";
            content += $"Total Cost: ${data.CostBreakdown?.TotalCost ?? 0}\n";
            return await Task.FromResult(content);
        }

        private async Task<string> GenerateKPIsPDFContentAsync(BuildingKPIs data)
        {
            var content = $"Building KPIs - {data.BuildingName}\n\n";
            content += $"Energy Consumption: {data.EnergyKPIs.TotalConsumption} kWh\n";
            content += $"Average Temperature: {data.EnvironmentalKPIs.AverageTemperature}°C\n";
            content += $"Occupancy Rate: {data.OccupancyKPIs.OccupancyRate:P}\n";
            return await Task.FromResult(content);
        }

        private async Task<string> GenerateTrendsExcelContentAsync(List<MetricTrend> trends)
        {
            var content = "Timestamp,Metric Type,Value,Unit\n";
            foreach (var trend in trends)
            {
                content += $"{trend.Timestamp:yyyy-MM-dd HH:mm:ss},{trend.MetricType},{trend.Value},{trend.Unit}\n";
            }
            return await Task.FromResult(content);
        }

        private async Task<string> GenerateKPIsExcelContentAsync(BuildingKPIs data)
        {
            var content = "KPI,Value,Unit\n";
            content += $"Total Energy Consumption,{data.EnergyKPIs.TotalConsumption},kWh\n";
            content += $"Average Temperature,{data.EnvironmentalKPIs.AverageTemperature},°C\n";
            content += $"Occupancy Rate,{data.OccupancyKPIs.OccupancyRate},%\n";
            return await Task.FromResult(content);
        }

        private async Task<string> GenerateTrendsCSVContentAsync(List<MetricTrend> trends)
        {
            var content = "Timestamp,Metric Type,Value,Unit,Change Percentage,Moving Average\n";
            foreach (var trend in trends)
            {
                content += $"{trend.Timestamp:yyyy-MM-dd HH:mm:ss},{trend.MetricType},{trend.Value},{trend.Unit},{trend.ChangePercentage},{trend.MovingAverage}\n";
            }
            return await Task.FromResult(content);
        }

        private async Task<string> GenerateEnergyCSVContentAsync(EnergyReportData data)
        {
            var content = "Timestamp,Consumption (kWh),Cost ($),Source,Peak Demand (kW)\n";
            if (data.ConsumptionData != null)
            {
                foreach (var consumption in data.ConsumptionData)
                {
                    content += $"{consumption.Timestamp:yyyy-MM-dd HH:mm:ss},{consumption.Consumption},{consumption.Cost},{consumption.Source},{consumption.PeakDemand}\n";
                }
            }
            return await Task.FromResult(content);
        }

        private async Task<string> GenerateKPIsHTMLContentAsync(BuildingKPIs data)
        {
            var html = $@"
    <div class='kpi'>
        <h3>Energy KPIs</h3>
        <p>Total Consumption: {data.EnergyKPIs.TotalConsumption} kWh</p>
        <p>Efficiency Score: {data.EnergyKPIs.EfficiencyScore}/100</p>
        <p>Total Cost: ${data.EnergyKPIs.Cost}</p>
    </div>
    <div class='kpi'>
        <h3>Environmental KPIs</h3>
        <p>Average Temperature: {data.EnvironmentalKPIs.AverageTemperature}°C</p>
        <p>Comfort Score: {data.EnvironmentalKPIs.ComfortScore}/100</p>
        <p>Air Quality Index: {data.EnvironmentalKPIs.AirQualityIndex}</p>
    </div>
    <div class='kpi'>
        <h3>Occupancy KPIs</h3>
        <p>Occupancy Rate: {data.OccupancyKPIs.OccupancyRate:P}</p>
        <p>Peak Occupancy: {data.OccupancyKPIs.PeakOccupancy}</p>
        <p>Space Utilization: {data.OccupancyKPIs.SpaceUtilization:P}</p>
    </div>";
            
            return await Task.FromResult(html);
        }

        private async Task<string> GenerateTrendsHTMLContentAsync(List<MetricTrend> trends)
        {
            var html = "<table><thead><tr><th>Timestamp</th><th>Metric Type</th><th>Value</th><th>Unit</th><th>Change</th></tr></thead><tbody>";
            
            foreach (var trend in trends)
            {
                var changeClass = trend.ChangePercentage.HasValue && trend.ChangePercentage.Value > 0 ? "positive" : "negative";
                var changeSymbol = trend.ChangePercentage.HasValue && trend.ChangePercentage.Value > 0 ? "↑" : "↓";
                
                html += $"<tr><td>{trend.Timestamp:yyyy-MM-dd HH:mm:ss}</td><td>{trend.MetricType}</td><td>{trend.Value}</td><td>{trend.Unit}</td><td class='{changeClass}'>{changeSymbol} {trend.ChangePercentage:P}</td></tr>";
            }
            
            html += "</tbody></table>";
            return await Task.FromResult(html);
        }
    }
}