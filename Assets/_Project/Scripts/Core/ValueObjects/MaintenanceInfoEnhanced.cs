using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.ValueObjects
{
    /// <summary>
    /// Maintenance Information Value Object
    /// 
    /// Architectural Intent:
    /// - Represents maintenance schedule and history
    /// - Provides predictive maintenance capabilities
    /// - Enables maintenance cost tracking and planning
    /// - Supports equipment lifecycle management
    /// 
    /// Invariants:
    /// - Next maintenance date must be in the future or null
    /// - Maintenance intervals must be positive
    /// - Cost cannot be negative
    /// </summary>
    public readonly struct MaintenanceInfo : IEquatable<MaintenanceInfo>
    {
        public DateTime LastMaintenance { get; }
        public DateTime? NextScheduledMaintenance { get; }
        public TimeSpan MaintenanceInterval { get; }
        public decimal LastMaintenanceCost { get; }
        public MaintenanceType LastMaintenanceType { get; }
        public int MaintenanceCount { get; }
        public MaintenanceStatus Status { get; }
        public Dictionary<string, object> MaintenanceNotes { get; }

        public MaintenanceInfo(DateTime lastMaintenance, DateTime? nextScheduledMaintenance, TimeSpan maintenanceInterval,
            decimal lastMaintenanceCost, MaintenanceType lastMaintenanceType, int maintenanceCount,
            MaintenanceStatus status, Dictionary<string, object> maintenanceNotes = null)
        {
            if (maintenanceInterval <= TimeSpan.Zero)
                throw new ArgumentException("Maintenance interval must be positive", nameof(maintenanceInterval));
            
            if (lastMaintenanceCost < 0)
                throw new ArgumentException("Maintenance cost cannot be negative", nameof(lastMaintenanceCost));
            
            if (maintenanceCount < 0)
                throw new ArgumentException("Maintenance count cannot be negative", nameof(maintenanceCount));

            LastMaintenance = lastMaintenance;
            NextScheduledMaintenance = nextScheduledMaintenance;
            MaintenanceInterval = maintenanceInterval;
            LastMaintenanceCost = lastMaintenanceCost;
            LastMaintenanceType = lastMaintenanceType;
            MaintenanceCount = maintenanceCount;
            Status = status;
            MaintenanceNotes = maintenanceNotes ?? new Dictionary<string, object>();
        }

        public static MaintenanceInfo CreateNew(TimeSpan interval, MaintenanceType type = MaintenanceType.Preventive)
            => new MaintenanceInfo(DateTime.UtcNow, DateTime.UtcNow + interval, interval, 0, type, 0, MaintenanceStatus.Scheduled);

        public bool IsOverdue => NextScheduledMaintenance.HasValue && DateTime.UtcNow > NextScheduledMaintenance.Value;
        public bool IsDueSoon => NextScheduledMaintenance.HasValue && 
                                DateTime.UtcNow.AddDays(7) > NextScheduledMaintenance.Value;
        public TimeSpan TimeSinceLastMaintenance => DateTime.UtcNow - LastMaintenance;
        public TimeSpan TimeUntilNextMaintenance => NextScheduledMaintenance.HasValue 
            ? NextScheduledMaintenance.Value - DateTime.UtcNow 
            : TimeSpan.MaxValue;

        public MaintenanceInfo ScheduleNextMaintenance(DateTime nextDate)
            => new MaintenanceInfo(LastMaintenance, nextDate, MaintenanceInterval, LastMaintenanceCost, LastMaintenanceType, MaintenanceCount, MaintenanceStatus.Scheduled, MaintenanceNotes);

        public MaintenanceInfo CompleteMaintenance(MaintenanceType type, decimal cost, string notes = null)
        {
            var newNotes = new Dictionary<string, object>(MaintenanceNotes);
            if (!string.IsNullOrEmpty(notes))
            {
                newNotes[$"Maintenance_{DateTime.UtcNow:yyyyMMdd}"] = notes;
            }

            return new MaintenanceInfo(
                DateTime.UtcNow,
                DateTime.UtcNow + MaintenanceInterval,
                MaintenanceInterval,
                cost,
                type,
                MaintenanceCount + 1,
                MaintenanceStatus.Completed,
                newNotes
            );
        }

        public MaintenanceInfo WithStatus(MaintenanceStatus newStatus)
            => new MaintenanceInfo(LastMaintenance, NextScheduledMaintenance, MaintenanceInterval, LastMaintenanceCost, LastMaintenanceType, MaintenanceCount, newStatus, MaintenanceNotes);

        public bool Equals(MaintenanceInfo other)
            => LastMaintenance == other.LastMaintenance &&
               NextScheduledMaintenance == other.NextScheduledMaintenance &&
               MaintenanceInterval == other.MaintenanceInterval &&
               LastMaintenanceCost == other.LastMaintenanceCost &&
               LastMaintenanceType == other.LastMaintenanceType &&
               MaintenanceCount == other.MaintenanceCount &&
               Status == other.Status;

        public override bool Equals(object obj) 
            => obj is MaintenanceInfo other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(LastMaintenance, NextScheduledMaintenance, MaintenanceInterval, LastMaintenanceCost, LastMaintenanceType, MaintenanceCount, Status);

        public override string ToString() 
            => $"Last: {LastMaintenance:yyyy-MM-dd}, Next: {NextScheduledMaintenance:yyyy-MM-dd}, Status: {Status}";
    }

    public enum MaintenanceType
    {
        Inspection,
        Preventive,
        Corrective,
        Emergency,
        Predictive,
        Calibration,
        Cleaning,
        Replacement
    }

    public enum MaintenanceStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Overdue,
        Cancelled
    }
}