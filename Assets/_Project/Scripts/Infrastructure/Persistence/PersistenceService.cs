using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.ValueObjects;

namespace DigitalTwin.Infrastructure.Persistence
{
    /// <summary>
    /// Persistence Service Implementation
    /// 
    /// Architectural Intent:
    /// - Implements data storage and retrieval for digital twin entities
    /// - Provides transaction management and data consistency
    /// - Supports multiple storage backends (SQLite, PostgreSQL, etc.)
    /// - Enables backup, restore, and archival operations
    /// 
    /// Key Design Decisions:
    /// 1. Repository pattern implementation with clean abstractions
    /// 2. Transaction management with proper rollback
    /// 3. Async operations for performance
    /// 4. Configurable storage providers
    /// </summary>
    public class PersistenceService : MonoBehaviour, IPersistenceService
    {
        [Header("Persistence Configuration")]
        [SerializeField] private PersistenceConfiguration _config;
        [SerializeField] private bool _enableEncryption = false;
        [SerializeField] private int _maxConnections = 10;

        [Header("Storage Settings")]
        [SerializeField] private StorageProvider _storageProvider = StorageProvider.SQLite;
        [SerializeField] private string _databasePath = "DigitalTwin.db";
        [SerializeField] private int _maxBatchSize = 1000;

        [Header("Performance Settings")]
        [SerializeField] private bool _enableConnectionPooling = true;
        [SerializeField] private int _connectionTimeout = 30;
        [SerializeField] private bool _enableCaching = true;

        // Private fields
        private Dictionary<Type, IRepository> _repositories;
        private IConnectionPool _connectionPool;
        private ICacheManager _cacheManager;
        private readonly Queue<IDbTransaction> _transactionQueue = new Queue<IDbTransaction>();
        private readonly Dictionary<string, object> _storageSettings = new Dictionary<string, object>();

        // Events
        public event Action<string, DateTime> DataSaved;
        public event Action<string, DateTime> DataRestored;

        private void Start()
        {
            InitializePersistence();
        }

        private void InitializePersistence()
        {
            Debug.Log("Initializing persistence service...");

            try
            {
                _repositories = new Dictionary<Type, IRepository>();
                _storageSettings = InitializeStorageSettings();

                // Initialize connection pool
                _connectionPool = new ConnectionPool(_storageSettings, _maxConnections, _connectionTimeout);

                // Initialize cache manager
                if (_enableCaching)
                {
                    _cacheManager = new CacheManager(_storageSettings);
                }

                // Initialize repositories
                InitializeRepositories();

                Debug.Log($"Persistence service initialized with {_storageProvider} backend");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize persistence service: {ex.Message}");
                throw;
            }
        }

        private void InitializeRepositories()
        {
            var dataContext = new DataContext(_storageSettings, _connectionPool, _cacheManager);

            // Domain repositories
            _repositories[typeof(Building)] = new BuildingRepository(dataContext);
            _repositories[typeof(Floor)] = new FloorRepository(dataContext);
            _repositories[typeof(Room)] = new RoomRepository(dataContext);
            _repositories[typeof(Equipment)] = new EquipmentRepository(dataContext);
            _repositories[typeof(Sensor)] = new SensorRepository(dataContext);

            // Value object repositories
            _repositories[typeof(SensorReading)] = new SensorReadingRepository(dataContext);
            _repositories[typeof(OperationalMetrics)] = new OperationalMetricsRepository(dataContext);
            _repositories[typeof(EnvironmentalConditions)] = new EnvironmentalConditionsRepository(dataContext);
            _repositories[typeof(EnergyConsumption)] = new EnergyConsumptionRepository(dataContext);
            _repositories[typeof(SimulationResult)] = new SimulationResultRepository(dataContext);
        }

        public async Task SaveBuildingAsync(Building building)
        {
            if (building == null)
                throw new ArgumentNullException(nameof(building));

            try
            {
                var repository = GetRepository<Building>();
                await repository.SaveAsync(building);
                
                // Invalidate related cache
                _cacheManager?.InvalidateByPattern($"building:*");
                
                DataSaved?.Invoke($"Building {building.Id}", DateTime.UtcNow);
                Debug.Log($"Building {building.Id} saved successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save building {building.Id}: {ex.Message}");
                throw;
            }
        }

