using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using DigitalTwin.Core.Entities;
using System.Text.Json;

namespace DigitalTwin.Core.Data
{
    /// <summary>
    /// Entity Framework DbContext for Digital Twin System
    /// 
    /// Architectural Intent:
    /// - Centralized data access layer for all entities
    /// - Supports PostgreSQL with EF Core migrations
    /// - Integrates with ASP.NET Core Identity for user management
    /// - Handles complex relationships and navigation properties
    /// 
    /// Key Features:
    /// 1. Full entity relationship mapping
    /// 2. Identity integration for user management
    /// 3. PostgreSQL optimization with indexes
    /// 4. JSON column support for complex properties
    /// 5. Audit trail and soft delete support
    /// </summary>
    public class DigitalTwinDbContext : DbContext
    {
        public DigitalTwinDbContext(DbContextOptions<DigitalTwinDbContext> options) : base(options)
        {
        }

        // AI Twin Entities
        public DbSet<AITwinProfile> AITwinProfiles { get; set; }
        public DbSet<AITwinKnowledge> AITwinKnowledge { get; set; }
        public DbSet<AITwinInteraction> AITwinInteractions { get; set; }
        public DbSet<AITwinMemory> AITwinMemories { get; set; }

        // Building Entities
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<Sensor> Sensors { get; set; }

        // Security Entities (simplified for now)
        // public DbSet<UserSession> UserSessions { get; set; }
        // public DbSet<SecurityEvent> SecurityEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AI Twin Profile
            modelBuilder.Entity<AITwinProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreationDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.LastInteraction).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.ActivationLevel).HasDefaultValue(0.5);
                
                // Convert complex objects to JSON
                entity.Property(e => e.PersonalityTraits)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<AITwinPersonalityTraits>(v, (JsonSerializerOptions)null));
                
                entity.Property(e => e.BehavioralPatterns)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, (JsonSerializerOptions)null));
                
                entity.Property(e => e.Preferences)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null));
            });

            // Configure AI Twin Knowledge
            modelBuilder.Entity<AITwinKnowledge>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Source).HasMaxLength(500);
                entity.Property(e => e.Importance).HasDefaultValue(0.5);
                entity.Property(e => e.Confidence).HasDefaultValue(0.5);
                entity.Property(e => e.CreationDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Convert tags list to JSON
                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));
            });

            // Configure AI Twin Interaction
            modelBuilder.Entity<AITwinInteraction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TwinId).IsRequired();
                entity.Property(e => e.MessageType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Convert complex objects to JSON
                entity.Property(e => e.Context)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null));
                
                // Configure response as owned entity
                entity.OwnsOne(e => e.Response, r =>
                {
                    r.Property(p => p.Content).IsRequired();
                    r.Property(p => p.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
                });
            });

            // Configure AI Twin Memory
            modelBuilder.Entity<AITwinMemory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Importance).HasDefaultValue(0.5);
                entity.Property(e => e.CreationDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.EmotionalValence).HasDefaultValue(0.0);
                
                // Convert complex properties to JSON
                entity.Property(e => e.AssociatedInteractions)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions)null));
                
                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));
            });

            // Configure Building
            modelBuilder.Entity<Building>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Name);
            });

            // Configure Floor
            modelBuilder.Entity<Floor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.HasOne<Building>().WithMany(b => b.Floors).HasForeignKey(e => e.BuildingId);
                entity.HasIndex(e => new { e.BuildingId, e.Number }).IsUnique();
            });

            // Configure Room
            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Type).HasMaxLength(100);
                entity.HasOne<Floor>().WithMany(f => f.Rooms).HasForeignKey(e => e.FloorId);
            });

            // Configure Equipment
            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Type).HasMaxLength(100);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.HasOne<Room>().WithMany(r => r.Equipment).HasForeignKey(e => e.RoomId);
            });

            // Configure Sensor
            modelBuilder.Entity<Sensor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.HasOne<Room>().WithMany(r => r.Sensors).HasForeignKey(e => e.RoomId);
                entity.HasIndex(e => new { e.RoomId, e.Type });
            });

            // Add indexes for performance
            modelBuilder.Entity<AITwinProfile>()
                .HasIndex(e => e.UserId);

            modelBuilder.Entity<AITwinInteraction>()
                .HasIndex(e => e.TwinId)
                .HasIndex(e => e.Timestamp);
        }
    }
}