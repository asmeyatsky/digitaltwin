using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository Interface Base
    /// 
    /// Architectural Intent:
    /// - Defines contract for all repository operations
    /// - Provides CRUD operations with async support
    /// - Supports batch operations for performance
    /// - Enables transaction management
    /// </summary>
    public interface IRepository<T> where T : class
    {
        Task SaveAsync(T entity);
        Task SaveBatchAsync(IEnumerable<T> entities);
        Task<T> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate);
        Task UpdateAsync(T entity);
        Task DeleteAsync(Guid id);
        Task<int> CountAsync();
        Task<bool> ExistsAsync(Guid id);
        Task<IEnumerable<T>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Building Repository Interface
    /// </summary>
    public interface IBuildingRepository : IRepository<Building>
    {
        Task<IEnumerable<Building>> GetByMetadataAsync(BuildingCategory category);
        Task<IEnumerable<Building>> GetByOwnerAsync(string owner);
        Task<Building> GetWithFloorsAsync(Guid buildingId);
    }

    /// <summary>
    /// Floor Repository Interface
    /// </summary>
    public interface IFloorRepository : IRepository<Floor>
    {
        Task<IEnumerable<Floor>> GetByBuildingIdAsync(Guid buildingId);
        Task<Floor> GetByNumberAsync(Guid buildingId, int floorNumber);
    }

    /// <summary>
    /// Room Repository Interface
    /// </summary>
    public interface IRoomRepository : IRepository<Room>
    {
        Task<IEnumerable<Room>> GetByFloorIdAsync(Guid floorId);
        Task<IEnumerable<Room>> GetByTypeAsync(RoomType roomType);
        Task<Room> GetByNameAsync(Guid floorId, string roomName);
    }

    /// <summary>
    /// Equipment Repository Interface
    /// </summary>
    public interface IEquipmentRepository : IRepository<Equipment>
    {
        Task<IEnumerable<Equipment>> GetByRoomIdAsync(Guid roomId);
        Task<IEnumerable<Equipment>> GetByTypeAsync(string equipmentType);
        Task<IEnumerable<Equipment>> GetByStatusAsync(string status);
        Task<Equipment> GetByModelAsync(string model);
    }

    /// <summary>
    /// Sensor Repository Interface
    /// </summary>
    public interface ISensorRepository : IRepository<Sensor>
    {
        Task<IEnumerable<Sensor>> GetByRoomIdAsync(Guid roomId);
        Task<IEnumerable<Sensor>> GetByTypeAsync(SensorType sensorType);
        Task<IEnumerable<Sensor>> GetByStatusAsync(string status);
        Task<Sensor> GetByModelAsync(string model);
    }

    /// <summary>
    /// Value Object Repositories
    /// </summary>
    public interface ISensorReadingRepository : IRepository<SensorReading>
    {
        Task<IEnumerable<SensorReading>> GetBySensorIdAsync(Guid sensorId, DateTime? startTime = null, DateTime? endTime = null);
        Task<IEnumerable<SensorReading>> GetBySensorTypeAsync(SensorType sensorType, DateTime? startTime = null, DateTime? endTime = null);
        Task<IEnumerable<SensorReading>> GetByDateRangeAsync(DateTime startDate, DateTime endTime);
        Task<long> GetCountBySensorIdAsync(Guid sensorId);
        Task DeleteByDateBeforeAsync(DateTime cutoffDate);
        Task DeleteBySensorIdAndDateBeforeAsync(Guid sensorId, DateTime cutoffDate);
    }

    public interface IOperationalMetricsRepository : IRepository<OperationalMetrics>
    {
        Task<OperationalMetrics> GetLatestByEquipmentIdAsync(Guid equipmentId);
        Task<IEnumerable<OperationalMetrics>> GetByEquipmentIdAsync(Guid equipmentId, DateTime startTime, DateTime endTime);
        Task<IEnumerable<OperationalMetrics>> GetByDateRangeAsync(DateTime startTime, DateTime endTime);
    }

    public interface IEnvironmentalConditionsRepository : IRepository<EnvironmentalConditions>
    {
        Task<EnvironmentalConditions> GetLatestByRoomIdAsync(Guid roomId);
        Task<IEnumerable<EnvironmentalConditions>> GetByRoomIdAsync(Guid roomId, DateTime startTime, DateTime endTime);
        Task<IEnumerable<EnvironmentalConditions>> GetByDateRangeAsync(DateTime startDate, DateTime endTime);
    }

    public interface IEnergyConsumptionRepository : IRepository<EnergyConsumption>
    {
        Task<IEnumerable<EnergyConsumption>> GetByBuildingIdAsync(Guid buildingId, DateTime? startTime = null, DateTime? endTime = null);
        Task<IEnumerable<EnergyConsumption>> GetByEquipmentTypeAsync(string equipmentType, DateTime? startTime = null, DateTime? endTime = null);
        Task<decimal> GetTotalConsumptionByBuildingIdAsync(Guid buildingId, DateTime startTime, DateTime endTime);
        Task<IEnumerable<EnergyConsumption>> GetByDateRangeAsync(DateTime startDate, DateTime endTime);
    }

    public interface ISimulationResultRepository : IRepository<SimulationResult>
    {
        Task<IEnumerable<SimulationResult>> GetByBuildingIdAsync(Guid buildingId, DateTime? startTime = null, DateTime? endTime = null);
        Task<SimulationResult> GetLatestByBuildingIdAsync(Guid buildingId, SimulationType? type = null);
        Task<SimulationResult> GetByTypeAsync(SimulationType type);
    }
}