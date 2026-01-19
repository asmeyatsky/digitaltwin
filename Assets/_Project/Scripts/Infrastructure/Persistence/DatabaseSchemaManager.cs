using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Infrastructure.Persistence.Repositories;

namespace DigitalTwin.Infrastructure.Persistence
{
    /// <summary>
    /// Database Schema Manager
    /// 
    /// Architectural Intent:
    /// - Manages database schema creation and migrations
    /// - Ensures proper table structures and indexes
    /// - Supports version-controlled schema evolution
    /// - Handles multiple database providers
    /// </summary>
    public class DatabaseSchemaManager
    {
        private readonly DataContext _context;
        private readonly Dictionary<string, object> _settings;
        private const int SCHEMA_VERSION = 1;

        public DatabaseSchemaManager(DataContext context, Dictionary<string, object> settings)
        {
            _context = context;
            _settings = settings;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                var currentVersion = await GetCurrentSchemaVersionAsync();
                if (currentVersion < SCHEMA_VERSION)
                {
                    await MigrateDatabaseAsync(currentVersion, SCHEMA_VERSION);
                await CreateOrUpdateSchemaAsync(SCHEMA_VERSION);
                await SaveSchemaVersionAsync(SCHEMA_VERSION);
                Debug.Log($"Database migrated to version {SCHEMA_VERSION}");
                return;
                }
                
                await CreateOrUpdateSchemaAsync(SCHEMA_VERSION);
                Debug.Log("Database initialized with current schema version");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize database: {ex.Message}");
                throw;
            }
        }

        public async Task CreateOrUpdateSchemaAsync(int version)
        {
            var connection = _context.Connection;
            
            // Create schema metadata table if it doesn't exist
            await CreateSchemaMetadataTableAsync(connection);
            
            switch (version)
            {
                case 1:
                    await CreateV1SchemaAsync(connection);
                    break;
                // Add future versions here for migrations
            }
        }

        private async Task CreateV1SchemaAsync(IDbConnection connection)
        {
            var createTables = new[]
            {
                CreateBuildingsTableAsync(connection),
                CreateFloorsTableAsync(connection),
                CreateRoomsTableAsync(connection),
                CreateEquipmentTableAsync(connection),
                CreateSensorsTableAsync(connection),
                CreateSensorReadingsTableAsync(connection),
                CreateOperationalMetricsTableAsync(connection),
                CreateEnvironmentalConditionsTableAsync(connection),
                CreateEnergyConsumptionTableAsync(connection),
                CreateSimulationResultsTableAsync(connection),
                CreateDomainEventsTableAsync(connection)
            };

            foreach (var createTable in createTables)
            {
                await createTable;
            }
            
            // Create indexes for performance
            await CreateIndexesAsync(connection);
        }

