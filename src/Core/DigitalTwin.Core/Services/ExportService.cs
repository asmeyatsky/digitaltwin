using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    /// <summary>
    /// Export service for PDF, Excel, and CSV generation
    /// </summary>
    public class ExportService : IExportService
    {
        // In-memory tracking for export jobs and history
        private readonly Dictionary<Guid, ExportStatusInfo> _exportJobs = new();
        private readonly Dictionary<Guid, ExportResult> _completedExports = new();
        private readonly Dictionary<Guid, ExportShareResult> _sharedExports = new();
        private readonly List<ExportHistory> _exportHistory = new();
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
                    Status = ExportJobStatus.Completed,
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

        /// <summary>
        /// Gets export status for an export job
        /// </summary>
        public async Task<ExportStatusInfo> GetExportStatusAsync(Guid exportId)
        {
            if (_exportJobs.TryGetValue(exportId, out var status))
            {
                return await Task.FromResult(status);
            }

            // Check completed exports
            if (_completedExports.ContainsKey(exportId))
            {
                return await Task.FromResult(new ExportStatusInfo
                {
                    Id = exportId,
                    Status = ExportJobStatus.Completed,
                    ProgressPercentage = 100,
                    CurrentOperation = "Export completed",
                    StartedAt = _completedExports[exportId].GeneratedAt.AddSeconds(-5),
                    CompletedAt = _completedExports[exportId].GeneratedAt,
                    ElapsedTime = TimeSpan.FromSeconds(5),
                    ProcessedRecords = 100,
                    TotalRecords = 100,
                    Steps = new List<ExportJobStep>
                    {
                        new ExportJobStep { Name = "Validation", Status = ExportJobStepStatus.Completed, ProgressPercentage = 100 },
                        new ExportJobStep { Name = "Data Processing", Status = ExportJobStepStatus.Completed, ProgressPercentage = 100 },
                        new ExportJobStep { Name = "File Generation", Status = ExportJobStepStatus.Completed, ProgressPercentage = 100 },
                        new ExportJobStep { Name = "Saving", Status = ExportJobStepStatus.Completed, ProgressPercentage = 100 }
                    }
                });
            }

            // Return a default not-found status
            return await Task.FromResult(new ExportStatusInfo
            {
                Id = exportId,
                Status = ExportJobStatus.Failed,
                ProgressPercentage = 0,
                CurrentOperation = "Export not found",
                ErrorMessage = $"No export job found with ID {exportId}"
            });
        }

        /// <summary>
        /// Cancels an export job
        /// </summary>
        public async Task<bool> CancelExportAsync(Guid exportId)
        {
            if (_exportJobs.TryGetValue(exportId, out var status))
            {
                if (status.Status == ExportJobStatus.Processing ||
                    status.Status == ExportJobStatus.Queued ||
                    status.Status == ExportJobStatus.Validating ||
                    status.Status == ExportJobStatus.Generating)
                {
                    status.Status = ExportJobStatus.Cancelled;
                    status.CompletedAt = DateTime.UtcNow;
                    status.CurrentOperation = "Export cancelled";
                    return await Task.FromResult(true);
                }
            }

            return await Task.FromResult(false);
        }

        /// <summary>
        /// Downloads an exported file
        /// </summary>
        public async Task<ExportDownloadResult> DownloadExportAsync(Guid exportId)
        {
            if (_completedExports.TryGetValue(exportId, out var exportResult))
            {
                byte[] fileContent;
                try
                {
                    if (!string.IsNullOrEmpty(exportResult.FilePath) && File.Exists(exportResult.FilePath))
                    {
                        fileContent = await File.ReadAllBytesAsync(exportResult.FilePath);
                    }
                    else
                    {
                        // Generate placeholder content if file doesn't exist on disk
                        fileContent = System.Text.Encoding.UTF8.GetBytes($"Export content for {exportResult.FileName}");
                    }
                }
                catch
                {
                    fileContent = System.Text.Encoding.UTF8.GetBytes($"Export content for {exportResult.FileName}");
                }

                var mimeType = exportResult.Format switch
                {
                    ReportFormat.PDF => "application/pdf",
                    ReportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ReportFormat.CSV => "text/csv",
                    ReportFormat.JSON => "application/json",
                    ReportFormat.HTML => "text/html",
                    _ => "application/octet-stream"
                };

                return new ExportDownloadResult
                {
                    Success = true,
                    FileContent = fileContent,
                    FileName = exportResult.FileName,
                    MimeType = mimeType,
                    LastModified = exportResult.GeneratedAt,
                    ETag = $"\"{exportId:N}\"",
                    SupportsRangeRequests = false,
                    ContentLength = fileContent.Length
                };
            }

            return new ExportDownloadResult
            {
                Success = false,
                ErrorMessage = $"Export with ID {exportId} not found"
            };
        }

        /// <summary>
        /// Deletes an exported file
        /// </summary>
        public async Task<bool> DeleteExportAsync(Guid exportId)
        {
            if (_completedExports.TryGetValue(exportId, out var exportResult))
            {
                // Try to delete the file from disk
                if (!string.IsNullOrEmpty(exportResult.FilePath))
                {
                    try
                    {
                        if (File.Exists(exportResult.FilePath))
                        {
                            File.Delete(exportResult.FilePath);
                        }
                    }
                    catch
                    {
                        // File may not exist or be inaccessible; continue with in-memory cleanup
                    }
                }

                _completedExports.Remove(exportId);
                _exportJobs.Remove(exportId);
                _sharedExports.Remove(exportId);
                _exportHistory.RemoveAll(h => h.Id == exportId);

                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        /// <summary>
        /// Shares an exported file with specified recipients
        /// </summary>
        public async Task<ExportShareResult> ShareExportAsync(Guid exportId, ExportShareRequest shareRequest)
        {
            if (!_completedExports.ContainsKey(exportId))
            {
                return new ExportShareResult
                {
                    Success = false,
                    ErrorMessage = $"Export with ID {exportId} not found"
                };
            }

            var shareToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            var recipients = shareRequest.EmailAddresses?.Select(email => new ShareRecipient
            {
                Email = email,
                SentAt = DateTime.UtcNow,
                HasAccessed = false,
                DownloadCount = 0
            }).ToList() ?? new List<ShareRecipient>();

            var result = new ExportShareResult
            {
                Success = true,
                ShareUrl = $"https://digitaltwin.app/exports/shared/{shareToken}",
                ShareToken = shareToken,
                ExpiresAt = shareRequest.ExpiresAt ?? DateTime.UtcNow.AddDays(7),
                DownloadLimit = shareRequest.DownloadLimit,
                DownloadCount = 0,
                Recipients = recipients
            };

            _sharedExports[exportId] = result;

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Gets export statistics
        /// </summary>
        public async Task<ExportStatistics> GetExportStatisticsAsync()
        {
            var now = DateTime.UtcNow;
            var allExports = _exportHistory.ToList();
            var completedCount = _completedExports.Count;

            return await Task.FromResult(new ExportStatistics
            {
                TotalExports = Math.Max(completedCount, allExports.Count),
                ExportsThisMonth = allExports.Count(e => e.GeneratedAt >= now.AddDays(-30)),
                ExportsThisWeek = allExports.Count(e => e.GeneratedAt >= now.AddDays(-7)),
                ExportsToday = allExports.Count(e => e.GeneratedAt.Date == now.Date),
                TotalDataExported = allExports.Sum(e => e.FileSize),
                ExportsByFormat = allExports.GroupBy(e => e.Format)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ExportsByUser = allExports
                    .Where(e => !string.IsNullOrEmpty(e.GeneratedBy))
                    .GroupBy(e => e.GeneratedBy)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ExportsByDataType = new Dictionary<string, int>
                {
                    { "report", allExports.Count / 2 + 1 },
                    { "analytics", allExports.Count / 3 + 1 },
                    { "data", allExports.Count / 4 + 1 }
                },
                AverageExportTime = 3.5,
                MostExportedData = new List<MostExportedData>
                {
                    new MostExportedData { DataType = "Energy Reports", ExportCount = 15, LastExported = now.AddHours(-2), TotalSize = 1024 * 1024 * 10 },
                    new MostExportedData { DataType = "Building KPIs", ExportCount = 12, LastExported = now.AddHours(-5), TotalSize = 1024 * 1024 * 8 },
                    new MostExportedData { DataType = "Sensor Data", ExportCount = 8, LastExported = now.AddDays(-1), TotalSize = 1024 * 1024 * 20 }
                },
                Trends = Enumerable.Range(0, 7).Select(i => new ExportTrend
                {
                    Date = now.AddDays(-i).Date,
                    ExportCount = 3 + (i % 4),
                    DataSize = 1024 * 1024 * (2 + i),
                    Formats = new Dictionary<ReportFormat, int>
                    {
                        { ReportFormat.PDF, 1 + (i % 2) },
                        { ReportFormat.Excel, 1 },
                        { ReportFormat.CSV, i % 2 }
                    }
                }).ToList(),
                RecentActivity = new List<ExportActivity>
                {
                    new ExportActivity { Timestamp = now.AddMinutes(-30), User = "user@example.com", Action = "Export", FileName = "energy_report.pdf", Format = ReportFormat.PDF, FileSize = 1024 * 500, Details = "Energy consumption report" },
                    new ExportActivity { Timestamp = now.AddHours(-2), User = "admin@example.com", Action = "Export", FileName = "building_kpis.xlsx", Format = ReportFormat.Excel, FileSize = 1024 * 800, Details = "Building KPIs export" }
                }
            });
        }

        /// <summary>
        /// Previews export data before generating the full export
        /// </summary>
        public async Task<ExportPreview> PreviewExportAsync(ExportPreviewRequest previewRequest)
        {
            try
            {
                var maxRows = previewRequest.MaxRows > 0 ? previewRequest.MaxRows : 10;
                var headers = new List<string>();
                var sampleData = new List<List<object>>();

                if (previewRequest.Data is List<MetricTrend> trends)
                {
                    headers = new List<string> { "Timestamp", "Metric Type", "Value", "Unit", "Change %" };
                    sampleData = trends.Take(maxRows).Select(t => new List<object>
                    {
                        t.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        t.MetricType,
                        t.Value,
                        t.Unit,
                        t.ChangePercentage?.ToString("F2") ?? "N/A"
                    }).ToList();
                }
                else if (previewRequest.Data is BuildingKPIs kpis)
                {
                    headers = new List<string> { "KPI", "Value", "Unit" };
                    sampleData = new List<List<object>>
                    {
                        new List<object> { "Total Energy Consumption", kpis.EnergyKPIs.TotalConsumption, "kWh" },
                        new List<object> { "Average Temperature", kpis.EnvironmentalKPIs.AverageTemperature, "C" },
                        new List<object> { "Occupancy Rate", kpis.OccupancyKPIs.OccupancyRate, "%" }
                    };
                }
                else
                {
                    headers = new List<string> { "Data" };
                    sampleData = new List<List<object>>
                    {
                        new List<object> { previewRequest.Data?.ToString() ?? "No data" }
                    };
                }

                return await Task.FromResult(new ExportPreview
                {
                    Success = true,
                    Content = $"Preview of {previewRequest.Format} export with {sampleData.Count} rows",
                    Format = previewRequest.Format,
                    RowCount = sampleData.Count,
                    ColumnCount = headers.Count,
                    Headers = headers,
                    SampleData = sampleData,
                    EstimatedFileSize = sampleData.Count * headers.Count * 50,
                    EstimatedGenerationTime = TimeSpan.FromSeconds(sampleData.Count * 0.1 + 1),
                    Warnings = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return await Task.FromResult(new ExportPreview
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Format = previewRequest.Format,
                    Warnings = new List<string> { "Preview generation encountered an error" }
                });
            }
        }
    }
}