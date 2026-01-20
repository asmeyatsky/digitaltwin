using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.ValueObjects;

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
                Channel = NotificationChannel.Email,
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
                Channel = NotificationChannel.Email,
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
                        Channel = NotificationChannel.SMS,
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
                        Channel = NotificationChannel.Voice,
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
            var resolvedAlerts = alerts.Where(a => a.ResolvedAt.HasValue && a.Timestamp.HasValue).ToList();
            if (!resolvedAlerts.Any()) return TimeSpan.Zero;

            var totalMinutes = resolvedAlerts.Sum(a => (a.ResolvedAt.Value - a.Timestamp.Value).TotalMinutes);
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
    }
}