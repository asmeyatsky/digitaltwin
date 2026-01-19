using System;
using System.Collections.Generic;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// Repository Interfaces for Data Access
    /// 
    /// Architectural Intent:
    /// - Defines contracts for data access following repository pattern
    /// - Provides abstraction over data storage implementation
    /// - Enables testability and dependency injection
    /// - Supports unit of work pattern for transactions
    /// </summary>
    public interface IBuildingRepository
    {
        Task<Building> GetByIdAsync(Guid id);
        Task<IEnumerable<Building>> GetAllAsync();
        Task SaveAsync(Building building);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }

    public interface IFloorRepository
    {
        Task<Core.Entities.Floor> GetByIdAsync(Guid id);
        Task<IEnumerable<Core.Entities.Floor>> GetByBuildingIdAsync(Guid buildingId);
        Task SaveAsync(Core.Entities.Floor floor);
        Task DeleteAsync(Guid id);
    }

    public interface IRoomRepository
    {
        Task<Core.Entities.Room> GetByIdAsync(Guid id);
        Task<IEnumerable<Core.Entities.Room>> GetByFloorIdAsync(Guid floorId);
        Task SaveAsync(Core.Entities.Room room);
        Task DeleteAsync(Guid id);
    }

    public interface IEquipmentRepository
    {
        Task<Core.Entities.Equipment> GetByIdAsync(Guid id);
        Task<IEnumerable<Core.Entities.Equipment>> GetByRoomIdAsync(Guid roomId);
        Task SaveAsync(Core.Entities.Equipment equipment);
        Task DeleteAsync(Guid id);
    }

    public interface ISensorRepository
    {
        Task<Core.Entities.Sensor> GetByIdAsync(Guid id);
        Task<IEnumerable<Core.Entities.Sensor>> GetByRoomIdAsync(Guid roomId);
        Task SaveAsync(Core.Entities.Sensor sensor);
        Task DeleteAsync(Guid id);
    }

    public interface ISensorReadingRepository
    {
        Task SaveAsync(SensorReading reading);
        Task SaveBatchAsync(IEnumerable<SensorReading> readings);
        Task<IEnumerable<SensorReading>> GetBySensorIdAsync(Guid sensorId, DateTime startTime, DateTime endTime);
        Task<IEnumerable<SensorReading>> GetByRoomIdAsync(Guid roomId, DateTime startTime, DateTime endTime);
        Task<IEnumerable<SensorReading>> GetLatestAsync(IEnumerable<Guid> sensorIds);
    }

    public interface ISimulationResultRepository
    {
        Task SaveAsync(SimulationResult result);
        Task<IEnumerable<SimulationResult>> GetByBuildingIdAsync(Guid buildingId, DateTime startTime, DateTime endTime);
        Task<SimulationResult> GetLatestAsync(Guid buildingId, SimulationType type);
    }

    /// <summary>
    /// Unit of Work Interface
    /// 
    /// Architectural Intent:
    /// - Manages transactions across multiple repositories
    /// - Ensures data consistency and atomic operations
    /// - Provides commit/rollback functionality
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IBuildingRepository Buildings { get; }
        IFloorRepository Floors { get; }
        IRoomRepository Rooms { get; }
        IEquipmentRepository Equipment { get; }
        ISensorRepository Sensors { get; }
        ISensorReadingRepository SensorReadings { get; }
        ISimulationResultRepository SimulationResults { get; }

        Task<int> CommitAsync();
        Task RollbackAsync();
    }

    /// <summary>
    /// Database Transaction Interface
    /// </summary>
    public interface IDbTransaction : IDisposable
    {
        Task CommitAsync();
        Task RollbackAsync();
    }
}