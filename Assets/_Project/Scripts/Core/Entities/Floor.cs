using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DigitalTwin.Core.Entities
{
    /// <summary>
    /// Floor Entity
    /// 
    /// Architectural Intent:
    /// - Represents a physical floor within a building
    /// - Maintains room organization and floor-level metrics
    /// - Encapsulates floor-specific business rules
    /// - Provides floor-level analytics and monitoring
    /// 
    /// Invariants:
    /// - Floor must have at least one room
    /// - All room numbers must be unique within floor
    /// - Floor area must equal sum of all room areas
    /// - Floor number must be positive
    /// </summary>
    public class Floor
    {
        private readonly List<Room> _rooms = new List<Room>();

        public Guid Id { get; }
        public int Number { get; }
        public string Name { get; }
        public FloorType Type { get; }
        public decimal Height { get; }
        public FloorMetadata Metadata { get; }
        
        public ReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();
        public decimal TotalArea => CalculateTotalArea();
        public int TotalOccupancy => CalculateTotalOccupancy();
        public int RoomCount => _rooms.Count;

        public Floor(Guid id, int number, string name, FloorType type, decimal height, FloorMetadata metadata)
        {
            if (number <= 0)
                throw new ArgumentException("Floor number must be positive", nameof(number));
            if (height <= 0)
                throw new ArgumentException("Floor height must be positive", nameof(height));

            Id = id;
            Number = number;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            Height = height;
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        public Floor AddRoom(Room room)
        {
            if (_rooms.Exists(r => r.Number == room.Number))
                throw new InvalidOperationException($"Room {room.Number} already exists on floor {Number}");

            var newFloor = CloneWithNewRoom(room);
            newFloor._rooms.Add(room);
            
            return newFloor;
        }

        public Floor RemoveRoom(Guid roomId)
        {
            var room = _rooms.Find(r => r.Id == roomId);
            if (room == null)
                throw new ArgumentException($"Room with ID {roomId} not found");

            if (_rooms.Count == 1)
                throw new InvalidOperationException("Floor must have at least one room");

            var newFloor = CloneWithoutRoom(roomId);
            newFloor._rooms.Remove(room);
            
            return newFloor;
        }

        public Room GetRoom(Guid roomId)
        {
            return _rooms.Find(r => r.Id == roomId);
        }

        public Room GetRoomByNumber(int roomNumber)
        {
            return _rooms.Find(r => r.Number == roomNumber);
        }

        public IEnumerable<Room> GetRoomsByType(RoomType type)
        {
            return _rooms.Where(r => r.Type == type);
        }

        public decimal GetOccupiedArea()
        {
            return _rooms.Sum(r => r.Area);
        }

        public bool HasAvailableSpace()
        {
            return _rooms.Any(r => r.IsOccupied == false);
        }

        private Floor CloneWithNewRoom(Room room)
        {
            var clone = new Floor(Id, Number, Name, Type, Height, Metadata);
            clone._rooms.AddRange(_rooms);
            return clone;
        }

        private Floor CloneWithoutRoom(Guid roomId)
        {
            var clone = new Floor(Id, Number, Name, Type, Height, Metadata);
            clone._rooms.AddRange(_rooms.Where(r => r.Id != roomId));
            return clone;
        }

        private decimal CalculateTotalArea()
        {
            return _rooms.Sum(r => r.Area);
        }

        private int CalculateTotalOccupancy()
        {
            return _rooms.Sum(r => r.MaxOccupancy);
        }
    }

    public enum FloorType
    {
        Residential,
        Commercial,
        Industrial,
        Parking,
        Mechanical,
        MixedUse
    }
}