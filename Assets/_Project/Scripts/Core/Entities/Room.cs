using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DigitalTwin.Core.Entities
{
    /// <summary>
    /// Room Entity
    /// 
    /// Architectural Intent:
    /// - Represents a physical room within a floor
    /// - Manages equipment and sensor relationships
    /// - Encapsulates room-level business rules and environmental conditions
    /// - Provides room monitoring and control capabilities
    /// 
    /// Invariants:
    /// - Room area must be positive
    /// - Room number must be unique within floor
    /// - Max occupancy cannot be negative
    /// - Equipment and sensors must be compatible with room type
    /// </summary>
    public class Room
    {
        private readonly List<Equipment> _equipment = new List<Equipment>();
        private readonly List<Sensor> _sensors = new List<Sensor>();

        public Guid Id { get; }
        public int Number { get; }
        public string Name { get; }
        public RoomType Type { get; }
        public decimal Area { get; }
        public int MaxOccupancy { get; }
        public bool IsOccupied { get; private set; }
        public RoomMetadata Metadata { get; }
        
        public ReadOnlyCollection<Equipment> Equipment => _equipment.AsReadOnly();
        public ReadOnlyCollection<Sensor> Sensors => _sensors.AsReadOnly();
        public EnvironmentalConditions CurrentConditions { get; private set; }

        public Room(Guid id, int number, string name, RoomType type, decimal area, int maxOccupancy, RoomMetadata metadata)
        {
            if (area <= 0)
                throw new ArgumentException("Room area must be positive", nameof(area));
            if (maxOccupancy < 0)
                throw new ArgumentException("Max occupancy cannot be negative", nameof(maxOccupancy));

            Id = id;
            Number = number;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            Area = area;
            MaxOccupancy = maxOccupancy;
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            IsOccupied = false;
            CurrentConditions = EnvironmentalConditions.Default;
        }

        public Room AddEquipment(Equipment equipment)
        {
            if (!IsEquipmentCompatible(equipment))
                throw new InvalidOperationException($"Equipment {equipment.Type} is not compatible with room type {Type}");

            var newRoom = CloneWithNewEquipment(equipment);
            newRoom._equipment.Add(equipment);
            
            return newRoom;
        }

        public Room RemoveEquipment(Guid equipmentId)
        {
            var equipment = _equipment.Find(e => e.Id == equipmentId);
            if (equipment == null)
                throw new ArgumentException($"Equipment with ID {equipmentId} not found");

            var newRoom = CloneWithoutEquipment(equipmentId);
            newRoom._equipment.Remove(equipment);
            
            return newRoom;
        }

        public Room AddSensor(Sensor sensor)
        {
            if (!IsSensorCompatible(sensor))
                throw new InvalidOperationException($"Sensor {sensor.Type} is not compatible with room type {Type}");

            var newRoom = CloneWithNewSensor(sensor);
            newRoom._sensors.Add(sensor);
            
            return newRoom;
        }

        public Room RemoveSensor(Guid sensorId)
        {
            var sensor = _sensors.Find(s => s.Id == sensorId);
            if (sensor == null)
                throw new ArgumentException($"Sensor with ID {sensorId} not found");

            var newRoom = CloneWithoutSensor(sensorId);
            newRoom._sensors.Remove(sensor);
            
            return newRoom;
        }

        public Room SetOccupied(bool occupied)
        {
            if (occupied && !IsOccupied)
            {
                var newRoom = CloneWithOccupancy(true);
                newRoom.IsOccupied = true;
                return newRoom;
            }
            else if (!occupied && IsOccupied)
            {
                var newRoom = CloneWithOccupancy(false);
                newRoom.IsOccupied = false;
                return newRoom;
            }
            
            return this;
        }

        public Room UpdateEnvironmentalConditions(EnvironmentalConditions conditions)
        {
            var newRoom = CloneWithNewConditions(conditions);
            newRoom.CurrentConditions = conditions;
            return newRoom;
        }

        public Equipment GetEquipment(Guid equipmentId)
        {
            return _equipment.Find(e => e.Id == equipmentId);
        }

        public Sensor GetSensor(Guid sensorId)
        {
            return _sensors.Find(s => s.Id == sensorId);
        }

        public IEnumerable<Equipment> GetEquipmentByType(EquipmentType type)
        {
            return _equipment.Where(e => e.Type == type);
        }

        public IEnumerable<Sensor> GetSensorsByType(SensorType type)
        {
            return _sensors.Where(s => s.Type == type);
        }

        public bool HasEquipmentType(EquipmentType type)
        {
            return _equipment.Any(e => e.Type == type);
        }

        public bool HasSensorType(SensorType type)
        {
            return _sensors.Any(s => s.Type == type);
        }

        public decimal GetEquipmentPowerConsumption()
        {
            return _equipment.Sum(e => e.PowerConsumption);
        }

        private bool IsEquipmentCompatible(Equipment equipment)
        {
            return Type switch
            {
                RoomType.Office => equipment.Type is EquipmentType.Computer or EquipmentType.Printer or EquipmentType.Projector,
                RoomType.ServerRoom => equipment.Type is EquipmentType.Server or EquipmentType.NetworkSwitch or EquipmentType.UPS,
                RoomType.Kitchen => equipment.Type is EquipmentType.Refrigerator or EquipmentType.Oven or EquipmentType.Dishwasher,
                RoomType.Conference => equipment.Type is EquipmentType.Projector or EquipmentType.VideoConference,
                RoomType.Laboratory => equipment.Type is EquipmentType.FumeHood or EquipmentType.SafetyEquipment,
                _ => true
            };
        }

        private bool IsSensorCompatible(Sensor sensor)
        {
            return Type switch
            {
                RoomType.Office => sensor.Type is SensorType.Temperature or SensorType.Humidity or SensorType.Motion or SensorType.Light,
                RoomType.ServerRoom => sensor.Type is SensorType.Temperature or SensorType.Humidity or SensorType.Power,
                RoomType.Laboratory => sensor.Type is SensorType.Temperature or SensorType.Humidity or SensorType.AirQuality or SensorType.Pressure,
                _ => true
            };
        }

        private Room CloneWithNewEquipment(Equipment equipment)
        {
            var clone = new Room(Id, Number, Name, Type, Area, MaxOccupancy, Metadata);
            clone._equipment.AddRange(_equipment);
            clone._sensors.AddRange(_sensors);
            clone.IsOccupied = IsOccupied;
            clone.CurrentConditions = CurrentConditions;
            return clone;
        }

        private Room CloneWithoutEquipment(Guid equipmentId)
        {
            var clone = new Room(Id, Number, Name, Type, Area, MaxOccupancy, Metadata);
            clone._equipment.AddRange(_equipment.Where(e => e.Id != equipmentId));
            clone._sensors.AddRange(_sensors);
            clone.IsOccupied = IsOccupied;
            clone.CurrentConditions = CurrentConditions;
            return clone;
        }

        private Room CloneWithNewSensor(Sensor sensor)
        {
            var clone = new Room(Id, Number, Name, Type, Area, MaxOccupancy, Metadata);
            clone._equipment.AddRange(_equipment);
            clone._sensors.AddRange(_sensors);
            clone.IsOccupied = IsOccupied;
            clone.CurrentConditions = CurrentConditions;
            return clone;
        }

        private Room CloneWithoutSensor(Guid sensorId)
        {
            var clone = new Room(Id, Number, Name, Type, Area, MaxOccupancy, Metadata);
            clone._equipment.AddRange(_equipment);
            clone._sensors.AddRange(_sensors.Where(s => s.Id != sensorId));
            clone.IsOccupied = IsOccupied;
            clone.CurrentConditions = CurrentConditions;
            return clone;
        }

        private Room CloneWithOccupancy(bool occupied)
        {
            var clone = new Room(Id, Number, Name, Type, Area, MaxOccupancy, Metadata);
            clone._equipment.AddRange(_equipment);
            clone._sensors.AddRange(_sensors);
            clone.IsOccupied = occupied;
            clone.CurrentConditions = CurrentConditions;
            return clone;
        }

        private Room CloneWithNewConditions(EnvironmentalConditions conditions)
        {
            var clone = new Room(Id, Number, Name, Type, Area, MaxOccupancy, Metadata);
            clone._equipment.AddRange(_equipment);
            clone._sensors.AddRange(_sensors);
            clone.IsOccupied = IsOccupied;
            clone.CurrentConditions = conditions;
            return clone;
        }
    }

    public enum RoomType
    {
        Office,
        Conference,
        ServerRoom,
        Kitchen,
        Laboratory,
        Storage,
        Restroom,
        Lobby,
        Mechanical,
        Electrical,
        Custom
    }
}