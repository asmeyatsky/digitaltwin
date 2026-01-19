using System;

namespace DigitalTwin.Core.Entities
{
    /// <summary>
    /// Equipment Entity
    /// 
    /// Architectural Intent:
    /// - Represents physical equipment within a room
    /// - Encapsulates equipment state, performance metrics, and maintenance requirements
    /// - Provides equipment monitoring and control capabilities
    /// - Maintains equipment lifecycle and operational history
    /// 
    /// Invariants:
    /// - Equipment power consumption cannot be negative
    /// - Equipment status transitions must follow defined state machine
    /// - Maintenance schedule cannot be in the past
    /// - Equipment cannot be operated if in failed state
    /// </summary>
    public class Equipment
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Model { get; }
        public string Manufacturer { get; }
        public EquipmentType Type { get; }
        public EquipmentStatus Status { get; private set; }
        public decimal PowerConsumption { get; }
        public OperationalMetrics Metrics { get; private set; }
        public MaintenanceInfo Maintenance { get; private set; }
        public EquipmentMetadata Metadata { get; }
        public DateTime InstalledDate { get; }
        public DateTime LastMaintenanceDate { get; private set; }

        public Equipment(Guid id, string name, string model, string manufacturer, EquipmentType type, 
                        decimal powerConsumption, EquipmentMetadata metadata, DateTime installedDate)
        {
            if (powerConsumption < 0)
                throw new ArgumentException("Power consumption cannot be negative", nameof(powerConsumption));

            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Manufacturer = manufacturer ?? throw new ArgumentNullException(nameof(manufacturer));
            Type = type;
            PowerConsumption = powerConsumption;
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            InstalledDate = installedDate;
            LastMaintenanceDate = installedDate;
            Status = EquipmentStatus.Operational;
            Metrics = OperationalMetrics.Default;
            Maintenance = MaintenanceInfo.Default;
        }

        public Equipment SetStatus(EquipmentStatus newStatus)
        {
            if (!IsValidStatusTransition(Status, newStatus))
                throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}");

            var newEquipment = CloneWithNewStatus(newStatus);
            newEquipment.Status = newStatus;
            
            if (newStatus == EquipmentStatus.Maintenance)
            {
                newEquipment.LastMaintenanceDate = DateTime.UtcNow;
            }
            
            return newEquipment;
        }

        public Equipment UpdateMetrics(OperationalMetrics metrics)
        {
            var newEquipment = CloneWithNewMetrics(metrics);
            newEquipment.Metrics = metrics;
            return newEquipment;
        }

        public Equipment ScheduleMaintenance(DateTime scheduledDate, string description)
        {
            if (scheduledDate < DateTime.UtcNow)
                throw new ArgumentException("Maintenance cannot be scheduled in the past");

            var newMaintenance = new MaintenanceInfo(scheduledDate, description, Maintenance.Type);
            var newEquipment = CloneWithNewMaintenance(newMaintenance);
            newEquipment.Maintenance = newMaintenance;
            
            return newEquipment;
        }

        public Equipment CompleteMaintenance(string notes)
        {
            var newEquipment = CloneWithNewStatus(EquipmentStatus.Operational);
            newEquipment.Status = EquipmentStatus.Operational;
            newEquipment.LastMaintenanceDate = DateTime.UtcNow;
            newEquipment.Maintenance = MaintenanceInfo.Default;
            
            return newEquipment;
        }

        public bool RequiresMaintenance()
        {
            return Maintenance.ScheduledDate <= DateTime.UtcNow || Status == EquipmentStatus.Failed;
        }

        public bool IsOperational()
        {
            return Status == EquipmentStatus.Operational;
        }

        public TimeSpan GetUptime()
        {
            return DateTime.UtcNow - InstalledDate;
        }

        public decimal GetEnergyConsumption(TimeSpan timePeriod)
        {
            return PowerConsumption * (decimal)timePeriod.TotalHours;
        }

        private bool IsValidStatusTransition(EquipmentStatus from, EquipmentStatus to)
        {
            return (from, to) switch
            {
                (EquipmentStatus.Operational, EquipmentStatus.Maintenance) => true,
                (EquipmentStatus.Operational, EquipmentStatus.Failed) => true,
                (EquipmentStatus.Operational, EquipmentStatus.Offline) => true,
                (EquipmentStatus.Maintenance, EquipmentStatus.Operational) => true,
                (EquipmentStatus.Failed, EquipmentStatus.Maintenance) => true,
                (EquipmentStatus.Failed, EquipmentStatus.Offline) => true,
                (EquipmentStatus.Offline, EquipmentStatus.Operational) => true,
                (EquipmentStatus.Offline, EquipmentStatus.Failed) => true,
                _ => false
            };
        }

        private Equipment CloneWithNewStatus(EquipmentStatus newStatus)
        {
            var clone = new Equipment(Id, Name, Model, Manufacturer, Type, PowerConsumption, Metadata, InstalledDate);
            clone.Status = newStatus;
            clone.Metrics = Metrics;
            clone.Maintenance = Maintenance;
            clone.LastMaintenanceDate = LastMaintenanceDate;
            return clone;
        }

        private Equipment CloneWithNewMetrics(OperationalMetrics metrics)
        {
            var clone = new Equipment(Id, Name, Model, Manufacturer, Type, PowerConsumption, Metadata, InstalledDate);
            clone.Status = Status;
            clone.Metrics = metrics;
            clone.Maintenance = Maintenance;
            clone.LastMaintenanceDate = LastMaintenanceDate;
            return clone;
        }

        private Equipment CloneWithNewMaintenance(MaintenanceInfo maintenance)
        {
            var clone = new Equipment(Id, Name, Model, Manufacturer, Type, PowerConsumption, Metadata, InstalledDate);
            clone.Status = Status;
            clone.Metrics = Metrics;
            clone.Maintenance = maintenance;
            clone.LastMaintenanceDate = LastMaintenanceDate;
            return clone;
        }
    }

    public enum EquipmentType
    {
        HVAC,
        Lighting,
        Computer,
        Server,
        NetworkSwitch,
        Printer,
        Scanner,
        Projector,
        VideoConference,
        Refrigerator,
        Oven,
        Dishwasher,
        WashingMachine,
        UPS,
        Generator,
        FumeHood,
        SafetyEquipment,
        AccessControl,
        Surveillance,
        Custom
    }

    public enum EquipmentStatus
    {
        Operational,
        Maintenance,
        Failed,
        Offline,
        Decommissioned
    }
}