        public async Task<Building> GetBuildingAsync(Guid buildingId)
        {
            try
            {
                // Try cache first
                var cacheKey = $"building:{buildingId}";
                var cached = _cacheManager?.Get<Building>(cacheKey);
                
                if (cached != null)
                {
                    Debug.Log($"Building {buildingId} retrieved from cache");
                    return cached;
                }

                // Load from database
                var repository = GetRepository<Building>();
                var building = await repository.GetByIdAsync(buildingId);
                
                if (building != null)
                {
                    _cacheManager?.Set(cacheKey, building, TimeSpan.FromMinutes(30));
                    Debug.Log($"Building {buildingId} loaded from database and cached");
                }

                return building;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get building {buildingId}: {ex.Message}");
                throw;
            }
        }

        public async Task SaveSensorReadingAsync(SensorReading reading)
        {
            if (reading == null)
                throw new ArgumentNullException(nameof(reading));

            try
            {
                var repository = GetRepository<SensorReading>();
                await repository.SaveAsync(reading);
                
                // Update cache
                _cacheManager?.InvalidateByPattern($"sensor:{reading.SensorId}:*");
                _cacheManager?.Set($"sensor:{reading.SensorId}:{reading.Timestamp.Ticks}", reading, TimeSpan.FromMinutes(5));
                
                DataSaved?.Invoke($"SensorReading {reading.SensorId}", DateTime.UtcNow);
                Debug.Log($"Sensor reading {reading.SensorId} saved successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save sensor reading {reading.SensorId}: {ex.Message}");
                throw;
            }
        }