        private async Task CreateBuildingsTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS Buildings (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Address TEXT NOT NULL,
                    MetadataId TEXT NOT NULL,
                    Status INTEGER NOT NULL DEFAULT 0,
                    ConstructedDate INTEGER NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateFloorsTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS Floors (
                    Id TEXT PRIMARY KEY,
                    BuildingId TEXT NOT NULL,
                    Number INTEGER NOT NULL,
                    Area REAL NOT NULL,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Height REAL NOT NULL,
                    MetadataId TEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (BuildingId) REFERENCES Buildings(Id) ON DELETE CASCADE
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateRoomsTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS Rooms (
                    Id TEXT PRIMARY KEY,
                    FloorId TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Area REAL NOT NULL,
                    MaxOccupancy INTEGER NOT NULL DEFAULT 0,
                    Height REAL NOT NULL DEFAULT 3.0,
                    MetadataId TEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (FloorId) REFERENCES Floors(Id) ON DELETE CASCADE
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateEquipmentTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS Equipment (
                    Id TEXT PRIMARY KEY,
                    RoomId TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Model TEXT,
                    Manufacturer TEXT,
                    Status TEXT NOT NULL DEFAULT 'Active',
                    MetadataId TEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE CASCADE
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateSensorsTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS Sensors (
                    Id TEXT PRIMARY KEY,
                    RoomId TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Model TEXT,
                    Manufacturer TEXT,
                    Status TEXT NOT NULL DEFAULT 'Active',
                    MetadataId TEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE CASCADE
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateSensorReadingsTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS SensorReadings (
                    Id TEXT PRIMARY KEY,
                    SensorId TEXT NOT NULL,
                    Timestamp DATETIME NOT NULL,
                    Value TEXT NOT NULL,
                    Unit TEXT NOT NULL,
                    QualityLevel INTEGER NOT NULL DEFAULT 0,
                    QualityReason TEXT,
                    Metadata TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (SensorId) REFERENCES Sensors(Id) ON DELETE CASCADE
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateOperationalMetricsTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS OperationalMetrics (
                    Id TEXT PRIMARY KEY,
                    EquipmentId TEXT NOT NULL,
                    Timestamp DATETIME NOT NULL,
                    Efficiency REAL NOT NULL,
                    PowerConsumption REAL NOT NULL,
                    RuntimeHours REAL NOT NULL,
                    CycleCount INTEGER NOT NULL DEFAULT 0,
                    Temperature REAL,
                    Metadata TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (EquipmentId) REFERENCES Equipment(Id) ON DELETE CASCADE
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateEnvironmentalConditionsTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS EnvironmentalConditions (
                    Id TEXT PRIMARY KEY,
                    RoomId TEXT NOT NULL,
                    Timestamp DATETIME NOT NULL,
                    Temperature TEXT NOT NULL,
                    Humidity REAL NOT NULL,
                    AirQualityIndex INTEGER NOT NULL,
                    CO2Level REAL,
                    LightLevel REAL,
                    NoiseLevel REAL,
                    Metadata TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE CASCADE
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateEnergyConsumptionTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS EnergyConsumption (
                    Id TEXT PRIMARY KEY,
                    BuildingId TEXT NOT NULL,
                    EquipmentId TEXT,
                    Timestamp DATETIME NOT NULL,
                    Amount REAL NOT NULL,
                    Unit TEXT NOT NULL,
                    Cost REAL NOT NULL,
                    CarbonFootprint REAL NOT NULL,
                    Metadata TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (BuildingId) REFERENCES Buildings(Id) ON DELETE CASCADE,
                    FOREIGN KEY (EquipmentId) REFERENCES Equipment(Id) ON DELETE CASCADE
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateSimulationResultsTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS SimulationResults (
                    Id TEXT PRIMARY KEY,
                    BuildingId TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Parameters TEXT,
                    Results TEXT,
                    Status TEXT NOT NULL DEFAULT 'Completed',
                    StartTime DATETIME NOT NULL,
                    EndTime DATETIME NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Metadata TEXT,
                    FOREIGN KEY (BuildingId) REFERENCES Buildings(Id) ON DELETE CASCADE
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateDomainEventsTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS DomainEvents (
                    Id TEXT PRIMARY KEY,
                    AggregateId TEXT NOT NULL,
                    EventType TEXT NOT NULL,
                    EventData TEXT,
                    Timestamp DATETIME NOT NULL,
                    Metadata TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateIndexesAsync(IDbConnection connection)
        {
            var indexes = new[]
            {
                "CREATE INDEX IF NOT EXISTS idx_buildings_metadata ON Buildings(MetadataId)",
                "CREATE INDEX IF NOT EXISTS idx_floors_building ON Floors(BuildingId)",
                "CREATE INDEX IF NOT EXISTS idx_rooms_floor ON Rooms(FloorId)",
                "CREATE INDEX IF NOT EXISTS idx_sensors_room ON Sensors(RoomId)",
                "CREATE INDEX IF NOT EXISTS idx_sensor_readings_sensor_timestamp ON SensorReadings(SensorId, Timestamp)",
                "CREATE INDEX IF NOT EXISTS idx_sensor_readings_timestamp ON SensorReadings(Timestamp)",
                "CREATE INDEX IF NOT EXISTS idx_equipment_room ON Equipment(RoomId)",
                "CREATE INDEX IF NOT EXISTS idx_operational_metrics_equipment_timestamp ON OperationalMetrics(EquipmentId, Timestamp)",
                "CREATE INDEX IF NOT EXISTS idx_environmental_room_timestamp ON EnvironmentalConditions(RoomId, Timestamp)",
                "CREATE INDEX IF NOT EXISTS idx_energy_consumption_building_timestamp ON EnergyConsumption(BuildingId, Timestamp)",
                "CREATE INDEX IF NOT EXISTS idx_simulation_results_building_type ON SimulationResults(BuildingId, Type)",
                "CREATE INDEX IF NOT EXISTS idx_domain_events_aggregate_timestamp ON DomainEvents(AggregateId, Timestamp)"
            };

            foreach (var index in indexes)
            {
                await connection.ExecuteAsync(index);
            }
        }

        private async Task CreateSchemaMetadataTableAsync(IDbConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS SchemaMetadata (
                    Id INTEGER PRIMARY KEY,
                    Version INTEGER NOT NULL,
                    AppliedAt DATETIME NOT NULL,
                    Description TEXT NOT NULL
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task<int> GetCurrentSchemaVersionAsync()
        {
            const string sql = "SELECT Version FROM SchemaMetadata ORDER BY Version DESC LIMIT 1";
            
            try
            {
                return await _context.Connection.QuerySingleAsync<int>(sql);
            }
            catch
            {
                return 0;
            }
        }

        private async Task SaveSchemaVersionAsync(int version)
        {
            const string sql = @"
                INSERT OR REPLACE INTO SchemaMetadata (Version, AppliedAt, Description) 
                VALUES (@Version, @AppliedAt, @Description)";

            await _context.Connection.ExecuteAsync(sql, new { version, DateTime.UtcNow, $"Schema version {version}" });
        }

        private async Task MigrateDatabaseAsync(int fromVersion, int toVersion)
        {
            Debug.Log($"Migrating database from version {fromVersion} to {toVersion}");

            for (int version = fromVersion + 1; version <= toVersion; version++)
            {
                switch (version)
                {
                    case 2:
                        await ApplyV2MigrationAsync();
                        break;
                    // Add future versions here
                }
            }
        }

        private async Task ApplyV2MigrationAsync()
        {
            var connection = _context.Connection;
            
            // Example migration: Add new columns or restructure existing tables
            try
            {
                // Add new columns if they don't exist
                await connection.ExecuteAsync("ALTER TABLE Buildings ADD COLUMN Version INTEGER DEFAULT 1");
                
                Debug.Log("Applied database migration to version 2");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Migration to version 2 failed: {ex.Message}");
                throw;
            }
        }
    }
}