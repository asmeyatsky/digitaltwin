using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Core.Services
{
    /// <summary>
    /// Analytics service for business intelligence and KPI calculations
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IBuildingRepository _buildingRepository;
        private readonly ISensorRepository _sensorRepository;
        private readonly IDataCollectionService _dataCollectionService;

        public AnalyticsService(
            IBuildingRepository buildingRepository,
            ISensorRepository sensorRepository,
            IDataCollectionService dataCollectionService)
        {
            _buildingRepository = buildingRepository;
            _sensorRepository = sensorRepository;
            _dataCollectionService = dataCollectionService;
        }

        /// <summary>
        /// Gets comprehensive KPIs for a building
        /// </summary>
        public async Task<BuildingKPIs> GetBuildingKPIsAsync(Guid buildingId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var building = await _buildingRepository.GetByIdAsync(buildingId);
            if (building == null)
                throw new ArgumentException($"Building with ID {buildingId} not found");

            var dateRange = startDate.HasValue && endDate.HasValue 
                ? new DateRange(startDate.Value, endDate.Value)
                : new DateRange(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

            var kpis = new BuildingKPIs
            {
                BuildingId = buildingId,
                BuildingName = building.Name,
                Period = dateRange,
                GeneratedAt = DateTime.UtcNow
            };

            // Energy KPIs
            kpis.EnergyKPIs = await CalculateEnergyKPIsAsync(buildingId, dateRange);
            
            // Environmental KPIs
            kpis.EnvironmentalKPIs = await CalculateEnvironmentalKPIsAsync(buildingId, dateRange);
            
            // Operational KPIs
            kpis.OperationalKPIs = await CalculateOperationalKPIsAsync(buildingId, dateRange);
            
            // Maintenance KPIs
            kpis.MaintenanceKPIs = await CalculateMaintenanceKPIsAsync(buildingId, dateRange);
            
            // Occupancy KPIs
            kpis.OccupancyKPIs = await CalculateOccupancyKPIsAsync(buildingId, dateRange);

            return kpis;
        }

        /// <summary>
        /// Gets trend data for specific metrics
        /// </summary>
        public async Task<List<MetricTrend>> GetMetricTrendsAsync(Guid buildingId, string metricType, DateTime startDate, DateTime endDate, TimeSpan interval)
        {
            var trends = new List<MetricTrend>();
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                var nextDate = currentDate.Add(interval);
                var trend = new MetricTrend
                {
                    Timestamp = currentDate,
                    MetricType = metricType
                };

                switch (metricType.ToLower())
                {
                    case "energy":
                        trend.Value = await CalculateEnergyConsumptionAsync(buildingId, currentDate, nextDate);
                        trend.Unit = "kWh";
                        break;
                    case "temperature":
                        trend.Value = await CalculateAverageTemperatureAsync(buildingId, currentDate, nextDate);
                        trend.Unit = "°C";
                        break;
                    case "humidity":
                        trend.Value = await CalculateAverageHumidityAsync(buildingId, currentDate, nextDate);
                        trend.Unit = "%";
                        break;
                    case "occupancy":
                        trend.Value = await CalculateOccupancyRateAsync(buildingId, currentDate, nextDate);
                        trend.Unit = "%";
                        break;
                    case "cost":
                        trend.Value = await CalculateEnergyCostAsync(buildingId, currentDate, nextDate);
                        trend.Unit = "$";
                        break;
                }

                trends.Add(trend);
                currentDate = nextDate;
            }

            return trends;
        }

        /// <summary>
        /// Gets comparative analytics between multiple buildings
        /// </summary>
        public async Task<ComparativeAnalytics> GetComparativeAnalyticsAsync(List<Guid> buildingIds, DateTime startDate, DateTime endDate)
        {
            var comparative = new ComparativeAnalytics
            {
                BuildingIds = buildingIds,
                Period = new DateRange(startDate, endDate),
                GeneratedAt = DateTime.UtcNow
            };

            var buildingKPIs = new List<BuildingKPIs>();
            foreach (var buildingId in buildingIds)
            {
                var kpis = await GetBuildingKPIsAsync(buildingId, startDate, endDate);
                buildingKPIs.Add(kpis);
            }

            comparative.BuildingKPIs = buildingKPIs;
            comparative.Rankings = CalculateRankings(buildingKPIs);
            comparative.Averages = CalculateAverages(buildingKPIs);

            return comparative;
        }

        /// <summary>
        /// Gets predictive insights using ML algorithms
        /// </summary>
        public async Task<PredictiveInsights> GetPredictiveInsightsAsync(Guid buildingId, DateTime startDate, DateTime endDate)
        {
            var insights = new PredictiveInsights
            {
                BuildingId = buildingId,
                GeneratedAt = DateTime.UtcNow,
                Confidence = 0.85 // Mock confidence score
            };

            // Get historical data for training
            var historicalData = await GetHistoricalDataAsync(buildingId, startDate.AddDays(-90), endDate);
            
            // Generate predictions
            insights.EnergyPrediction = await PredictEnergyConsumptionAsync(historicalData);
            insights.MaintenancePrediction = await PredictMaintenanceNeedsAsync(buildingId, historicalData);
            insights.OccupancyPrediction = await PredictOccupancyAsync(historicalData);
            insights.CostPrediction = await PredictCostsAsync(historicalData);
            insights.Recommendations = GenerateRecommendations(insights);

            return insights;
        }

        private async Task<EnergyKPIs> CalculateEnergyKPIsAsync(Guid buildingId, DateRange dateRange)
        {
            var energyKPIs = new EnergyKPIs();

            // Get all energy sensors for the building
            var energySensors = await _sensorRepository.GetByBuildingAndTypeAsync(buildingId, SensorType.EnergyMeter);
            
            var totalConsumption = 0.0;
            var peakConsumption = 0.0;
            var consumptionData = new List<double>();

            foreach (var sensor in energySensors)
            {
                var readings = await _dataCollectionService.GetSensorReadingsAsync(sensor.Id, dateRange.Start, dateRange.End);
                
                foreach (var reading in readings)
                {
                    var value = reading.Value;
                    totalConsumption += value;
                    consumptionData.Add(value);
                    peakConsumption = Math.Max(peakConsumption, value);
                }
            }

            energyKPIs.TotalConsumption = totalConsumption;
            energyKPIs.AverageConsumption = consumptionData.Any() ? consumptionData.Average() : 0;
            energyKPIs.PeakConsumption = peakConsumption;
            energyKPIs.ConsumptionPerSqm = energyKPIs.AverageConsumption / 1000; // Mock area calculation
            energyKPIs.Cost = totalConsumption * 0.12; // Mock energy rate
            energyKPIs.CarbonFootprint = totalConsumption * 0.0005; // Mock CO2 factor

            // Calculate efficiency score
            energyKPIs.EfficiencyScore = CalculateEnergyEfficiencyScore(energyKPIs);

            return energyKPIs;
        }

        private async Task<EnvironmentalKPIs> CalculateEnvironmentalKPIsAsync(Guid buildingId, DateRange dateRange)
        {
            var environmentalKPIs = new EnvironmentalKPIs();

            // Temperature sensors
            var tempSensors = await _sensorRepository.GetByBuildingAndTypeAsync(buildingId, SensorType.Temperature);
            var tempReadings = new List<double>();
            
            foreach (var sensor in tempSensors)
            {
                var readings = await _dataCollectionService.GetSensorReadingsAsync(sensor.Id, dateRange.Start, dateRange.End);
                tempReadings.AddRange(readings.Select(r => r.Value));
            }

            // Humidity sensors
            var humiditySensors = await _sensorRepository.GetByBuildingAndTypeAsync(buildingId, SensorType.Humidity);
            var humidityReadings = new List<double>();
            
            foreach (var sensor in humiditySensors)
            {
                var readings = await _dataCollectionService.GetSensorReadingsAsync(sensor.Id, dateRange.Start, dateRange.End);
                humidityReadings.AddRange(readings.Select(r => r.Value));
            }

            // Air quality sensors
            var airQualitySensors = await _sensorRepository.GetByBuildingAndTypeAsync(buildingId, SensorType.AirQuality);
            var airQualityReadings = new List<double>();
            
            foreach (var sensor in airQualitySensors)
            {
                var readings = await _dataCollectionService.GetSensorReadingsAsync(sensor.Id, dateRange.Start, dateRange.End);
                airQualityReadings.AddRange(readings.Select(r => r.Value));
            }

            environmentalKPIs.AverageTemperature = tempReadings.Any() ? tempReadings.Average() : 0;
            environmentalKPIs.TemperatureVariance = tempReadings.Any() ? CalculateVariance(tempReadings) : 0;
            environmentalKPIs.AverageHumidity = humidityReadings.Any() ? humidityReadings.Average() : 0;
            environmentalKPIs.HumidityVariance = humidityReadings.Any() ? CalculateVariance(humidityReadings) : 0;
            environmentalKPIs.AirQualityIndex = airQualityReadings.Any() ? airQualityReadings.Average() : 0;
            environmentalKPIs.ComfortScore = CalculateComfortScore(environmentalKPIs);

            return environmentalKPIs;
        }

        private async Task<OperationalKPIs> CalculateOperationalKPIsAsync(Guid buildingId, DateRange dateRange)
        {
            var operationalKPIs = new OperationalKPIs();

            // Get all equipment for the building
            var building = await _buildingRepository.GetByIdAsync(buildingId);
            var allEquipment = new List<Equipment>();

            foreach (var floor in building.Floors)
            {
                foreach (var room in floor.Rooms)
                {
                    allEquipment.AddRange(room.Equipment);
                }
            }

            operationalKPIs.TotalEquipment = allEquipment.Count;
            operationalKPIs.ActiveEquipment = allEquipment.Count(e => e.Status == Status.Operational);
            operationalKPIs.EquipmentUptime = CalculateEquipmentUptime(allEquipment, dateRange);
            operationalKPIs.SystemAvailability = operationalKPIs.EquipmentUptime / 100.0;
            operationalKPIs.ResponseTime = 2.5; // Mock response time in minutes
            operationalKPIs.ResolutionTime = 15.0; // Mock resolution time in minutes

            return operationalKPIs;
        }

        private async Task<MaintenanceKPIs> CalculateMaintenanceKPIsAsync(Guid buildingId, DateRange dateRange)
        {
            var maintenanceKPIs = new MaintenanceKPIs();

            // Mock maintenance data
            maintenanceKPIs.ScheduledMaintenance = 12;
            maintenanceKPIs.CompletedMaintenance = 10;
            maintenanceKPIs.EmergencyMaintenance = 3;
            maintenanceKPIs.PreventiveMaintenance = 8;
            maintenanceKPIs.MaintenanceCost = 5000.0;
            maintenanceKPIs.MeanTimeBetweenFailures = 720.0; // Hours
            maintenanceKPIs.MeanTimeToRepair = 4.5; // Hours
            maintenanceKPIs.MaintenanceCompliance = 83.3; // Percentage

            return maintenanceKPIs;
        }

        private async Task<OccupancyKPIs> CalculateOccupancyKPIsAsync(Guid buildingId, DateRange dateRange)
        {
            var occupancyKPIs = new OccupancyKPIs();

            // Get occupancy sensors
            var occupancySensors = await _sensorRepository.GetByBuildingAndTypeAsync(buildingId, SensorType.Motion);
            var occupancyReadings = new List<double>();

            foreach (var sensor in occupancySensors)
            {
                var readings = await _dataCollectionService.GetSensorReadingsAsync(sensor.Id, dateRange.Start, dateRange.End);
                occupancyReadings.AddRange(readings.Select(r => r.Value));
            }

            occupancyKPIs.AverageOccupancy = occupancyReadings.Any() ? occupancyReadings.Average() : 0;
            occupancyKPIs.PeakOccupancy = occupancyReadings.Any() ? occupancyReadings.Max() : 0;
            occupancyKPIs.OccupancyRate = occupancyKPIs.AverageOccupancy / 100.0; // Assuming max capacity is 100
            occupancyKPIs.SpaceUtilization = occupancyKPIs.OccupancyRate * 0.85; // Mock utilization factor

            return occupancyKPIs;
        }

        private async Task<double> CalculateEnergyConsumptionAsync(Guid buildingId, DateTime start, DateTime end)
        {
            var energySensors = await _sensorRepository.GetByBuildingAndTypeAsync(buildingId, SensorType.EnergyMeter);
            var totalConsumption = 0.0;

            foreach (var sensor in energySensors)
            {
                var readings = await _dataCollectionService.GetSensorReadingsAsync(sensor.Id, start, end);
                totalConsumption += readings.Sum(r => r.Value);
            }

            return totalConsumption;
        }

        private async Task<double> CalculateAverageTemperatureAsync(Guid buildingId, DateTime start, DateTime end)
        {
            var tempSensors = await _sensorRepository.GetByBuildingAndTypeAsync(buildingId, SensorType.Temperature);
            var allReadings = new List<double>();

            foreach (var sensor in tempSensors)
            {
                var readings = await _dataCollectionService.GetSensorReadingsAsync(sensor.Id, start, end);
                allReadings.AddRange(readings.Select(r => r.Value));
            }

            return allReadings.Any() ? allReadings.Average() : 0;
        }

        private async Task<double> CalculateAverageHumidityAsync(Guid buildingId, DateTime start, DateTime end)
        {
            var humiditySensors = await _sensorRepository.GetByBuildingAndTypeAsync(buildingId, SensorType.Humidity);
            var allReadings = new List<double>();

            foreach (var sensor in humiditySensors)
            {
                var readings = await _dataCollectionService.GetSensorReadingsAsync(sensor.Id, start, end);
                allReadings.AddRange(readings.Select(r => r.Value));
            }

            return allReadings.Any() ? allReadings.Average() : 0;
        }

        private async Task<double> CalculateOccupancyRateAsync(Guid buildingId, DateTime start, DateTime end)
        {
            var occupancySensors = await _sensorRepository.GetByBuildingAndTypeAsync(buildingId, SensorType.Motion);
            var allReadings = new List<double>();

            foreach (var sensor in occupancySensors)
            {
                var readings = await _dataCollectionService.GetSensorReadingsAsync(sensor.Id, start, end);
                allReadings.AddRange(readings.Select(r => r.Value));
            }

            return allReadings.Any() ? allReadings.Average() : 0;
        }

        private async Task<double> CalculateEnergyCostAsync(Guid buildingId, DateTime start, DateTime end)
        {
            var consumption = await CalculateEnergyConsumptionAsync(buildingId, start, end);
            return consumption * 0.12; // Mock energy rate
        }

        private double CalculateVariance(List<double> values)
        {
            if (!values.Any()) return 0;
            
            var mean = values.Average();
            var squaredDiffs = values.Select(v => Math.Pow(v - mean, 2));
            return squaredDiffs.Average();
        }

        private double CalculateEnergyEfficiencyScore(EnergyKPIs energyKPIs)
        {
            // Mock efficiency calculation based on consumption per area
            var consumptionPerSqm = energyKPIs.ConsumptionPerSqm;
            
            if (consumptionPerSqm < 50) return 95;
            if (consumptionPerSqm < 100) return 85;
            if (consumptionPerSqm < 150) return 75;
            if (consumptionPerSqm < 200) return 65;
            return 55;
        }

        private double CalculateComfortScore(EnvironmentalKPIs environmentalKPIs)
        {
            // Mock comfort score based on temperature and humidity
            var tempScore = Math.Max(0, 100 - Math.Abs(environmentalKPIs.AverageTemperature - 22) * 5);
            var humidityScore = Math.Max(0, 100 - Math.Abs(environmentalKPIs.AverageHumidity - 45) * 2);
            
            return (tempScore + humidityScore) / 2;
        }

        private double CalculateEquipmentUptime(List<Equipment> equipment, DateRange dateRange)
        {
            if (!equipment.Any()) return 0;
            
            var operationalCount = equipment.Count(e => e.Status == Status.Operational);
            return (operationalCount / (double)equipment.Count) * 100;
        }

        private List<BuildingRanking> CalculateRankings(List<BuildingKPIs> buildingKPIs)
        {
            var rankings = new List<BuildingRanking>();
            
            // Rank by energy efficiency
            var energyRanking = buildingKPIs
                .OrderByDescending(k => k.EnergyKPIs.EfficiencyScore)
                .Select((k, index) => new { Building = k, Rank = index + 1 })
                .ToList();

            // Rank by comfort score
            var comfortRanking = buildingKPIs
                .OrderByDescending(k => k.EnvironmentalKPIs.ComfortScore)
                .Select((k, index) => new { Building = k, Rank = index + 1 })
                .ToList();

            // Create ranking objects
            foreach (var kpi in buildingKPIs)
            {
                var ranking = new BuildingRanking
                {
                    BuildingId = kpi.BuildingId,
                    BuildingName = kpi.BuildingName,
                    EnergyEfficiencyRank = energyRanking.First(r => r.Building.BuildingId == kpi.BuildingId).Rank,
                    ComfortRank = comfortRanking.First(r => r.Building.BuildingId == kpi.BuildingId).Rank,
                    OverallRank = (energyRanking.First(r => r.Building.BuildingId == kpi.BuildingId).Rank +
                                 comfortRanking.First(r => r.Building.BuildingId == kpi.BuildingId).Rank) / 2.0
                };
                rankings.Add(ranking);
            }

            return rankings;
        }

        private AggregateKPIs CalculateAverages(List<BuildingKPIs> buildingKPIs)
        {
            return new AggregateKPIs
            {
                AverageEnergyConsumption = buildingKPIs.Average(k => k.EnergyKPIs.TotalConsumption),
                AverageTemperature = buildingKPIs.Average(k => k.EnvironmentalKPIs.AverageTemperature),
                AverageOccupancyRate = buildingKPIs.Average(k => k.OccupancyKPIs.OccupancyRate),
                AverageEfficiencyScore = buildingKPIs.Average(k => k.EnergyKPIs.EfficiencyScore),
                AverageComfortScore = buildingKPIs.Average(k => k.EnvironmentalKPIs.ComfortScore)
            };
        }

        private async Task<List<SensorReading>> GetHistoricalDataAsync(Guid buildingId, DateTime start, DateTime end)
        {
            // Mock historical data retrieval
            var historicalData = new List<SensorReading>();
            var sensors = await _sensorRepository.GetByBuildingAsync(buildingId);

            foreach (var sensor in sensors)
            {
                var readings = await _dataCollectionService.GetSensorReadingsAsync(sensor.Id, start, end);
                historicalData.AddRange(readings);
            }

            return historicalData;
        }

        private async Task<EnergyPrediction> PredictEnergyConsumptionAsync(List<SensorReading> historicalData)
        {
            // Mock ML prediction
            return new EnergyPrediction
            {
                NextDayConsumption = 1250.5,
                NextWeekConsumption = 8750.0,
                NextMonthConsumption = 37500.0,
                Trend = "Increasing",
                Confidence = 0.87,
                Factors = new List<string> { "Seasonal increase", "Occupancy growth", "Equipment efficiency" }
            };
        }

        private async Task<MaintenancePrediction> PredictMaintenanceNeedsAsync(Guid buildingId, List<SensorReading> historicalData)
        {
            // Mock maintenance prediction
            return new MaintenancePrediction
            {
                UpcomingMaintenance = new List<MaintenanceItem>
                {
                    new MaintenanceItem { EquipmentName = "HVAC System", Priority = "High", EstimatedDate = DateTime.UtcNow.AddDays(7) },
                    new MaintenanceItem { EquipmentName = "Elevator A", Priority = "Medium", EstimatedDate = DateTime.UtcNow.AddDays(14) }
                },
                RiskScore = 0.65,
                Recommendations = new List<string> { "Schedule HVAC inspection", "Monitor elevator performance" }
            };
        }

        private async Task<OccupancyPrediction> PredictOccupancyAsync(List<SensorReading> historicalData)
        {
            // Mock occupancy prediction
            return new OccupancyPrediction
            {
                NextDayPeakOccupancy = 85.5,
                NextWeekAverageOccupancy = 78.2,
                NextMonthOccupancyTrend = "Stable",
                SeasonalPattern = "Higher occupancy expected in summer months",
                Confidence = 0.82
            };
        }

        private async Task<CostPrediction> PredictCostsAsync(List<SensorReading> historicalData)
        {
            // Mock cost prediction
            return new CostPrediction
            {
                NextMonthEnergyCost = 4500.0,
                NextQuarterMaintenanceCost = 12500.0,
                NextYearOperationalCost = 75000.0,
                CostTrend = "Increasing",
                SavingsOpportunities = new List<string> { "Optimize HVAC schedule", "Upgrade lighting to LED", "Implement smart controls" }
            };
        }

        private List<string> GenerateRecommendations(PredictiveInsights insights)
        {
            var recommendations = new List<string>();

            if (insights.EnergyPrediction.Trend == "Increasing")
            {
                recommendations.Add("Consider energy efficiency upgrades");
            }

            if (insights.MaintenancePrediction.RiskScore > 0.7)
            {
                recommendations.Add("Increase preventive maintenance frequency");
            }

            if (insights.OccupancyPrediction.NextDayPeakOccupancy > 90)
            {
                recommendations.Add("Optimize space allocation for high occupancy");
            }

            return recommendations;
        }
    }
}