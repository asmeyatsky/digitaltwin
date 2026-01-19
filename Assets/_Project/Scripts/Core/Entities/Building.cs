using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DigitalTwin.Core.Entities
{
    /// <summary>
    /// Building Aggregate Root
    /// 
    /// Architectural Intent:
    /// - Represents the complete digital twin of a physical building
    /// - Maintains consistency boundaries for all building operations
    /// - Encapsulates business rules for building management
    /// - Provides domain events for external system integration
    /// 
    /// Invariants:
    /// - Building must have at least one floor
    /// - All floor numbers must be unique within building
    /// - Total area must equal sum of all floor areas
    /// - Building cannot be modified if in maintenance state
    /// </summary>
    public class Building
    {
        private readonly List<Floor> _floors = new List<Floor>();
        private readonly List<BuildingEvent> _events = new List<BuildingEvent>();

        public Guid Id { get; }
        public string Name { get; }
        public string Address { get; }
        public BuildingStatus Status { get; private set; }
        public DateTime ConstructedDate { get; }
        public BuildingMetadata Metadata { get; }
        
        public ReadOnlyCollection<Floor> Floors => _floors.AsReadOnly();
        public ReadOnlyCollection<BuildingEvent> Events => _events.AsReadOnly();
        public decimal TotalArea => CalculateTotalArea();
        public int TotalOccupancy => CalculateTotalOccupancy();

        public Building(Guid id, string name, string address, BuildingMetadata metadata, DateTime constructedDate)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            ConstructedDate = constructedDate;
            Status = BuildingStatus.Operational;
        }

        public Building AddFloor(Floor floor)
        {
            if (Status == BuildingStatus.Maintenance)
                throw new InvalidOperationException("Cannot modify building while in maintenance state");

            if (_floors.Exists(f => f.Number == floor.Number))
                throw new InvalidOperationException($"Floor {floor.Number} already exists");

            var newBuilding = CloneWithNewFloor(floor);
            newBuilding._floors.Add(floor);
            newBuilding._events.Add(new FloorAddedEvent(Id, floor.Id, DateTime.UtcNow));
            
            return newBuilding;
        }

        public Building RemoveFloor(Guid floorId)
        {
            if (Status == BuildingStatus.Maintenance)
                throw new InvalidOperationException("Cannot modify building while in maintenance state");

            var floor = _floors.Find(f => f.Id == floorId);
            if (floor == null)
                throw new ArgumentException($"Floor with ID {floorId} not found");

            if (_floors.Count == 1)
                throw new InvalidOperationException("Building must have at least one floor");

            var newBuilding = CloneWithoutFloor(floorId);
            newBuilding._floors.Remove(floor);
            newBuilding._events.Add(new FloorRemovedEvent(Id, floorId, DateTime.UtcNow));
            
            return newBuilding;
        }

        public Building SetMaintenanceMode(bool inMaintenance)
        {
            if (inMaintenance && Status != BuildingStatus.Maintenance)
            {
                var newBuilding = CloneWithStatus(BuildingStatus.Maintenance);
                newBuilding._events.Add(new MaintenanceModeChangedEvent(Id, true, DateTime.UtcNow));
                return newBuilding;
            }
            else if (!inMaintenance && Status == BuildingStatus.Maintenance)
            {
                var newBuilding = CloneWithStatus(BuildingStatus.Operational);
                newBuilding._events.Add(new MaintenanceModeChangedEvent(Id, false, DateTime.UtcNow));
                return newBuilding;
            }
            
            return this;
        }

        public Floor GetFloor(Guid floorId)
        {
            return _floors.Find(f => f.Id == floorId);
        }

        public Floor GetFloorByNumber(int floorNumber)
        {
            return _floors.Find(f => f.Number == floorNumber);
        }

        public IEnumerable<Equipment> GetAllEquipment()
        {
            foreach (var floor in _floors)
            {
                foreach (var room in floor.Rooms)
                {
                    foreach (var equipment in room.Equipment)
                    {
                        yield return equipment;
                    }
                }
            }
        }

        public IEnumerable<Sensor> GetAllSensors()
        {
            foreach (var floor in _floors)
            {
                foreach (var room in floor.Rooms)
                {
                    foreach (var sensor in room.Sensors)
                    {
                        yield return sensor;
                    }
                }
            }
        }

        private Building CloneWithNewFloor(Floor floor)
        {
            var clone = new Building(Id, Name, Address, Metadata, ConstructedDate);
            clone._floors.AddRange(_floors);
            clone._events.AddRange(_events);
            clone.Status = Status;
            return clone;
        }

        private Building CloneWithoutFloor(Guid floorId)
        {
            var clone = new Building(Id, Name, Address, Metadata, ConstructedDate);
            clone._floors.AddRange(_floors.Where(f => f.Id != floorId));
            clone._events.AddRange(_events);
            clone.Status = Status;
            return clone;
        }

        private Building CloneWithStatus(BuildingStatus newStatus)
        {
            var clone = new Building(Id, Name, Address, Metadata, ConstructedDate);
            clone._floors.AddRange(_floors);
            clone._events.AddRange(_events);
            clone.Status = newStatus;
            return clone;
        }

        private decimal CalculateTotalArea()
        {
            decimal total = 0;
            foreach (var floor in _floors)
            {
                total += floor.TotalArea;
            }
            return total;
        }

        private int CalculateTotalOccupancy()
        {
            int total = 0;
            foreach (var floor in _floors)
            {
                total += floor.TotalOccupancy;
            }
            return total;
        }
    }

    public enum BuildingStatus
    {
        Operational,
        Maintenance,
        Emergency,
        Decommissioned
    }
}