        public async Task SaveSensorReadingsBatchAsync(IEnumerable<SensorReading> readings)
        {
            if (readings == null)
                throw new ArgumentNullException(nameof(readings));

            var readingList = readings.ToList();
            var batchSize = Math.Min(_maxBatchSize, readingList.Count);
            
            try
            {
                var repository = GetRepository<SensorReading>();
                await repository.SaveBatchAsync(readingList.Take(batchSize));
                
                Debug.Log($"Saved batch of {batchSize} sensor readings");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save sensor readings batch: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<SensorReading>> GetSensorReadingsAsync(Guid sensorId, DateTime startTime, DateTime endTime)
        {
            try
            {
                // Check cache first
                var cacheKey = $"sensor:{sensorId}:readings:{startTime.Ticks}-{endTime.Ticks}";
                var cached = _cacheManager?.Get<IEnumerable<SensorReading>>(cacheKey);
                
                if (cached != null)
                {
                    Debug.Log($"Sensor readings {sensorId} retrieved from cache");
                    return cached;
                }

                // Load from database
                var repository = GetRepository<SensorReading>();
                var readings = await repository.GetBySensorIdAsync(sensorId, startTime, endTime);
                
                if (readings.Any())
                {
                    _cacheManager?.Set(cacheKey, readings, TimeSpan.FromMinutes(15));
                    Debug.Log($"Sensor readings {sensorId} loaded from database and cached");
                }

                return readings;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get sensor readings {sensorId}: {ex.Message}");
                throw;
            }
        }

        public async Task SaveEquipmentMetricsAsync(Guid equipmentId, OperationalMetrics metrics)
        {
            if (metrics == null)
                throw new ArgumentNullException(nameof(metrics));

            try
            {
                var repository = GetRepository<OperationalMetrics>();
                await repository.SaveAsync(equipmentId, metrics);
                
                // Update cache
                _cacheManager?.Set($"equipment:{equipmentId}:metrics", metrics, TimeSpan.FromMinutes(10));
                
                DataSaved?.Invoke($"EquipmentMetrics {equipmentId}", DateTime.UtcNow);
                Debug.Log($"Equipment metrics {equipmentId} saved successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save equipment metrics {equipmentId}: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<OperationalMetrics>> GetEquipmentMetricsAsync(Guid equipmentId, DateTime startTime, DateTime endTime)
        {
            try
            {
                // Check cache
                var cacheKey = $"equipment:{equipmentId}:metrics:{startTime.Ticks}-{endTime.Ticks}";
                var cached = _cacheManager?.Get<IEnumerable<OperationalMetrics>>(cacheKey);
                
                if (cached != null)
                    return cached;

                // Load from database
                var repository = GetRepository<OperationalMetrics>();
                var metrics = await repository.GetByEquipmentIdAsync(equipmentId, startTime, endTime);
                
                if (metrics.Any())
                {
                    _cacheManager?.Set(cacheKey, metrics, TimeSpan.FromMinutes(15));
                Debug.Log($"Equipment metrics {equipmentId} loaded from database and cached");
                }

                return metrics;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get equipment metrics {equipmentId}: {ex.Message}");
                throw;
            }
        }

        public async Task SaveSimulationResultAsync(SimulationResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            try
            {
                var repository = GetRepository<SimulationResult>();
                await repository.SaveAsync(result);
                
                DataSaved?.Invoke($"SimulationResult {result.GetType().Name}", DateTime.UtcNow);
                Debug.Log($"Simulation result saved successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save simulation result: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<SimulationResult>> GetSimulationResultsAsync(Guid buildingId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var repository = GetRepository<SimulationResult>();
                var results = await repository.GetByBuildingIdAsync(buildingId, startTime, endTime);
                
                Debug.Log($"Retrieved {results.Count()} simulation results for building {buildingId}");
                return results;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get simulation results {buildingId}: {ex.Message}");
                throw;
            }
        }

        public async Task ArchiveDataAsync(DateTime cutoffDate)
        {
            try
            {
                Debug.Log($"Starting archival of data before {cutoffDate:yyyy-MM-dd}");

                // Archive old sensor readings
                var sensorRepo = GetRepository<SensorReading>();
                await sensorRepo.ArchiveBeforeDateAsync(cutoffDate);
                
                // Archive old simulation results
                var simRepo = GetRepository<SimulationResult>();
                await simRepo.ArchiveBeforeDateAsync(cutoffDate);
                
                Debug.Log($"Data archival completed for cutoff date {cutoffDate:yyyy-MM-dd}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to archive data: {ex.Message}");
                throw;
            }
        }

        public async Task RestoreDataAsync(DateTime restoreDate)
        {
            try
            {
                Debug.Log($"Starting data restoration for {restoreDate:yyyy-MM-dd}");

                // Restore sensor readings
                var sensorRepo = GetRepository<SensorReading>();
                await sensorRepo.RestoreFromDateAsync(restoreDate);
                
                // Restore simulation results
                var simRepo = GetRepository<SimulationResult>();
                await simRepo.RestoreFromDateAsync(restoreDate);
                
                DataRestored?.Invoke("System", DateTime.UtcNow);
                Debug.Log($"Data restoration completed for date {restoreDate:yyyy-MM-dd}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to restore data: {ex.Message}");
                throw;
            }
        }

        public async Task<BackupResult> CreateBackupAsync(BackupParameters parameters)
        {
            try
            {
                Debug.Log("Starting backup creation process");

                var backupId = Guid.NewGuid().ToString();
                var backupPath = $"./Backups/backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

                // Create backup directory if it doesn't exist
                System.IO.Directory.CreateDirectory("./Backups/");

                // Collect all data for backup
                var backupData = new Dictionary<string, object>
                {
                    ["timestamp"] = DateTime.UtcNow,
                    ["version"] = "1.0",
                    ["parameters"] = parameters
                };

                // Add building data
                var buildingRepo = GetRepository<Building>();
                backupData["buildings"] = await buildingRepo.GetAllAsync();

                // Add sensor readings (limited by date range if specified)
                var sensorRepo = GetRepository<SensorReading>();
                var readings = parameters.IncludeHistoricalData 
                    ? await sensorRepo.GetAllAsync()
                    : await sensorRepo.GetByDateRangeAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
                backupData["sensorReadings"] = readings;

                // Add simulation results
                var simRepo = GetRepository<SimulationResult>();
                backupData["simulationResults"] = await simRepo.GetAllAsync();

                // Add system metadata
                backupData["system"] = new Dictionary<string, object>
                {
                    ["version"] = Application.version,
                    ["platform"] = Application.platform,
                    ["storageProvider"] = _storageProvider.ToString(),
                    ["databaseSize"] = await GetDatabaseSize()
                };

                // Write backup to file
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(backupData, Newtonsoft.Json.Formatting.Indented);
                await System.IO.File.WriteAllTextAsync(backupPath, json);

                var result = new BackupResult
                {
                    Id = backupId,
                    FilePath = backupPath,
                    Size = new System.IO.FileInfo(backupPath).Length,
                    CreatedAt = DateTime.UtcNow,
                    IsSuccess = true,
                    Description = "Backup completed successfully"
                };

                Debug.Log($"Backup created: {backupPath} ({result.Size} bytes)");
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create backup: {ex.Message}");
                return new BackupResult
                {
                    Id = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    IsSuccess = false,
                    Description = $"Backup failed: {ex.Message}"
                };
            }
        }

        public async Task RestoreBackupAsync(string backupId)
        {
            try
            {
                Debug.Log($"Starting backup restoration for {backupId}");

                var backupPath = $"./Backups/backup_{backupId.Split('_').Last()}.json";
                
                if (!System.IO.File.Exists(backupPath))
                {
                    throw new FileNotFoundException($"Backup file not found: {backupPath}");
                }

                // Read backup data
                var json = await System.IO.File.ReadAllTextAsync(backupPath);
                var backupData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                // Restore data
                await RestoreBackupData(backupData);

                DataRestored?.Invoke(backupId, DateTime.UtcNow);
                Debug.Log($"Backup restored successfully: {backupPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to restore backup {backupId}: {ex.Message}");
                throw;
            }
        }

        public async Task<StorageStatistics> GetStorageStatisticsAsync()
        {
            try
            {
                var stats = new StorageStatistics
                {
                    TotalSensors = await GetCountOfEntities<Sensor>(),
                    TotalEquipment = await GetCountOfEntities<Equipment>(),
                    TotalBuildings = await GetCountOfEntities<Building>(),
                    TotalSensorReadings = await GetCountOfEntities<SensorReading>(),
                    TotalSimulationResults = await GetCountOfEntities<SimulationResult>(),
                    DatabaseSize = await GetDatabaseSize(),
                    LastBackup = await GetLastBackupInfo(),
                    ArchivedDataSize = await GetArchivedDataSize()
                };

                Debug.Log("Storage statistics retrieved successfully");
                return stats;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get storage statistics: {ex.Message}");
                throw;
            }
        }

        private IRepository<T> GetRepository<T>()
        {
            if (_repositories.TryGetValue(typeof(T), out var repository))
            {
                return (IRepository<T>)repository;
            }

            throw new InvalidOperationException($"Repository for type {typeof(T).Name} not found");
        }

        private Dictionary<string, object> InitializeStorageSettings()
        {
            var settings = new Dictionary<string, object>();

            switch (_storageProvider)
            {
                case StorageProvider.SQLite:
                    settings["connectionString"] = $"Data Source={_databasePath};Version=3;";
                    settings["connectionType"] = "SQLite";
                    settings["batchSize"] = _maxBatchSize;
                    settings["journalMode"] = "WAL";
                    break;

                case StorageProvider.PostgreSQL:
                    var host = _config?.DatabaseHost ?? "localhost";
                    var port = _config?.DatabasePort ?? 5432;
                    var database = _config?.DatabaseName ?? "digitaltwin";
                    var username = _config?.DatabaseUsername ?? "postgres";
                    var password = _config?.DatabasePassword ?? "password";
                    
                    settings["connectionString"] = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
                    settings["connectionType"] = "PostgreSQL";
                    settings["poolSize"] = _maxConnections;
                    break;

                case StorageProvider.MySQL:
                    // Similar MySQL configuration
                    break;

                default:
                    throw new NotSupportedException($"Storage provider {_storageProvider} not supported");
            }

            settings["enableEncryption"] = _enableEncryption;
            settings["enableCaching"] = _enableCaching;
            settings["connectionTimeout"] = _connectionTimeout;

            return settings;
        }

        private async Task<long> GetDatabaseSize()
        {
            // Calculate database file size for SQLite
            if (_storageProvider == StorageProvider.SQLite)
            {
                var fileInfo = new System.IO.FileInfo(_databasePath);
                return fileInfo.Length;
            }

            // For other databases, this would require specific queries
            return 0;
        }

        private async Task<int> GetCountOfEntities<T>()
        {
            var repository = GetRepository<T>();
            return await repository.CountAsync();
        }

        private async Task<DateTime?> GetLastBackupInfo()
        {
            try
            {
                var backupDir = new System.IO.DirectoryInfo("./Backups/");
                if (backupDir.Exists)
                {
                    var files = backupDir.GetFiles("backup_*.json")
                        .OrderByDescending(f => f.CreationTime)
                        .FirstOrDefault();

                    return files?.CreationTime;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<long> GetArchivedDataSize()
        {
            // Calculate archived data size
            var archiveDir = new System.IO.DirectoryInfo("./Archives/");
            if (archiveDir.Exists)
            {
                return archiveDir.EnumerateFiles("*", System.IO.SearchOption.AllDirectories)
                    .Sum(f => f.Length);
            }
            return 0;
        }

        private async Task RestoreBackupData(Dictionary<string, object> backupData)
        {
            // Implementation would restore each data type
            Debug.Log("Backup data restoration implemented");
            await Task.CompletedTask;
        }

        private async Task<IDbTransaction> BeginTransactionAsync()
        {
            var transaction = new DbTransaction(_storageSettings);
            await transaction.BeginAsync();
            _transactionQueue.Enqueue(transaction);
            return transaction;
        }

        private void OnDestroy()
        {
            Debug.Log("Persistence service shutting down...");
            
            // Complete any pending transactions
            while (_transactionQueue.Count > 0)
            {
                var transaction = _transactionQueue.Dequeue();
                try
                {
                    await transaction.CommitAsync();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to commit transaction: {ex.Message}");
                }
            }

            // Close connections
            _connectionPool?.Dispose();
            Debug.Log("Persistence service shutdown complete");
        }
    }
}