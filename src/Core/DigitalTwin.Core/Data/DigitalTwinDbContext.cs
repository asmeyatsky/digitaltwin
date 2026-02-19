using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using DigitalTwin.Core.Entities;
using Pgvector;
using Pgvector.EntityFrameworkCore;
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

        // Conversation Entities
        public DbSet<ConversationSession> ConversationSessions { get; set; }
        public DbSet<ConversationMessage> ConversationMessages { get; set; }
        public DbSet<ConversationMemory> ConversationMemories { get; set; }

        // Emotional Entities
        public DbSet<EmotionalMemory> EmotionalMemories { get; set; }

        // Subscription Entities
        public DbSet<Subscription> Subscriptions { get; set; }

        // Biometric Entities
        public DbSet<BiometricReading> BiometricReadings { get; set; }

        // Coaching Entities
        public DbSet<Goal> Goals { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<HabitRecord> HabitRecords { get; set; }

        // Shared Experience Entities
        public DbSet<SharedRoom> SharedRooms { get; set; }

        // Personal History Entities
        public DbSet<LifeEvent> LifeEvents { get; set; }
        public DbSet<PersonalContext> PersonalContexts { get; set; }

        // Achievement Entities
        public DbSet<AchievementDefinition> AchievementDefinitions { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }

        // Check-In Entities
        public DbSet<CheckInRecord> CheckInRecords { get; set; }

        // Family Entities
        public DbSet<Family> Families { get; set; }
        public DbSet<FamilyMember> FamilyMembers { get; set; }
        public DbSet<FamilyInvite> FamilyInvites { get; set; }

        // Community Entities
        public DbSet<CommunityGroup> CommunityGroups { get; set; }
        public DbSet<CommunityPost> CommunityPosts { get; set; }
        public DbSet<CommunityReply> CommunityReplies { get; set; }
        public DbSet<CommunityMembership> CommunityMemberships { get; set; }

        // Creative Expression Entities
        public DbSet<CreativeWork> CreativeWorks { get; set; }
        public DbSet<CollaborativeStory> CollaborativeStories { get; set; }
        public DbSet<StoryChapter> StoryChapters { get; set; }

        // Therapy / Clinical Screening Entities
        public DbSet<TherapistProfile> TherapistProfiles { get; set; }
        public DbSet<TherapySession> TherapySessions { get; set; }
        public DbSet<ClinicalScreening> ClinicalScreenings { get; set; }
        public DbSet<TherapistReferral> TherapistReferrals { get; set; }

        // Moderation Entities
        public DbSet<ContentReport> ContentReports { get; set; }
        public DbSet<AutoModerationResult> AutoModerationResults { get; set; }

        // Learning Entities
        public DbSet<LearningPath> LearningPaths { get; set; }
        public DbSet<LearningModule> LearningModules { get; set; }
        public DbSet<UserLearningProgress> UserLearningProgress { get; set; }

        // Notification Entities
        public DbSet<DeviceToken> DeviceTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enable pgvector extension
            modelBuilder.HasPostgresExtension("vector");

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

            // Configure ConversationSession
            modelBuilder.Entity<ConversationSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.StartedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasMany(e => e.Messages).WithOne().OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.SessionContext)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null));
                entity.Property(e => e.ConversationContext)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null));
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.IsActive);
            });

            // Configure ConversationMessage
            modelBuilder.Entity<ConversationMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure ConversationMemory
            modelBuilder.Entity<ConversationMemory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Importance).HasDefaultValue(0.5);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.Key });
            });

            // Configure Subscription
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Tier).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.StripeCustomerId);
                entity.HasIndex(e => e.StripeSubscriptionId);
            });

            // Configure EmotionalMemory embedding for pgvector
            modelBuilder.Entity<EmotionalMemory>(entity =>
            {
                entity.Property(e => e.Embedding)
                    .HasColumnType("vector(1536)");

                entity.HasIndex(e => e.Embedding)
                    .HasMethod("ivfflat")
                    .HasOperators("vector_cosine_ops");
            });

            // Configure CheckInRecord
            modelBuilder.Entity<CheckInRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(30);
                entity.Property(e => e.EmotionContext).HasMaxLength(500);
                entity.Property(e => e.Response).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.UserId, e.ScheduledAt });
            });

            // Configure BiometricReading
            modelBuilder.Entity<BiometricReading>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Unit).HasMaxLength(20);
                entity.Property(e => e.Source).HasMaxLength(30);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.UserId, e.Type, e.Timestamp });
            });

            // Configure Goal
            modelBuilder.Entity<Goal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.UserId, e.Status });
            });

            // Configure JournalEntry
            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Mood).HasMaxLength(30);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            });

            // Configure HabitRecord
            modelBuilder.Entity<HabitRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.HabitName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.UserId, e.HabitName, e.Date });
            });

            // Configure DeviceToken
            modelBuilder.Entity<DeviceToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Platform).IsRequired().HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.UserId, e.Token }).IsUnique();
            });

            // Configure SharedRoom
            modelBuilder.Entity<SharedRoom>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CreatorUserId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Participants)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));
                entity.HasIndex(e => e.IsActive);
            });

            // Configure LifeEvent
            modelBuilder.Entity<LifeEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Category).IsRequired()
                    .HasConversion<string>().HasMaxLength(30);
                entity.Property(e => e.EmotionalImpact)
                    .HasConversion<string>().HasMaxLength(30);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.EventDate);
                entity.HasIndex(e => new { e.UserId, e.EventDate });
            });

            // Configure PersonalContext
            modelBuilder.Entity<PersonalContext>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CulturalBackground).HasMaxLength(500);
                entity.Property(e => e.CommunicationPreferences).HasMaxLength(2000);
                entity.Property(e => e.ImportantPeople).HasMaxLength(4000);
                entity.Property(e => e.Values).HasMaxLength(4000);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId).IsUnique();
            });

            // Configure Family
            modelBuilder.Entity<Family>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.CreatedByUserId);
            });

            // Configure FamilyMember
            modelBuilder.Entity<FamilyMember>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role).IsRequired()
                    .HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.FamilyId);
                entity.HasIndex(e => new { e.FamilyId, e.UserId }).IsUnique();
            });

            // Configure FamilyInvite
            modelBuilder.Entity<FamilyInvite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.InviteCode).IsRequired().HasMaxLength(8);
                entity.Property(e => e.Role).IsRequired()
                    .HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.InviteCode).IsUnique();
                entity.HasIndex(e => e.FamilyId);
            });

            // Configure AchievementDefinition
            modelBuilder.Entity<AchievementDefinition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.IconName).HasMaxLength(50);
                entity.Property(e => e.Category).IsRequired()
                    .HasConversion<string>().HasMaxLength(30);
                entity.HasIndex(e => e.Key).IsUnique();
            });

            // Configure UserAchievement
            modelBuilder.Entity<UserAchievement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.UserId, e.AchievementDefinitionId }).IsUnique();
            });

            // Configure CommunityGroup
            modelBuilder.Entity<CommunityGroup>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Category).IsRequired()
                    .HasConversion<string>().HasMaxLength(30);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.CreatedByUserId);
            });

            // Configure CommunityPost
            modelBuilder.Entity<CommunityPost>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.GroupId);
                entity.HasIndex(e => e.AuthorUserId);
            });

            // Configure CommunityReply
            modelBuilder.Entity<CommunityReply>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.PostId);
                entity.HasIndex(e => e.AuthorUserId);
            });

            // Configure CommunityMembership
            modelBuilder.Entity<CommunityMembership>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role).IsRequired()
                    .HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.GroupId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
            });

            // Configure ContentReport
            modelBuilder.Entity<ContentReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContentType).IsRequired()
                    .HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.Reason).IsRequired()
                    .HasConversion<string>().HasMaxLength(30);
                entity.Property(e => e.Status).IsRequired()
                    .HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.Action).IsRequired()
                    .HasConversion<string>().HasMaxLength(30);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.ReviewNotes).HasMaxLength(4000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.ContentType, e.ContentId });
                entity.HasIndex(e => e.ReporterUserId);
                entity.HasIndex(e => e.Status);
            });

            // Configure AutoModerationResult
            modelBuilder.Entity<AutoModerationResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContentType).IsRequired()
                    .HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.FlagReason).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.ContentType, e.ContentId });
                entity.HasIndex(e => e.IsFlagged);
            });

            // Configure CreativeWork
            modelBuilder.Entity<CreativeWork>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Type).IsRequired()
                    .HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.Mood).IsRequired()
                    .HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.Type });
                entity.HasIndex(e => e.IsShared);
                entity.HasIndex(e => e.SharedToGroupId);
            });

            // Configure CollaborativeStory
            modelBuilder.Entity<CollaborativeStory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.RoomId);
                entity.HasIndex(e => e.CreatedByUserId);
            });

            // Configure StoryChapter
            modelBuilder.Entity<StoryChapter>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.StoryId);
                entity.HasIndex(e => new { e.StoryId, e.ChapterOrder });
            });

            // Configure TherapistProfile
            modelBuilder.Entity<TherapistProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Credentials).HasMaxLength(500);
                entity.Property(e => e.Bio).HasMaxLength(4000);
                entity.Property(e => e.Specializations).HasMaxLength(2000);
                entity.Property(e => e.Availability).HasMaxLength(4000);
                entity.Property(e => e.RatePerSession).HasPrecision(10, 2);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.IsVerified);
            });

            // Configure TherapySession
            modelBuilder.Entity<TherapySession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired()
                    .HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.Notes).HasMaxLength(4000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.TherapistId);
                entity.HasIndex(e => e.ClientUserId);
                entity.HasIndex(e => new { e.ClientUserId, e.ScheduledAt });
            });

            // Configure ClinicalScreening
            modelBuilder.Entity<ClinicalScreening>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired()
                    .HasConversion<string>().HasMaxLength(10);
                entity.Property(e => e.Responses).HasMaxLength(500);
                entity.Property(e => e.Severity).HasMaxLength(30);
                entity.Property(e => e.CompletedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.Type });
            });

            // Configure TherapistReferral
            modelBuilder.Entity<TherapistReferral>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reason).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.Urgency).IsRequired()
                    .HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Urgency);
            });

            // Configure LearningPath
            modelBuilder.Entity<LearningPath>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Category).IsRequired()
                    .HasConversion<string>().HasMaxLength(30);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Category);
            });

            // Configure LearningModule
            modelBuilder.Entity<LearningModule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.ExercisePrompt).HasMaxLength(4000);
                entity.HasIndex(e => e.PathId);
                entity.HasIndex(e => new { e.PathId, e.Order });
            });

            // Configure UserLearningProgress
            modelBuilder.Entity<UserLearningProgress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CompletedModules).HasMaxLength(2000);
                entity.Property(e => e.ReflectionNotes).HasMaxLength(8000);
                entity.Property(e => e.StartedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.PathId);
                entity.HasIndex(e => new { e.UserId, e.PathId }).IsUnique();
            });

            // Add indexes for performance
            modelBuilder.Entity<AITwinProfile>()
                .HasIndex(e => e.UserId);

            modelBuilder.Entity<AITwinInteraction>()
                .HasIndex(e => e.TwinId);

            modelBuilder.Entity<AITwinInteraction>()
                .HasIndex(e => e.Timestamp);
        }
    }
}