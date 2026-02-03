using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DigitalTwin.Core.Data;

namespace DigitalTwin.Core.Configuration
{
    /// <summary>
    /// Database configuration for Digital Twin System
    /// 
    /// Architectural Intent:
    /// - Configures EF Core with PostgreSQL
    /// - Sets up migration and development tools
    /// - Enables proper connection string management
    /// - Configures database performance settings
    /// 
    /// Key Features:
    /// 1. PostgreSQL connection configuration
    /// 2. Migration management
    /// 3. Development vs production settings
    /// 4. Connection pooling and timeout settings
    /// </summary>
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDigitalTwinDatabase(
            this IServiceCollection services, 
            string connectionString)
        {
            services.AddDbContext<DigitalTwinDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                });

                // Enable sensitive data logging in development
                #if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                #endif

                // Configure query tracking behavior
                options.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            });

            return services;
        }

        public static IApplicationBuilder EnsureDatabaseCreated(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DigitalTwinDbContext>();
            
            // Ensure database is created
            context.Database.EnsureCreated();
            
            return app;
        }

        public static IApplicationBuilder MigrateDatabase(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DigitalTwinDbContext>();
            
            // Run pending migrations
            context.Database.Migrate();
            
            return app;
        }
    }
}