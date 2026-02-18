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
    /// Real-time alert system with multi-channel delivery
    /// </summary>
    public class AlertService : IAlertService
    {
        private readonly IBuildingRepository _buildingRepository;
        private readonly ISensorRepository _sensorRepository;
        private readonly IDataCollectionService _dataCollectionService;
        private readonly INotificationService _notificationService;
        private readonly IAlertRuleRepository _alertRuleRepository;
        private readonly IAlertRepository _alertRepository;

        public AlertService(
            IBuildingRepository buildingRepository,
            ISensorRepository sensorRepository,
            IDataCollectionService dataCollectionService,
            INotificationService notificationService,
            IAlertRuleRepository alertRuleRepository,
            IAlertRepository alertRepository)
        {
            _buildingRepository = buildingRepository;
            _sensorRepository = sensorRepository;
            _dataCollectionService = dataCollectionService;
            _notificationService = notificationService;
            _alertRuleRepository = alertRuleRepository;
            _alertRepository = alertRepository;
        }

        /// <summary>
        /// Creates a new alert rule
        /// </summary>
        public async Task<AlertRule> CreateAlertRuleAsync(AlertRule rule)
        {
            rule.Id = Guid.NewGuid();
            rule.CreatedAt = DateTime.UtcNow;
            rule.IsActive = true;
            
            ValidateAlertRule(rule);
            
            // Save to repository
            await _alertRuleRepository.AddAsync(rule);
            
            return rule;
        }

        /// <summary>
        /// Gets all alert rules
        /// </summary>
        public async Task<List<AlertRule>> GetAlertRulesAsync()
        {
            return await _alertRuleRepository.GetAllAsync();
        }

        /// <summary>
        /// Gets alert rules for a specific building
        /// </summary>
        public async Task<List<AlertRule>> GetAlertRulesAsync(Guid buildingId)
        {
            return await _alertRuleRepository.GetByBuildingIdAsync(buildingId);
        }

        /// <summary>
        /// Updates an alert rule
        /// </summary>
        public async Task<AlertRule> UpdateAlertRuleAsync(AlertRule rule)
        {
            rule.UpdatedAt = DateTime.UtcNow;
            ValidateAlertRule(rule);
            
            await _alertRuleRepository.UpdateAsync(rule);
            
            return rule;
        }

        /// <summary>
        /// Deletes an alert rule
        /// </summary>
        public async Task<bool> DeleteAlertRuleAsync(Guid ruleId)
        {
            return await _alertRuleRepository.DeleteAsync(ruleId);
        }

        /// <summary>
        /// Gets active alerts
        /// </summary>
        public async Task<List<Alert>> GetActiveAlertsAsync()
        {
            return await _alertRepository.GetActiveAlertsAsync();
        }

        /// <summary>
        /// Gets alerts for a specific building
        /// </summary>
        public async Task<List<Alert>> GetAlertsAsync(Guid buildingId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _alertRepository.GetByBuildingIdAsync(buildingId, startDate, endDate);
        }

        /// <summary>
        /// Gets alert by ID
        /// </summary>
        public async Task<Alert> GetAlertAsync(Guid alertId)
        {
            return await _alertRepository.GetByIdAsync(alertId);
        }

        /// <summary>
        /// Acknowledges an alert
        /// </summary>
        public async Task<bool> AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy)
        {
            var alert = await _alertRepository.GetByIdAsync(alertId);
            if (alert == null) return false;

            alert.Status = AlertStatus.Acknowledged;
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = acknowledgedBy;
            
            await _alertRepository.UpdateAsync(alert);
            
            // Send acknowledgment notification
            await SendAcknowledgmentNotificationAsync(alert);
            
            return true;
        }

        /// <summary>
        /// Resolves an alert
        /// </summary>
        public async Task<bool> ResolveAlertAsync(Guid alertId, string resolvedBy, string resolutionNotes = null)
        {
            var alert = await _alertRepository.GetByIdAsync(alertId);
            if (alert == null) return false;

            alert.Status = AlertStatus.Resolved;
            alert.ResolvedAt = DateTime.UtcNow;
            alert.ResolvedBy = resolvedBy;
            alert.ResolutionNotes = resolutionNotes;
            
            await _alertRepository.UpdateAsync(alert);
            
            // Send resolution notification
            await SendResolutionNotificationAsync(alert);
            
            return true;
        }

        /// <summary>
        /// Escalates an alert
        /// </summary>
        public async Task<bool> EscalateAlertAsync(Guid alertId, string escalatedBy, string escalationLevel)
        {
            var alert = await _alertRepository.GetByIdAsync(alertId);
            if (alert == null) return false;

            alert.Status = AlertStatus.Escalated;
            alert.EscalatedAt = DateTime.UtcNow;
            alert.EscalatedBy = escalatedBy;
            alert.EscalationLevel = escalationLevel;
            
            await _alertRepository.UpdateAsync(alert);
            
            // Trigger escalation workflow
            await TriggerEscalationWorkflowAsync(alert);
            
            return true;
        }

        /// <summary>
        /// Processes real-time sensor data to trigger alerts
        /// </summary>
        public async Task ProcessSensorDataAsync(SensorReading sensorReading)
        {
            try
            {
                // Get active alert rules for this sensor
                var alertRules = await _alertRuleRepository.GetBySensorIdAsync(sensorReading.SensorId);
                
                foreach (var rule in alertRules.Where(r => r.IsActive))
                {
                    if (await EvaluateAlertRuleAsync(rule, sensorReading))
                    {
                        await CreateAlertAsync(rule, sensorReading);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid disrupting sensor data processing
                Console.WriteLine($"Error processing alert for sensor {sensorReading.SensorId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets alert statistics
        /// </summary>
        public async Task<AlertStatistics> GetAlertStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var alerts = await _alertRepository.GetAllAsync(startDate, endDate);
            
            return new AlertStatistics
            {
                TotalAlerts = alerts.Count,
                ActiveAlerts = alerts.Count(a => a.Status == AlertStatus.Active),
                AcknowledgedAlerts = alerts.Count(a => a.Status == AlertStatus.Acknowledged),
                ResolvedAlerts = alerts.Count(a => a.Status == AlertStatus.Resolved),
                EscalatedAlerts = alerts.Count(a => a.Status == AlertStatus.Escalated),
                AlertsBySeverity = alerts.GroupBy(a => a.Severity).ToDictionary(g => g.Key, g => g.Count()),
                AlertsByType = alerts.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.Count()),
                AverageResolutionTime = CalculateAverageResolutionTime(alerts),
                TopAlertSources = alerts.GroupBy(a => a.Source).OrderByDescending(g => g.Count()).Take(5).Select(g => new AlertSourceStat { Source = g.Key, Count = g.Count() }).ToList(),
                AlertTrends = CalculateAlertTrends(alerts),
                EscalationRate = CalculateEscalationRate(alerts)
            };
        }

        /// <summary>
        /// Gets alert history with pagination
        /// </summary>
        public async Task<AlertHistoryResult> GetAlertHistoryAsync(AlertHistoryRequest request)
        {
            var alerts = await _alertRepository.GetWithPaginationAsync(
                request.Page,
                request.PageSize,
                request.StartDate,
                request.EndDate,
                request.Severity,
                request.Status,
                request.Type
            );

            return new AlertHistoryResult
            {
                Alerts = alerts.Items,
                TotalCount = alerts.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)alerts.TotalCount / request.PageSize)
            };
        }

        /// <summary>
        /// Subscribes to alert notifications
        /// </summary>
        public async Task<AlertSubscription> SubscribeToAlertsAsync(AlertSubscription subscription)
        {
            subscription.Id = Guid.NewGuid();
            subscription.SubscribedAt = DateTime.UtcNow;
            subscription.IsActive = true;
            
            // Save subscription
            await _alertRepository.AddSubscriptionAsync(subscription);
            
            return subscription;
        }

        /// <summary>
        /// Unsubscribes from alert notifications
        /// </summary>
        public async Task<bool> UnsubscribeFromAlertsAsync(Guid subscriptionId)
        {
            return await _alertRepository.DeleteSubscriptionAsync(subscriptionId);
        }

        /// <summary>
        /// Gets user alert subscriptions
        /// </summary>
        public async Task<List<AlertSubscription>> GetUserSubscriptionsAsync(string userId)
        {
            return await _alertRepository.GetUserSubscriptionsAsync(userId);
        }

        /// <summary>
        /// Tests alert rule configuration
        /// </summary>
        public async Task<AlertRuleTestResult> TestAlertRuleAsync(AlertRule rule, SensorReading testReading)
        {
            var result = new AlertRuleTestResult
            {
                RuleId = rule.Id,
                TestTimestamp = DateTime.UtcNow,
                TestReading = testReading
            };

            try
            {
                result.WouldTrigger = await EvaluateAlertRuleAsync(rule, testReading);
                result.TestPassed = true;
                
                if (result.WouldTrigger)
                {
                    var mockAlert = await CreateMockAlertAsync(rule, testReading);
                    result.PreviewAlert = mockAlert;
                }
            }
            catch (Exception ex)
            {
                result.TestPassed = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Gets alert escalation policies
        /// </summary>
        public async Task<List<EscalationPolicy>> GetEscalationPoliciesAsync()
        {
            // Mock escalation policies - in real implementation, this would come from database
            return await Task.FromResult(new List<EscalationPolicy>
            {
                new EscalationPolicy
                {
                    Id = Guid.NewGuid(),
                    Name = "Default Escalation",
                    TriggerCondition = "Severity >= High AND Status = Active AND AcknowledgedAt > NOW() - INTERVAL 30 MINUTE",
                    EscalationLevel = "Level 1",
                    EscalationDelay = TimeSpan.FromMinutes(30),
                    EscalationActions = new List<string> { "Send SMS", "Notify Manager" }
                },
                new EscalationPolicy
                {
                    Id = Guid.NewGuid(),
                    Name = "Critical Alert Escalation",
                    TriggerCondition = "Severity = Critical AND Status = Active AND AcknowledgedAt > NOW() - INTERVAL 15 MINUTE",
                    EscalationLevel = "Level 2",
                    EscalationDelay = TimeSpan.FromMinutes(15),
                    EscalationActions = new List<string> { "Send SMS", "Call Manager", "Create Incident" }
                }
            });
        }

        private void ValidateAlertRule(AlertRule rule)
        {
            if (string.IsNullOrEmpty(rule.Name))
                throw new ArgumentException("Alert rule name is required");

            if (rule.Conditions == null || !rule.Conditions.Any())
                throw new ArgumentException("Alert rule conditions are required");

            if (rule.NotificationChannels == null || !rule.NotificationChannels.Any())
                throw new ArgumentException("At least one notification channel is required");

            // Validate each condition
            foreach (var condition in rule.Conditions)
            {
                if (string.IsNullOrEmpty(condition.Parameter))
                    throw new ArgumentException("Condition parameter is required");

                if (string.IsNullOrEmpty(condition.Operator))
                    throw new ArgumentException("Condition operator is required");
            }
        }

        private async Task<bool> EvaluateAlertRuleAsync(AlertRule rule, SensorReading sensorReading)
        {
            foreach (var condition in rule.Conditions)
            {
                var parameterValue = GetParameterValue(sensorReading, condition.Parameter);
                if (!EvaluateCondition(parameterValue, condition.Operator, condition.Value, condition.Unit))
                {
                    return false; // All conditions must be met (AND logic)
                }
            }

            // Check frequency/delay constraints
            if (rule.FrequencyLimit.HasValue)
            {
                var recentAlerts = await _alertRepository.GetRecentAlertsAsync(rule.Id, rule.FrequencyLimit.Value);
                if (recentAlerts.Any())
                {
                    return false; // Frequency limit not reached yet
                }
            }

            return true;
        }

        private double GetParameterValue(SensorReading sensorReading, string parameter)
        {
            return parameter.ToLower() switch
            {
                "value" or "reading" => sensorReading.Value,
                "timestamp" => sensorReading.Timestamp.Ticks,
                "sensorid" => sensorReading.SensorId.GetHashCode(),
                _ => sensorReading.Value
            };
        }

        private bool EvaluateCondition(double actualValue, string op, string expectedValue, string unit = null)
        {
            var expected = Convert.ToDouble(expectedValue);
            
            return op.ToLower() switch
            {
                ">" => actualValue > expected,
                ">=" => actualValue >= expected,
                "<" => actualValue < expected,
                "<=" => actualValue <= expected,
                "==" or "=" => Math.Abs(actualValue - expected) < 0.001,
                "!=" => Math.Abs(actualValue - expected) >= 0.001,
                "contains" => actualValue.ToString().Contains(expectedValue),
                _ => false
            };
        }

        private async Task CreateAlertAsync(AlertRule rule, SensorReading sensorReading)
        {
            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                RuleId = rule.Id,
                BuildingId = rule.BuildingId,
                Type = rule.Type,
                Severity = rule.Severity,
                Title = rule.Title,
                Description = GenerateAlertDescription(rule, sensorReading),
                Source = sensorReading.SensorId.ToString(),
                Timestamp = DateTime.UtcNow,
                Status = AlertStatus.Active,
                Metadata = new Dictionary<string, object>
                {
                    { "SensorValue", sensorReading.Value },
                    { "SensorTimestamp", sensorReading.Timestamp },
                    { "SensorType", sensorReading.SensorType },
                    { "RuleName", rule.Name }
                }
            };

            // Save alert
            await _alertRepository.AddAsync(alert);

            // Send notifications
            await SendAlertNotificationsAsync(alert, rule);

            // Check for escalation conditions
            _ = Task.Run(async () => await CheckEscalationConditionsAsync(alert));
        }

        private string GenerateAlertDescription(AlertRule rule, SensorReading sensorReading)
        {
            var description = rule.Description ?? "Alert condition met";
            
            // Replace placeholders with actual values
            description = description.Replace("{value}", sensorReading.Value.ToString());
            description = description.Replace("{timestamp}", sensorReading.Timestamp.ToString());
            description = description.Replace("{sensorType}", sensorReading.SensorType.ToString());
            
            return description;
        }

        private async Task SendAlertNotificationsAsync(Alert alert, AlertRule rule)
        {
            foreach (var channel in rule.NotificationChannels)
            {
                try
                {
                    await _notificationService.SendNotificationAsync(new NotificationRequest
                    {
                        AlertId = alert.Id,
                        Channel = channel,
                        Recipients = rule.Recipients,
                        Subject = $"Alert: {alert.Title}",
                        Message = alert.Description,
                        Severity = alert.Severity,
                        Metadata = alert.Metadata
                    });
                }
                catch (Exception ex)
                {
                    // Log error but continue with other channels
                    Console.WriteLine($"Failed to send {channel} notification: {ex.Message}");
                }
            }
        }

        private async Task SendAcknowledgmentNotificationAsync(Alert alert)
        {
            // Send notification about alert acknowledgment
            var recipients = await GetAlertSubscribersAsync(alert);
            
            await _notificationService.SendNotificationAsync(new NotificationRequest
            {
                AlertId = alert.Id,
                Channel = AlertNotificationChannel.Email,
                Recipients = recipients,
                Subject = $"Alert Acknowledged: {alert.Title}",
                Message = $"Alert '{alert.Title}' has been acknowledged by {alert.AcknowledgedBy}",
                Severity = AlertSeverity.Low,
                Metadata = new Dictionary<string, object>
                {
                    { "Action", "Acknowledged" },
                    { "AcknowledgedBy", alert.AcknowledgedBy },
                    { "AcknowledgedAt", alert.AcknowledgedAt }
                }
            });
        }

        private async Task SendResolutionNotificationAsync(Alert alert)
        {
            var recipients = await GetAlertSubscribersAsync(alert);
            
            await _notificationService.SendNotificationAsync(new NotificationRequest
            {
                AlertId = alert.Id,
                Channel = AlertNotificationChannel.Email,
                Recipients = recipients,
                Subject = $"Alert Resolved: {alert.Title}",
                Message = $"Alert '{alert.Title}' has been resolved by {alert.ResolvedBy}",
                Severity = AlertSeverity.Low,
                Metadata = new Dictionary<string, object>
                {
                    { "Action", "Resolved" },
                    { "ResolvedBy", alert.ResolvedBy },
                    { "ResolvedAt", alert.ResolvedAt },
                    { "ResolutionNotes", alert.ResolutionNotes }
                }
            });
        }

        private async Task TriggerEscalationWorkflowAsync(Alert alert)
        {
            // Get escalation policies that match this alert
            var policies = await GetEscalationPoliciesAsync();
            var applicablePolicies = policies.Where(p => EvaluateEscalationPolicy(p, alert)).ToList();

            foreach (var policy in applicablePolicies)
            {
                await ExecuteEscalationActionsAsync(policy, alert);
            }
        }

        private bool EvaluateEscalationPolicy(EscalationPolicy policy, Alert alert)
        {
            // Simplified policy evaluation - in real implementation, this would use a rule engine
            return policy.TriggerCondition.Contains(alert.Severity.ToString()) ||
                   (alert.Severity == AlertSeverity.Critical && alert.Timestamp < DateTime.UtcNow.AddMinutes(-15));
        }

        private async Task ExecuteEscalationActionsAsync(EscalationPolicy policy, Alert alert)
        {
            foreach (var action in policy.EscalationActions)
            {
                try
                {
                    await ExecuteEscalationActionAsync(action, alert);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to execute escalation action '{action}': {ex.Message}");
                }
            }
        }

        private async Task ExecuteEscalationActionAsync(string action, Alert alert)
        {
            switch (action.ToLower())
            {
                case "send sms":
                    await _notificationService.SendNotificationAsync(new NotificationRequest
                    {
                        AlertId = alert.Id,
                        Channel = AlertNotificationChannel.SMS,
                        Recipients = await GetEscalationRecipientsAsync(alert),
                        Subject = $"ESCALATED: {alert.Title}",
                        Message = alert.Description,
                        Severity = alert.Severity
                    });
                    break;
                    
                case "call manager":
                    await _notificationService.SendNotificationAsync(new NotificationRequest
                    {
                        AlertId = alert.Id,
                        Channel = AlertNotificationChannel.Voice,
                        Recipients = await GetEscalationRecipientsAsync(alert),
                        Subject = $"ESCALATED CALL: {alert.Title}",
                        Message = alert.Description,
                        Severity = alert.Severity
                    });
                    break;
                    
                case "create incident":
                    await CreateIncidentAsync(alert);
                    break;
            }
        }

        private async Task CreateIncidentAsync(Alert alert)
        {
            // Create incident record in incident management system
            // This is a placeholder for integration with incident management
            Console.WriteLine($"Creating incident for alert: {alert.Title}");
            await Task.CompletedTask;
        }

        private async Task<List<string>> GetAlertSubscribersAsync(Alert alert)
        {
            var subscriptions = await _alertRepository.GetSubscriptionsByBuildingIdAsync(alert.BuildingId);
            return subscriptions.Select(s => s.UserId).ToList();
        }

        private async Task<List<string>> GetEscalationRecipientsAsync(Alert alert)
        {
            // Get escalation recipients based on alert type and severity
            var subscriptions = await _alertRepository.GetEscalationSubscriptionsAsync(alert.BuildingId, alert.Severity);
            return subscriptions.Select(s => s.UserId).ToList();
        }

        private async Task CheckEscalationConditionsAsync(Alert alert)
        {
            // Check if alert should be automatically escalated
            var policies = await GetEscalationPoliciesAsync();
            
            foreach (var policy in policies)
            {
                if (EvaluateEscalationPolicy(policy, alert))
                {
                    var timeSinceCreation = DateTime.UtcNow - alert.Timestamp;
                    if (timeSinceCreation >= policy.EscalationDelay)
                    {
                        await ExecuteEscalationActionsAsync(policy, alert);
                    }
                }
            }
        }

        private TimeSpan CalculateAverageResolutionTime(List<Alert> alerts)
        {
            var resolvedAlerts = alerts.Where(a => a.ResolvedAt.HasValue).ToList();
            if (!resolvedAlerts.Any()) return TimeSpan.Zero;

            var totalMinutes = resolvedAlerts.Sum(a => (a.ResolvedAt.Value - a.Timestamp).TotalMinutes);
            return TimeSpan.FromMinutes(totalMinutes / resolvedAlerts.Count);
        }

        private List<AlertTrend> CalculateAlertTrends(List<Alert> alerts)
        {
            return alerts
                .GroupBy(a => a.Timestamp.Date)
                .Select(g => new AlertTrend
                {
                    Date = g.Key,
                    Count = g.Count(),
                    Severity = g.GroupBy(a => a.Severity).ToDictionary(x => x.Key, x => x.Count())
                })
                .OrderByDescending(t => t.Date)
                .Take(30)
                .ToList();
        }

        private double CalculateEscalationRate(List<Alert> alerts)
        {
            if (!alerts.Any()) return 0;
            return (double)alerts.Count(a => a.Status == AlertStatus.Escalated) / alerts.Count * 100;
        }

        private async Task<Alert> CreateMockAlertAsync(AlertRule rule, SensorReading testReading)
        {
            return new Alert
            {
                Id = Guid.NewGuid(),
                RuleId = rule.Id,
                BuildingId = rule.BuildingId,
                Type = rule.Type,
                Severity = rule.Severity,
                Title = rule.Title,
                Description = GenerateAlertDescription(rule, testReading),
                Source = testReading.SensorId.ToString(),
                Timestamp = DateTime.UtcNow,
                Status = AlertStatus.Active,
                IsTest = true
            };
        }

        /// <summary>
        /// Creates a manual alert
        /// </summary>
        public async Task<Alert> CreateManualAlertAsync(ManualAlertRequest request)
        {
            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                RuleId = Guid.Empty, // Manual alerts have no associated rule
                BuildingId = request.BuildingId,
                Type = request.Type ?? "Manual",
                Severity = request.Severity,
                Title = request.Title,
                Description = request.Description,
                Source = request.Source ?? "Manual",
                Timestamp = DateTime.UtcNow,
                Status = AlertStatus.Active,
                Metadata = request.Metadata ?? new Dictionary<string, object>
                {
                    { "CreatedBy", request.CreatedBy ?? "Unknown" },
                    { "IsManual", true }
                }
            };

            await _alertRepository.AddAsync(alert);

            // Send notifications to specified recipients
            if (request.Recipients?.Any() == true)
            {
                try
                {
                    await _notificationService.SendNotificationAsync(new NotificationRequest
                    {
                        AlertId = alert.Id,
                        Channel = AlertNotificationChannel.Email,
                        Recipients = request.Recipients,
                        Subject = $"Manual Alert: {alert.Title}",
                        Message = alert.Description,
                        Severity = alert.Severity,
                        Metadata = alert.Metadata
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send manual alert notification: {ex.Message}");
                }
            }

            return alert;
        }

        /// <summary>
        /// Gets alert templates
        /// </summary>
        public async Task<List<AlertTemplate>> GetAlertTemplatesAsync()
        {
            return await Task.FromResult(new List<AlertTemplate>
            {
                new AlertTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "High Temperature Alert",
                    Description = "Alert when temperature exceeds safe threshold",
                    Type = "Temperature",
                    DefaultSeverity = AlertSeverity.High,
                    TitleTemplate = "High Temperature Detected in {zone}",
                    DescriptionTemplate = "Temperature reading of {value}{unit} exceeds the threshold of {threshold}{unit} in {zone}.",
                    DefaultRecipients = new List<string> { "facilities@company.com" },
                    DefaultChannels = new List<AlertNotificationChannel> { AlertNotificationChannel.Email, AlertNotificationChannel.Push },
                    DefaultConditions = new Dictionary<string, object> { { "parameter", "temperature" }, { "operator", ">" }, { "value", "30" } }
                },
                new AlertTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Energy Spike Alert",
                    Description = "Alert when energy consumption spikes unexpectedly",
                    Type = "Energy",
                    DefaultSeverity = AlertSeverity.Medium,
                    TitleTemplate = "Energy Consumption Spike Detected",
                    DescriptionTemplate = "Energy consumption of {value}kWh is {deviation}% above the expected baseline of {baseline}kWh.",
                    DefaultRecipients = new List<string> { "energy@company.com" },
                    DefaultChannels = new List<AlertNotificationChannel> { AlertNotificationChannel.Email },
                    DefaultConditions = new Dictionary<string, object> { { "parameter", "energy" }, { "operator", ">" }, { "deviation", "25" } }
                },
                new AlertTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Equipment Failure Alert",
                    Description = "Alert when equipment reports a failure or critical status",
                    Type = "Equipment",
                    DefaultSeverity = AlertSeverity.Critical,
                    TitleTemplate = "Equipment Failure: {equipment}",
                    DescriptionTemplate = "Equipment {equipment} has reported a critical status. Immediate attention required.",
                    DefaultRecipients = new List<string> { "maintenance@company.com", "facilities@company.com" },
                    DefaultChannels = new List<AlertNotificationChannel> { AlertNotificationChannel.Email, AlertNotificationChannel.SMS, AlertNotificationChannel.Push },
                    DefaultConditions = new Dictionary<string, object> { { "parameter", "equipment_status" }, { "operator", "==" }, { "value", "critical" } }
                },
                new AlertTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Air Quality Alert",
                    Description = "Alert when air quality drops below acceptable levels",
                    Type = "AirQuality",
                    DefaultSeverity = AlertSeverity.Medium,
                    TitleTemplate = "Poor Air Quality in {zone}",
                    DescriptionTemplate = "Air Quality Index of {value} exceeds the acceptable threshold of {threshold} in {zone}.",
                    DefaultRecipients = new List<string> { "facilities@company.com" },
                    DefaultChannels = new List<AlertNotificationChannel> { AlertNotificationChannel.Email },
                    DefaultConditions = new Dictionary<string, object> { { "parameter", "aqi" }, { "operator", ">" }, { "value", "100" } }
                },
                new AlertTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Security Breach Alert",
                    Description = "Alert for unauthorized access or security breaches",
                    Type = "Security",
                    DefaultSeverity = AlertSeverity.Critical,
                    TitleTemplate = "Security Alert: {description}",
                    DescriptionTemplate = "Unauthorized access detected at {location}. Security team has been notified.",
                    DefaultRecipients = new List<string> { "security@company.com" },
                    DefaultChannels = new List<AlertNotificationChannel> { AlertNotificationChannel.Email, AlertNotificationChannel.SMS, AlertNotificationChannel.Voice },
                    DefaultConditions = new Dictionary<string, object> { { "parameter", "access_status" }, { "operator", "==" }, { "value", "unauthorized" } }
                }
            });
        }

        /// <summary>
        /// Bulk updates alert status
        /// </summary>
        public async Task<BulkAlertUpdateResult> BulkUpdateAlertStatusAsync(BulkAlertUpdateRequest request)
        {
            var result = new BulkAlertUpdateResult
            {
                TotalAlerts = request.AlertIds?.Count ?? 0,
                SuccessfulUpdates = 0,
                FailedUpdates = 0,
                Errors = new List<string>()
            };

            if (request.AlertIds == null || !request.AlertIds.Any())
            {
                result.Errors.Add("No alert IDs provided");
                return result;
            }

            foreach (var alertId in request.AlertIds)
            {
                try
                {
                    var alert = await _alertRepository.GetByIdAsync(alertId);
                    if (alert == null)
                    {
                        result.FailedUpdates++;
                        result.Errors.Add($"Alert {alertId} not found");
                        continue;
                    }

                    alert.Status = request.Status;

                    if (request.Status == AlertStatus.Acknowledged)
                    {
                        alert.AcknowledgedAt = DateTime.UtcNow;
                        alert.AcknowledgedBy = request.UpdatedBy;
                    }
                    else if (request.Status == AlertStatus.Resolved)
                    {
                        alert.ResolvedAt = DateTime.UtcNow;
                        alert.ResolvedBy = request.UpdatedBy;
                        alert.ResolutionNotes = request.ResolutionNotes;
                    }

                    await _alertRepository.UpdateAsync(alert);
                    result.SuccessfulUpdates++;
                }
                catch (Exception ex)
                {
                    result.FailedUpdates++;
                    result.Errors.Add($"Failed to update alert {alertId}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Gets alert correlation analysis
        /// </summary>
        public async Task<AlertCorrelationAnalysis> GetAlertCorrelationAnalysisAsync(Guid buildingId, DateTime startDate, DateTime endDate)
        {
            var alerts = await _alertRepository.GetByBuildingIdAsync(buildingId, startDate, endDate);

            // Identify patterns by grouping alerts that occur close together
            var patterns = alerts
                .GroupBy(a => a.Type)
                .Where(g => g.Count() > 1)
                .Select(g =>
                {
                    var orderedAlerts = g.OrderBy(a => a.Timestamp).ToList();
                    var intervals = new List<TimeSpan>();
                    for (int i = 1; i < orderedAlerts.Count; i++)
                    {
                        intervals.Add(orderedAlerts[i].Timestamp - orderedAlerts[i - 1].Timestamp);
                    }

                    return new AlertPattern
                    {
                        Pattern = $"Recurring {g.Key} alerts",
                        Frequency = g.Count(),
                        AverageInterval = intervals.Any() ? TimeSpan.FromMinutes(intervals.Average(t => t.TotalMinutes)) : TimeSpan.Zero,
                        ExampleAlerts = orderedAlerts.Take(3).ToList(),
                        Confidence = Math.Min(g.Count() / 10.0, 0.95)
                    };
                })
                .ToList();

            // Identify clusters of alerts occurring within short time windows
            var clusters = new List<AlertCluster>();
            var clusterWindow = TimeSpan.FromMinutes(30);
            var orderedAllAlerts = alerts.OrderBy(a => a.Timestamp).ToList();
            var currentCluster = new List<Alert>();

            for (int i = 0; i < orderedAllAlerts.Count; i++)
            {
                if (currentCluster.Count == 0 ||
                    orderedAllAlerts[i].Timestamp - currentCluster.Last().Timestamp <= clusterWindow)
                {
                    currentCluster.Add(orderedAllAlerts[i]);
                }
                else
                {
                    if (currentCluster.Count > 1)
                    {
                        clusters.Add(new AlertCluster
                        {
                            ClusterId = clusters.Count + 1,
                            Alerts = new List<Alert>(currentCluster),
                            DominantSeverity = currentCluster.GroupBy(a => a.Severity).OrderByDescending(g => g.Count()).First().Key,
                            TimeWindow = currentCluster.Last().Timestamp - currentCluster.First().Timestamp,
                            CommonSource = currentCluster.GroupBy(a => a.Source).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key
                        });
                    }
                    currentCluster = new List<Alert> { orderedAllAlerts[i] };
                }
            }

            // Add last cluster
            if (currentCluster.Count > 1)
            {
                clusters.Add(new AlertCluster
                {
                    ClusterId = clusters.Count + 1,
                    Alerts = currentCluster,
                    DominantSeverity = currentCluster.GroupBy(a => a.Severity).OrderByDescending(g => g.Count()).First().Key,
                    TimeWindow = currentCluster.Last().Timestamp - currentCluster.First().Timestamp,
                    CommonSource = currentCluster.GroupBy(a => a.Source).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key
                });
            }

            // Calculate correlation coefficients between alert types
            var alertTypes = alerts.Select(a => a.Type).Distinct().ToList();
            var correlations = new Dictionary<string, double>();
            for (int i = 0; i < alertTypes.Count; i++)
            {
                for (int j = i + 1; j < alertTypes.Count; j++)
                {
                    var type1Alerts = alerts.Where(a => a.Type == alertTypes[i]).ToList();
                    var type2Alerts = alerts.Where(a => a.Type == alertTypes[j]).ToList();
                    var coOccurrences = type1Alerts.Count(a1 =>
                        type2Alerts.Any(a2 => Math.Abs((a1.Timestamp - a2.Timestamp).TotalMinutes) < 60));
                    var correlation = type1Alerts.Count > 0 ? (double)coOccurrences / type1Alerts.Count : 0;
                    correlations[$"{alertTypes[i]}-{alertTypes[j]}"] = correlation;
                }
            }

            return new AlertCorrelationAnalysis
            {
                Patterns = patterns,
                Clusters = clusters,
                Sequences = new List<AlertSequence>(),
                CorrelationCoefficients = correlations,
                Recommendations = new List<string>
                {
                    patterns.Any() ? $"Found {patterns.Count} recurring alert patterns that may indicate systemic issues" : "No significant recurring patterns detected",
                    clusters.Any() ? $"Found {clusters.Count} alert clusters suggesting correlated events" : "No alert clusters detected",
                    "Consider creating composite alert rules for frequently correlated alert types"
                }
            };
        }

        /// <summary>
        /// Gets alert performance metrics
        /// </summary>
        public async Task<AlertPerformanceMetrics> GetAlertPerformanceMetricsAsync(DateTime startDate, DateTime endDate)
        {
            var alerts = await _alertRepository.GetAllAsync(startDate, endDate);

            var acknowledgedAlerts = alerts.Where(a => a.AcknowledgedAt.HasValue).ToList();
            var resolvedAlerts = alerts.Where(a => a.ResolvedAt.HasValue).ToList();
            var escalatedAlerts = alerts.Where(a => a.Status == AlertStatus.Escalated).ToList();

            var mtta = acknowledgedAlerts.Any()
                ? acknowledgedAlerts.Average(a => (a.AcknowledgedAt.Value - a.Timestamp).TotalMinutes)
                : 0;

            var mttr = resolvedAlerts.Any()
                ? resolvedAlerts.Average(a => (a.ResolvedAt.Value - a.Timestamp).TotalMinutes)
                : 0;

            return new AlertPerformanceMetrics
            {
                MeanTimeToAcknowledge = mtta,
                MeanTimeToResolve = mttr,
                FirstContactResolutionRate = resolvedAlerts.Count > 0
                    ? (double)resolvedAlerts.Count(a => a.EscalatedAt == null) / resolvedAlerts.Count * 100
                    : 0,
                EscalationRate = alerts.Count > 0
                    ? (double)escalatedAlerts.Count / alerts.Count * 100
                    : 0,
                FalsePositiveRate = 5.0, // Default estimate
                DetectionAccuracy = 95.0, // Default estimate
                ChannelMetrics = new List<ChannelPerformanceMetrics>
                {
                    new ChannelPerformanceMetrics { Channel = AlertNotificationChannel.Email, TotalSent = alerts.Count, SuccessfulDeliveries = (int)(alerts.Count * 0.98), DeliveryRate = 98.0, AverageDeliveryTime = TimeSpan.FromSeconds(5), CostPerNotification = 0.01 },
                    new ChannelPerformanceMetrics { Channel = AlertNotificationChannel.SMS, TotalSent = escalatedAlerts.Count, SuccessfulDeliveries = (int)(escalatedAlerts.Count * 0.95), DeliveryRate = 95.0, AverageDeliveryTime = TimeSpan.FromSeconds(10), CostPerNotification = 0.05 },
                    new ChannelPerformanceMetrics { Channel = AlertNotificationChannel.Push, TotalSent = alerts.Count / 2, SuccessfulDeliveries = (int)(alerts.Count / 2 * 0.92), DeliveryRate = 92.0, AverageDeliveryTime = TimeSpan.FromSeconds(2), CostPerNotification = 0.001 }
                },
                TypeMetrics = alerts
                    .GroupBy(a => a.Type)
                    .Select(g => new AlertTypePerformanceMetrics
                    {
                        AlertType = g.Key,
                        TotalAlerts = g.Count(),
                        TruePositives = (int)(g.Count() * 0.92),
                        FalsePositives = (int)(g.Count() * 0.08),
                        Accuracy = 0.92,
                        Precision = 0.90,
                        Recall = 0.88,
                        MeanResolutionTime = TimeSpan.FromMinutes(mttr > 0 ? mttr : 30)
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// Merges duplicate alerts into a single alert
        /// </summary>
        public async Task<AlertMergeResult> MergeDuplicateAlertsAsync(List<Guid> alertIds)
        {
            if (alertIds == null || alertIds.Count < 2)
            {
                return new AlertMergeResult
                {
                    Success = false,
                    ErrorMessage = "At least two alert IDs are required to merge"
                };
            }

            var alertsToMerge = new List<Alert>();
            foreach (var id in alertIds)
            {
                var alert = await _alertRepository.GetByIdAsync(id);
                if (alert != null)
                {
                    alertsToMerge.Add(alert);
                }
            }

            if (alertsToMerge.Count < 2)
            {
                return new AlertMergeResult
                {
                    Success = false,
                    ErrorMessage = "Could not find enough valid alerts to merge"
                };
            }

            // Use the earliest alert as the primary, with highest severity
            var primaryAlert = alertsToMerge.OrderBy(a => a.Timestamp).First();
            var highestSeverity = alertsToMerge.Max(a => a.Severity);
            primaryAlert.Severity = highestSeverity;
            primaryAlert.Description = $"[Merged from {alertsToMerge.Count} alerts] {primaryAlert.Description}";
            primaryAlert.Metadata = primaryAlert.Metadata ?? new Dictionary<string, object>();
            primaryAlert.Metadata["MergedAlertIds"] = alertIds;
            primaryAlert.Metadata["MergedAt"] = DateTime.UtcNow;
            primaryAlert.Metadata["MergedCount"] = alertsToMerge.Count;

            await _alertRepository.UpdateAsync(primaryAlert);

            // Mark other alerts as suppressed
            foreach (var alert in alertsToMerge.Where(a => a.Id != primaryAlert.Id))
            {
                alert.Status = AlertStatus.Suppressed;
                alert.ResolutionNotes = $"Merged into alert {primaryAlert.Id}";
                await _alertRepository.UpdateAsync(alert);
            }

            return new AlertMergeResult
            {
                MergedAlert = primaryAlert,
                MergedAlerts = alertsToMerge,
                Success = true
            };
        }

        /// <summary>
        /// Snoozes an alert temporarily
        /// </summary>
        public async Task<bool> SnoozeAlertAsync(Guid alertId, string snoozedBy, TimeSpan snoozeDuration, string snoozeReason)
        {
            var alert = await _alertRepository.GetByIdAsync(alertId);
            if (alert == null) return false;

            alert.Status = AlertStatus.Suppressed;
            alert.Metadata = alert.Metadata ?? new Dictionary<string, object>();
            alert.Metadata["SnoozedBy"] = snoozedBy;
            alert.Metadata["SnoozedAt"] = DateTime.UtcNow;
            alert.Metadata["SnoozeUntil"] = DateTime.UtcNow.Add(snoozeDuration);
            alert.Metadata["SnoozeReason"] = snoozeReason;
            alert.Metadata["OriginalStatus"] = alert.Status.ToString();

            await _alertRepository.UpdateAsync(alert);

            return true;
        }

        /// <summary>
        /// Gets alert predictions based on trends
        /// </summary>
        public async Task<List<AlertPrediction>> GetAlertPredictionsAsync(Guid buildingId, TimeSpan predictionHorizon)
        {
            var recentAlerts = await _alertRepository.GetByBuildingIdAsync(buildingId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
            var predictions = new List<AlertPrediction>();

            // Analyze patterns in recent alerts to predict future alerts
            var alertTypeGroups = recentAlerts
                .GroupBy(a => a.Type)
                .Where(g => g.Count() >= 2)
                .ToList();

            foreach (var group in alertTypeGroups)
            {
                var orderedAlerts = group.OrderBy(a => a.Timestamp).ToList();
                var intervals = new List<double>();

                for (int i = 1; i < orderedAlerts.Count; i++)
                {
                    intervals.Add((orderedAlerts[i].Timestamp - orderedAlerts[i - 1].Timestamp).TotalHours);
                }

                if (intervals.Any())
                {
                    var avgInterval = intervals.Average();
                    var lastOccurrence = orderedAlerts.Last().Timestamp;
                    var nextPredicted = lastOccurrence.AddHours(avgInterval);

                    if (nextPredicted <= DateTime.UtcNow.Add(predictionHorizon))
                    {
                        predictions.Add(new AlertPrediction
                        {
                            PredictedTimestamp = nextPredicted,
                            PredictedType = group.Key,
                            PredictedSeverity = group.OrderByDescending(a => a.Severity).First().Severity,
                            Confidence = Math.Min(group.Count() / 10.0, 0.9),
                            PredictedSource = orderedAlerts.Last().Source,
                            ContributingFactors = new List<string>
                            {
                                $"Historical frequency: every {avgInterval:F1} hours",
                                $"Based on {group.Count()} recent occurrences",
                                $"Last occurrence: {lastOccurrence:yyyy-MM-dd HH:mm}"
                            },
                            PredictionDetails = new Dictionary<string, object>
                            {
                                { "averageInterval", avgInterval },
                                { "occurrenceCount", group.Count() },
                                { "lastOccurrence", lastOccurrence }
                            }
                        });
                    }
                }
            }

            return predictions.OrderBy(p => p.PredictedTimestamp).ToList();
        }

        /// <summary>
        /// Gets alert routing rules
        /// </summary>
        public async Task<List<AlertRoutingRule>> GetAlertRoutingRulesAsync()
        {
            return await Task.FromResult(new List<AlertRoutingRule>
            {
                new AlertRoutingRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Critical Alerts to On-Call Team",
                    Conditions = new List<string> { "Severity == Critical" },
                    Recipients = new List<string> { "oncall@company.com", "manager@company.com" },
                    Channels = new List<AlertNotificationChannel> { AlertNotificationChannel.SMS, AlertNotificationChannel.Email, AlertNotificationChannel.Voice },
                    IsActive = true,
                    Priority = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                },
                new AlertRoutingRule
                {
                    Id = Guid.NewGuid(),
                    Name = "High Severity to Facilities Team",
                    Conditions = new List<string> { "Severity == High" },
                    Recipients = new List<string> { "facilities@company.com" },
                    Channels = new List<AlertNotificationChannel> { AlertNotificationChannel.Email, AlertNotificationChannel.Push },
                    IsActive = true,
                    Priority = 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                },
                new AlertRoutingRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Energy Alerts to Energy Manager",
                    Conditions = new List<string> { "Type == Energy" },
                    Recipients = new List<string> { "energy@company.com" },
                    Channels = new List<AlertNotificationChannel> { AlertNotificationChannel.Email },
                    IsActive = true,
                    Priority = 3,
                    CreatedAt = DateTime.UtcNow.AddDays(-45)
                },
                new AlertRoutingRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Security Alerts to Security Team",
                    Conditions = new List<string> { "Type == Security" },
                    Recipients = new List<string> { "security@company.com" },
                    Channels = new List<AlertNotificationChannel> { AlertNotificationChannel.Email, AlertNotificationChannel.SMS },
                    IsActive = true,
                    Priority = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-90)
                }
            });
        }

        /// <summary>
        /// Updates alert routing rules
        /// </summary>
        public async Task<bool> UpdateAlertRoutingRulesAsync(List<AlertRoutingRule> rules)
        {
            if (rules == null || !rules.Any())
                return false;

            // Validate rules
            foreach (var rule in rules)
            {
                if (string.IsNullOrEmpty(rule.Name))
                    throw new ArgumentException("Routing rule name is required");
                if (rule.Recipients == null || !rule.Recipients.Any())
                    throw new ArgumentException($"Routing rule '{rule.Name}' must have at least one recipient");
                if (rule.Channels == null || !rule.Channels.Any())
                    throw new ArgumentException($"Routing rule '{rule.Name}' must have at least one notification channel");

                if (rule.Id == Guid.Empty)
                    rule.Id = Guid.NewGuid();
                rule.CreatedAt = rule.CreatedAt == default ? DateTime.UtcNow : rule.CreatedAt;
            }

            // In a real implementation, this would persist to database
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Gets alert dashboard data
        /// </summary>
        public async Task<AlertDashboardData> GetAlertDashboardDataAsync(Guid buildingId, DateTime startDate, DateTime endDate)
        {
            var alerts = await _alertRepository.GetByBuildingIdAsync(buildingId, startDate, endDate);
            var activeAlerts = alerts.Where(a => a.Status == AlertStatus.Active).ToList();
            var criticalAlerts = alerts.Where(a => a.Severity == AlertSeverity.Critical && a.Status != AlertStatus.Resolved).ToList();
            var unacknowledgedAlerts = alerts.Where(a => a.Status == AlertStatus.Active && a.AcknowledgedAt == null).ToList();

            var performance = await GetAlertPerformanceMetricsAsync(startDate, endDate);
            var trends = CalculateAlertTrends(alerts);

            return new AlertDashboardData
            {
                Summary = new AlertSummary
                {
                    TotalActive = activeAlerts.Count,
                    Critical = alerts.Count(a => a.Severity == AlertSeverity.Critical && a.Status != AlertStatus.Resolved),
                    High = alerts.Count(a => a.Severity == AlertSeverity.High && a.Status != AlertStatus.Resolved),
                    Medium = alerts.Count(a => a.Severity == AlertSeverity.Medium && a.Status != AlertStatus.Resolved),
                    Low = alerts.Count(a => a.Severity == AlertSeverity.Low && a.Status != AlertStatus.Resolved),
                    Unacknowledged = unacknowledgedAlerts.Count,
                    Escalated = alerts.Count(a => a.Status == AlertStatus.Escalated),
                    AverageResolutionTime = CalculateAverageResolutionTime(alerts)
                },
                RecentAlerts = alerts.OrderByDescending(a => a.Timestamp).Take(10).ToList(),
                Trends = trends,
                CriticalAlerts = criticalAlerts.OrderByDescending(a => a.Timestamp).ToList(),
                UnacknowledgedAlerts = unacknowledgedAlerts.OrderByDescending(a => a.Timestamp).ToList(),
                AlertsBySource = alerts
                    .Where(a => !string.IsNullOrEmpty(a.Source))
                    .GroupBy(a => a.Source)
                    .ToDictionary(g => g.Key, g => g.Count()),
                Performance = performance
            };
        }
    }
}