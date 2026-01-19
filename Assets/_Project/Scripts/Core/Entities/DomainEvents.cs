using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.Entities
{
    /// <summary>
    /// Base class for all domain events in the digital twin system
    /// 
    /// Architectural Intent:
    /// - Provides immutable event representation for domain state changes
    /// - Enables event sourcing and audit trail capabilities
    /// - Supports loose coupling between bounded contexts
    /// - Facilitates reactive programming patterns
    /// 
    /// Invariants:
    /// - Events are immutable once created
    /// - All events must have a timestamp
    /// - Event IDs must be unique across the system
    /// </summary>
    public abstract class DomainEvent
    {
        public Guid Id { get; }
        public Guid AggregateId { get; }
        public DateTime OccurredAt { get; }
        public string EventType { get; }
        public Dictionary<string, object> Metadata { get; }

        protected DomainEvent(Guid aggregateId, string eventType)
        {
            Id = Guid.NewGuid();
            AggregateId = aggregateId;
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            OccurredAt = DateTime.UtcNow;
            Metadata = new Dictionary<string, object>();
        }

        protected DomainEvent(Guid aggregateId, string eventType, Dictionary<string, object> metadata)
            : this(aggregateId, eventType)
        {
            if (metadata != null)
            {
                Metadata = new Dictionary<string, object>(metadata);
            }
        }

        public override string ToString()
        {
            return $"{EventType} - {Id} (Aggregate: {AggregateId}, Occurred: {OccurredAt:yyyy-MM-dd HH:mm:ss})";
        }
    }

    /// <summary>
    /// Building-specific domain events
    /// </summary>
    
    public class BuildingCreatedEvent : DomainEvent
    {
        public string BuildingName { get; }
        public string Address { get; }

        public BuildingCreatedEvent(Guid buildingId, string buildingName, string address)
            : base(buildingId, nameof(BuildingCreatedEvent))
        {
            BuildingName = buildingName ?? throw new ArgumentNullException(nameof(buildingName));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            
            Metadata["BuildingName"] = buildingName;
            Metadata["Address"] = address;
        }
    }

    public class FloorAddedEvent : DomainEvent
    {
        public Guid FloorId { get; }
        public int FloorNumber { get; }
        public decimal FloorArea { get; }

        public FloorAddedEvent(Guid buildingId, Guid floorId, int floorNumber, decimal floorArea)
            : base(buildingId, nameof(FloorAddedEvent))
        {
            FloorId = floorId;
            FloorNumber = floorNumber;
            FloorArea = floorArea;
            
            Metadata["FloorId"] = floorId;
            Metadata["FloorNumber"] = floorNumber;
            Metadata["FloorArea"] = floorArea;
        }
    }

    public class FloorAddedEvent : DomainEvent
    {
        public Guid FloorId { get; }

        public FloorAddedEvent(Guid buildingId, Guid floorId)
            : base(buildingId, nameof(FloorAddedEvent))
        {
            FloorId = floorId;
            Metadata["FloorId"] = floorId;
        }
    }

    public class FloorRemovedEvent : DomainEvent
    {
        public Guid FloorId { get; }

        public FloorRemovedEvent(Guid buildingId, Guid floorId)
            : base(buildingId, nameof(FloorRemovedEvent))
        {
            FloorId = floorId;
            Metadata["FloorId"] = floorId;
        }
    }

    public class MaintenanceModeChangedEvent : DomainEvent
    {
        public bool InMaintenance { get; }

        public MaintenanceModeChangedEvent(Guid buildingId, bool inMaintenance)
            : base(buildingId, nameof(MaintenanceModeChangedEvent))
        {
            InMaintenance = inMaintenance;
            Metadata["InMaintenance"] = inMaintenance;
        }
    }

    public class RoomAddedEvent : DomainEvent
    {
        public Guid RoomId { get; }
        public Guid FloorId { get; }
        public string RoomName { get; }
        public string RoomType { get; }

        public RoomAddedEvent(Guid buildingId, Guid roomId, Guid floorId, string roomName, string roomType)
            : base(buildingId, nameof(RoomAddedEvent))
        {
            RoomId = roomId;
            FloorId = floorId;
            RoomName = roomName ?? throw new ArgumentNullException(nameof(roomName));
            RoomType = roomType ?? throw new ArgumentNullException(nameof(roomType));
            
            Metadata["RoomId"] = roomId;
            Metadata["FloorId"] = floorId;
            Metadata["RoomName"] = roomName;
            Metadata["RoomType"] = roomType;
        }
    }

    public class EquipmentAddedEvent : DomainEvent
    {
        public Guid EquipmentId { get; }
        public Guid RoomId { get; }
        public string EquipmentType { get; }
        public string EquipmentName { get; }

        public EquipmentAddedEvent(Guid buildingId, Guid equipmentId, Guid roomId, string equipmentType, string equipmentName)
            : base(buildingId, nameof(EquipmentAddedEvent))
        {
            EquipmentId = equipmentId;
            RoomId = roomId;
            EquipmentType = equipmentType ?? throw new ArgumentNullException(nameof(equipmentType));
            EquipmentName = equipmentName ?? throw new ArgumentNullException(nameof(equipmentName));
            
            Metadata["EquipmentId"] = equipmentId;
            Metadata["RoomId"] = roomId;
            Metadata["EquipmentType"] = equipmentType;
            Metadata["EquipmentName"] = equipmentName;
        }
    }

    public class SensorAddedEvent : DomainEvent
    {
        public Guid SensorId { get; }
        public Guid RoomId { get; }
        public string SensorType { get; }
        public string SensorName { get; }

        public SensorAddedEvent(Guid buildingId, Guid sensorId, Guid roomId, string sensorType, string sensorName)
            : base(buildingId, nameof(SensorAddedEvent))
        {
            SensorId = sensorId;
            RoomId = roomId;
            SensorType = sensorType ?? throw new ArgumentNullException(nameof(sensorType));
            SensorName = sensorName ?? throw new ArgumentNullException(nameof(sensorName));
            
            Metadata["SensorId"] = sensorId;
            Metadata["RoomId"] = roomId;
            Metadata["SensorType"] = sensorType;
            Metadata["SensorName"] = sensorName;
        }
    }

    public class EquipmentStatusChangedEvent : DomainEvent
    {
        public Guid EquipmentId { get; }
        public string OldStatus { get; }
        public string NewStatus { get; }

        public EquipmentStatusChangedEvent(Guid buildingId, Guid equipmentId, string oldStatus, string newStatus)
            : base(buildingId, nameof(EquipmentStatusChangedEvent))
        {
            EquipmentId = equipmentId;
            OldStatus = oldStatus ?? throw new ArgumentNullException(nameof(oldStatus));
            NewStatus = newStatus ?? throw new ArgumentNullException(nameof(newStatus));
            
            Metadata["EquipmentId"] = equipmentId;
            Metadata["OldStatus"] = oldStatus;
            Metadata["NewStatus"] = newStatus;
        }
    }

    public class AlertTriggeredEvent : DomainEvent
    {
        public Guid EntityId { get; }
        public string EntityType { get; }
        public string AlertType { get; }
        public string Severity { get; }
        public string Message { get; }
        public Dictionary<string, object> AlertData { get; }

        public AlertTriggeredEvent(Guid buildingId, Guid entityId, string entityType, string alertType, string severity, string message, Dictionary<string, object> alertData = null)
            : base(buildingId, nameof(AlertTriggeredEvent))
        {
            EntityId = entityId;
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
            AlertType = alertType ?? throw new ArgumentNullException(nameof(alertType));
            Severity = severity ?? throw new ArgumentNullException(nameof(severity));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            AlertData = alertData ?? new Dictionary<string, object>();
            
            Metadata["EntityId"] = entityId;
            Metadata["EntityType"] = entityType;
            Metadata["AlertType"] = alertType;
            Metadata["Severity"] = severity;
            Metadata["Message"] = message;
            
            foreach (var kvp in AlertData)
            {
                Metadata[$"Alert_{kvp.Key}"] = kvp.Value;
            }
        }
    }

    public class EnergyConsumptionRecordedEvent : DomainEvent
    {
        public Guid EquipmentId { get; }
        public decimal Consumption { get; }
        public string Unit { get; }
        public DateTime ReadingTime { get; }

        public EnergyConsumptionRecordedEvent(Guid buildingId, Guid equipmentId, decimal consumption, string unit, DateTime readingTime)
            : base(buildingId, nameof(EnergyConsumptionRecordedEvent))
        {
            EquipmentId = equipmentId;
            Consumption = consumption;
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            ReadingTime = readingTime;
            
            Metadata["EquipmentId"] = equipmentId;
            Metadata["Consumption"] = consumption;
            Metadata["Unit"] = unit;
            Metadata["ReadingTime"] = readingTime;
        }
    }

    public class EnvironmentalConditionsUpdatedEvent : DomainEvent
    {
        public Guid RoomId { get; }
        public DigitalTwin.Core.ValueObjects.Temperature Temperature { get; }
        public decimal Humidity { get; }
        public decimal AirQuality { get; }
        public DateTime ReadingTime { get; }

        public EnvironmentalConditionsUpdatedEvent(Guid buildingId, Guid roomId, DigitalTwin.Core.ValueObjects.Temperature temperature, decimal humidity, decimal airQuality, DateTime readingTime)
            : base(buildingId, nameof(EnvironmentalConditionsUpdatedEvent))
        {
            RoomId = roomId;
            Temperature = temperature;
            Humidity = humidity;
            AirQuality = airQuality;
            ReadingTime = readingTime;
            
            Metadata["RoomId"] = roomId;
            Metadata["Temperature"] = temperature.ToString();
            Metadata["Humidity"] = humidity;
            Metadata["AirQuality"] = airQuality;
            Metadata["ReadingTime"] = readingTime;
        }
    }
}