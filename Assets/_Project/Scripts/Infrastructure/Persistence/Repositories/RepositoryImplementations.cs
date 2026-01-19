using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Data Context
    /// 
    /// Architectural Intent:
    /// - Provides centralized database connection management
    /// - Implements transaction scope management
    /// - Handles connection pooling for performance
    /// - Supports multiple database providers
    /// </summary>
    public class DataContext : IDisposable
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;
        private readonly Dictionary<string, object> _settings;
        private bool _disposed;

        public DataContext(Dictionary<string, object> settings)
        {
            _settings = settings;
            _connection = CreateConnection(settings);
            _transaction = null;
        }

        public IDbConnection Connection => _connection;

        public IDbTransaction Transaction => _transaction;

        public IDbTransaction BeginTransaction()
        {
            if (_transaction != null)
                throw new InvalidOperationException("Transaction already in progress");

            _transaction = _connection.BeginTransaction();
            return _transaction;
        }

        private IDbConnection CreateConnection(Dictionary<string, object> settings)
        {
            var connectionString = settings["connectionString"].ToString();
            var connectionType = settings["connectionType"].ToString();

            return connectionType switch
            {
                "SQLite" => new SQLiteConnection(connectionString),
                "PostgreSQL" => new NpgsqlConnection(connectionString),
                _ => throw new NotSupportedException($"Connection type {connectionType} not supported")
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Repository Base Implementation
    /// </summary>
    public abstract class RepositoryBase<T> : IRepository<T> where T : class
    {
        protected readonly DataContext _context;
        protected readonly string _tableName;

        protected RepositoryBase(DataContext context, string tableName)
        {
            _context = context;
            _tableName = tableName;
        }

        public virtual async Task SaveAsync(T entity)
        {
            const string sql = $"INSERT INTO {_tableName} (Id, Data, CreatedAt, UpdatedAt) VALUES (@Id, @Data, @CreatedAt, @UpdatedAt) " +
                             $"ON CONFLICT(Id) DO UPDATE SET Data = @Data, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            
            await _context.Connection.ExecuteAsync(sql, new { entity, entity.SerializeToJson(), DateTime.UtcNow, DateTime.UtcNow });
        }

        public virtual async Task SaveBatchAsync(IEnumerable<T> entities)
        {
            var entityList = entities.ToList();
            
            using (var transaction = _context.BeginTransaction())
            {
                foreach (var entity in entityList)
                {
                    await SaveAsync(entity);
                }
                
                await transaction.CommitAsync();
            }
        }

        public virtual async Task<T> GetByIdAsync(Guid id)
        {
            const string sql = $"SELECT * FROM {_tableName} WHERE Id = @Id";
            return await _context.Connection.QueryFirstOrDefaultAsync<T>(sql, new { id });
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            const string sql = $"SELECT * FROM {_tableName} ORDER BY CreatedAt DESC";
            return await _context.Connection.QueryAsync<T>(sql);
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate)
        {
            const string sql = $"SELECT * FROM {_tableName}";
            return await _context.Connection.QueryAsync<T>(sql, (T[])Array.CreateInstance(typeof(T[]), 0));
        }

        public virtual async Task UpdateAsync(T entity)
        {
            const string sql = $"UPDATE {_tableName} SET Data = @Data, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            await _context.Connection.ExecuteAsync(sql, new { entity.SerializeToJson(), DateTime.UtcNow });
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            const string sql = $"DELETE FROM {_tableName} WHERE Id = @Id";
            await _context.Connection.ExecuteAsync(sql, new { id });
        }

        public virtual async Task<int> CountAsync()
        {
            const string sql = $"SELECT COUNT(*) FROM {_tableName}";
            return await _context.Connection.QuerySingleAsync<int>(sql);
        }

        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            const string sql = $"SELECT EXISTS(SELECT 1 FROM {_tableName} WHERE Id = @Id)";
            return await _context.Connection.QuerySingleAsync<bool>(sql, new { id });
        }

        public virtual async Task<IEnumerable<T>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = $"SELECT * FROM {_tableName} WHERE CreatedAt >= @StartDate AND CreatedAt <= @EndDate ORDER BY CreatedAt DESC";
            return await _context.Connection.QueryAsync<T>(sql, new { startDate, endDate });
        }
    }

    /// <summary>
    /// Building Repository Implementation
    /// </summary>
    public class BuildingRepository : RepositoryBase<Building>, IBuildingRepository
    {
        public BuildingRepository(DataContext context) : base(context, "Buildings") { }

        public async Task<IEnumerable<Building>> GetByMetadataAsync(BuildingCategory category)
        {
            const string sql = @"SELECT * FROM Buildings b 
                              JOIN BuildingMetadata m ON b.Id = m.BuildingId 
                              WHERE m.Category = @Category 
                              ORDER BY b.Name";
            return await _context.Connection.QueryAsync<Building>(sql, new { category });
        }

        public async Task<IEnumerable<Building>> GetByOwnerAsync(string owner)
        {
            const string sql = @"SELECT * FROM BuildingMetadata m 
                              JOIN Buildings b ON b.Id = m.BuildingId 
                              WHERE m.Owner = @Owner 
                              ORDER BY b.Name";
            return await _context.Connection.QueryAsync<Building>(sql, new { owner });
        }

        public async Task<Building> GetWithFloorsAsync(Guid buildingId)
        {
            // This would require complex query with JOINs
            var building = await GetByIdAsync(buildingId);
            // In a real implementation, you'd load related floors separately
            return building;
        }
    }

    /// <summary>
    /// Sensor Reading Repository Implementation
    /// </summary>
    public class SensorReadingRepository : RepositoryBase<SensorReading>, ISensorReadingRepository
    {
        public SensorReadingRepository(DataContext context) : base(context, "SensorReadings") { }

        public async Task<IEnumerable<SensorReading>> GetBySensorIdAsync(Guid sensorId, DateTime? startTime = null, DateTime? endTime = null)
        {
            var sql = "SELECT * FROM SensorReadings WHERE SensorId = @SensorId";
            var parameters = new { sensorId };

            if (startTime.HasValue)
            {
                sql += " AND Timestamp >= @StartTime";
                parameters = new { sensorId, startTime.Value };
            }

            if (endTime.HasValue)
            {
                sql += " AND Timestamp <= @EndTime";
                parameters = new { sensorId, startTime.Value, endTime.Value };
            }

            sql += " ORDER BY Timestamp DESC";
            
            return await _context.Connection.QueryAsync<SensorReading>(sql, parameters);
        }

        public async Task<IEnumerable<SensorReading>> GetBySensorTypeAsync(SensorType sensorType, DateTime? startTime = null, DateTime? endTime = null)
        {
            var sql = @"SELECT sr.* FROM SensorReadings sr 
                              JOIN Sensors s ON sr.SensorId = s.Id 
                              WHERE s.Type = @SensorType";
            var parameters = new { sensorType };

            if (startTime.HasValue)
            {
                sql += " AND sr.Timestamp >= @StartTime";
                parameters = new { sensorType, startTime.Value };
            }

            if (endTime.HasValue)
            {
                sql += " AND sr.Timestamp <= @EndTime";
                parameters = new { sensorType, startTime.Value, endTime.Value };
            }

            sql += " ORDER BY sr.Timestamp DESC";
            
            return await _context.Connection.QueryAsync<SensorReading>(sql, parameters);
        }

        public async Task<long> GetCountBySensorIdAsync(Guid sensorId)
        {
            const string sql = "SELECT COUNT(*) FROM SensorReadings WHERE SensorId = @SensorId";
            return await _context.Connection.QuerySingleAsync<long>(sql, new { sensorId });
        }

        public async Task DeleteByDateBeforeAsync(DateTime cutoffDate)
        {
            const string sql = "DELETE FROM SensorReadings WHERE Timestamp < @CutoffDate";
            await _context.Connection.ExecuteAsync(sql, new { cutoffDate });
        }

        public async Task DeleteBySensorIdAndDateBeforeAsync(Guid sensorId, DateTime cutoffDate)
        {
            const string sql = "DELETE FROM SensorReadings WHERE SensorId = @SensorId AND Timestamp < @CutoffDate";
            await _context.Connection.ExecuteAsync(sql, new { sensorId, cutoffDate });
        }
    }

    // Extension methods for entity serialization
    public static class EntityExtensions
    {
        public static string SerializeToJson<T>(this T entity)
        {
            // In a real implementation, use a proper JSON serializer
            return System.Text.Json.JsonSerializer.Serialize(entity);
        }
    }
}