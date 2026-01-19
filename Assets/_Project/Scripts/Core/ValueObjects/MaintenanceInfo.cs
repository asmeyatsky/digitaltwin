using System;

namespace DigitalTwin.Core.ValueObjects
{
    /// <summary>
    /// Maintenance Information Value Object
    /// 
    /// Architectural Intent:
    /// - Represents equipment maintenance scheduling and history
    /// - Provides immutable maintenance data for planning
    /// - Encapsulates maintenance requirements and tracking
    /// </summary>
    public readonly struct MaintenanceInfo : IEquatable<MaintenanceInfo>
    {
        public DateTime ScheduledDate { get; }
        public string Description { get; }
        public MaintenanceType Type { get; }
        public TimeSpan EstimatedDuration { get; }
        public decimal EstimatedCost { get; }

        public MaintenanceInfo(DateTime scheduledDate, string description, MaintenanceType type, 
                             TimeSpan estimatedDuration = default, decimal estimatedCost = 0)
        {
            if (string.IsNullOrEmpty(description))
                throw new ArgumentNullException(nameof(description));
            if (estimatedDuration < TimeSpan.Zero)
                throw new ArgumentException("Estimated duration cannot be negative", nameof(estimatedDuration));
            if (estimatedCost < 0)
                throw new ArgumentException("Estimated cost cannot be negative", nameof(estimatedCost));

            ScheduledDate = scheduledDate;
            Description = description;
            Type = type;
            EstimatedDuration = estimatedDuration == default ? TimeSpan.FromHours(2) : estimatedDuration;
            EstimatedCost = estimatedCost;
        }

        public static MaintenanceInfo Default 
            => new MaintenanceInfo(DateTime.MaxValue, "No maintenance scheduled", MaintenanceType.None);

        public bool IsOverdue() 
            => DateTime.UtcNow > ScheduledDate && Type != MaintenanceType.None;

        public bool IsUpcoming(TimeSpan threshold) 
            => ScheduledDate - DateTime.UtcNow <= threshold && ScheduledDate >= DateTime.UtcNow;

        public TimeSpan GetTimeUntilMaintenance() 
            => ScheduledDate - DateTime.UtcNow;

        public bool IsScheduled() 
            => Type != MaintenanceType.None && ScheduledDate != DateTime.MaxValue;

        public MaintenanceInfo Reschedule(DateTime newDate) 
            => new MaintenanceInfo(newDate, Description, Type, EstimatedDuration, EstimatedCost);

        public bool Equals(MaintenanceInfo other) 
            => ScheduledDate == other.ScheduledDate && Description == other.Description && 
               Type == other.Type && EstimatedDuration == other.EstimatedDuration && EstimatedCost == other.EstimatedCost;

        public override bool Equals(object obj) 
            => obj is MaintenanceInfo other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(ScheduledDate, Description, Type, EstimatedDuration, EstimatedCost);

        public override string ToString() 
            => $"{Type}: {Description} on {ScheduledDate:yyyy-MM-dd} (Duration: {EstimatedDuration.TotalHours:F1}h, Cost: ${EstimatedCost:F2})";
    }

    public enum MaintenanceType
    {
        None,
        Routine,
        Preventive,
        Corrective,
        Emergency,
        Calibration,
        Inspection,
        Upgrade
    }

    /// <summary>
    /// Building Event Value Object
    /// 
    /// Architectural Intent:
    /// - Represents domain events within the building system
    /// - Provides immutable event data for auditing and analytics
    /// - Encapsulates event metadata and classification
    /// </summary>
    public abstract class BuildingEvent
    {
        public Guid Id { get; }
        public Guid BuildingId { get; }
        public DateTime Timestamp { get; }
        public string Description { get; }
        public EventType Type { get; }
        public EventSeverity Severity { get; }

        protected BuildingEvent(Guid buildingId, string description, EventType type, EventSeverity severity)
        {
            Id = Guid.NewGuid();
            BuildingId = buildingId;
            Timestamp = DateTime.UtcNow;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Type = type;
            Severity = severity;
        }
    }

    public class FloorAddedEvent : BuildingEvent
    {
        public Guid FloorId { get; }

        public FloorAddedEvent(Guid buildingId, Guid floorId, DateTime timestamp) 
            : base(buildingId, $"Floor {floorId} added to building", EventType.Structural, EventSeverity.Info)
        {
            FloorId = floorId;
        }
    }

    public class FloorRemovedEvent : BuildingEvent
    {
        public Guid FloorId { get; }

        public FloorRemovedEvent(Guid buildingId, Guid floorId, DateTime timestamp) 
            : base(buildingId, $"Floor {floorId} removed from building", EventType.Structural, EventSeverity.Warning)
        {
            FloorId = floorId;
        }
    }

    public class MaintenanceModeChangedEvent : BuildingEvent
    {
        public bool InMaintenance { get; }

        public MaintenanceModeChangedEvent(Guid buildingId, bool inMaintenance, DateTime timestamp) 
            : base(buildingId, $"Building maintenance mode {(inMaintenance ? "enabled" : "disabled")}", 
                   EventType.Operational, inMaintenance ? EventSeverity.Warning : EventSeverity.Info)
        {
            InMaintenance = inMaintenance;
        }
    }

    public enum EventType
    {
        Structural,
        Operational,
        Environmental,
        Security,
        Maintenance,
        Emergency
    }

    public enum EventSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}