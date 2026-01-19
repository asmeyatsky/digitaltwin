using System;

namespace DigitalTwin.Core.ValueObjects
{
    /// <summary>
    /// Position Value Object
    /// 
    /// Architectural Intent:
    /// - Represents 3D spatial coordinates within building context
    /// - Provides immutable position calculations and transformations
    /// - Encapsulates position validation and coordinate system logic
    /// - Supports relative positioning and distance calculations
    /// 
    /// Invariants:
    /// - Position values must be finite (not NaN or Infinity)
    /// - Building coordinates must be non-negative
    /// - Floor number must be integer
    /// </summary>
    public readonly struct Position : IEquatable<Position>
    {
        public decimal X { get; }
        public decimal Y { get; }
        public decimal Z { get; }
        public int Floor { get; }
        public string BuildingId { get; }

        public Position(decimal x, decimal y, decimal z, int floor, string buildingId)
        {
            if (!IsValidCoordinate(x) || !IsValidCoordinate(y) || !IsValidCoordinate(z))
                throw new ArgumentException("Position coordinates must be finite numbers");
            if (floor < 0)
                throw new ArgumentException("Floor number cannot be negative");
            if (string.IsNullOrEmpty(buildingId))
                throw new ArgumentNullException(nameof(buildingId));

            X = x;
            Y = y;
            Z = z;
            Floor = floor;
            BuildingId = buildingId;
        }

        public static Position Origin(string buildingId) => new Position(0, 0, 0, 0, buildingId);
        
        public Position WithX(decimal newX) => new Position(newX, Y, Z, Floor, BuildingId);
        public Position WithY(decimal newY) => new Position(X, newY, Z, Floor, BuildingId);
        public Position WithZ(decimal newZ) => new Position(X, Y, newZ, Floor, BuildingId);
        public Position WithFloor(int newFloor) => new Position(X, Y, Z, newFloor, BuildingId);

        public Position Offset(decimal dx, decimal dy, decimal dz) 
            => new Position(X + dx, Y + dy, Z + dz, Floor, BuildingId);

        public Position MoveToFloor(int targetFloor) 
            => new Position(X, Y, Z, targetFloor, BuildingId);

        public decimal DistanceTo(Position other)
        {
            if (BuildingId != other.BuildingId)
                throw new ArgumentException("Cannot calculate distance between different buildings");

            var dx = X - other.X;
            var dy = Y - other.Y;
            var dz = Z - other.Z;
            var floorHeight = 3.5m; // Standard floor height in meters
            var floorDiff = (Floor - other.Floor) * floorHeight;

            return (decimal)Math.Sqrt((double)(dx * dx + dy * dy + dz * dz + floorDiff * floorDiff));
        }

        public bool IsNear(Position other, decimal threshold) 
            => DistanceTo(other) <= threshold;

        public bool IsSameFloor(Position other) 
            => Floor == other.Floor && BuildingId == other.BuildingId;

        public override string ToString() 
            => $"Building:{BuildingId}, Floor:{Floor}, X:{X}, Y:{Y}, Z:{Z}";

        public bool Equals(Position other) 
            => X == other.X && Y == other.Y && Z == other.Z && Floor == other.Floor && BuildingId == other.BuildingId;

        public override bool Equals(object obj) 
            => obj is Position other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(X, Y, Z, Floor, BuildingId);

        public static bool operator ==(Position left, Position right) 
            => left.Equals(right);

        public static bool operator !=(Position left, Position right) 
            => !left.Equals(right);

        private static bool IsValidCoordinate(decimal value)
        {
            return !decimal.IsNaN(value) && !decimal.IsInfinity(value);
        }
    }
}