using System;
using NUnit.Framework;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.ValueObjects;
using DigitalTwin.Core.Metadata;

namespace DigitalTwin.Tests.EditMode.Core.Entities
{
    /// <summary>
    /// Building Entity Tests
    /// 
    /// Architectural Intent:
    /// - Tests domain logic and business rules for Building entity
    /// - Validates invariants and state transitions
    /// - Ensures immutability and consistency
    /// - Tests edge cases and error conditions
    /// 
    /// Key Testing Decisions:
    /// 1. Pure domain logic testing (no Unity dependencies)
    /// 2. Comprehensive coverage of all public methods
    /// 3. Validation of all invariants and constraints
    /// 4. Testing of immutability and new instance creation
    /// </summary>
    [TestFixture]
    public class BuildingTests
    {
        private Building _testBuilding;
        private BuildingMetadata _testMetadata;

        [SetUp]
        public void Setup()
        {
            _testMetadata = CreateTestMetadata();
            _testBuilding = CreateTestBuilding();
        }

        [Test]
        public void Constructor_WithValidParameters_CreatesBuilding()
        {
            // Arrange
            var buildingId = Guid.NewGuid();
            var name = "Test Building";
            var address = "123 Test St";
            var constructedDate = DateTime.UtcNow;

            // Act
            var building = new Building(buildingId, name, address, _testMetadata, constructedDate);

            // Assert
            Assert.That(building.Id, Is.EqualTo(buildingId));
            Assert.That(building.Name, Is.EqualTo(name));
            Assert.That(building.Address, Is.EqualTo(address));
            Assert.That(building.Metadata, Is.EqualTo(_testMetadata));
            Assert.That(building.ConstructedDate, Is.EqualTo(constructedDate));
            Assert.That(building.Status, Is.EqualTo(BuildingStatus.Operational));
            Assert.That(building.Floors, Is.Empty);
            Assert.That(building.Events, Is.Empty);
        }

        [Test]
        public void Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            var buildingId = Guid.NewGuid();
            var address = "123 Test St";
            var constructedDate = DateTime.UtcNow;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new Building(buildingId, null, address, _testMetadata, constructedDate));
        }

        [Test]
        public void Constructor_WithNullAddress_ThrowsArgumentNullException()
        {
            // Arrange
            var buildingId = Guid.NewGuid();
            var name = "Test Building";
            var constructedDate = DateTime.UtcNow;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new Building(buildingId, name, null, _testMetadata, constructedDate));
        }

        [Test]
        public void Constructor_WithNullMetadata_ThrowsArgumentNullException()
        {
            // Arrange
            var buildingId = Guid.NewGuid();
            var name = "Test Building";
            var address = "123 Test St";
            var constructedDate = DateTime.UtcNow;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new Building(buildingId, name, address, null, constructedDate));
        }

        [Test]
        public void AddFloor_WithValidFloor_ReturnsBuildingWithFloor()
        {
            // Arrange
            var floor = CreateTestFloor();

            // Act
            var result = _testBuilding.AddFloor(floor);

            // Assert
            Assert.That(result.Floors.Count, Is.EqualTo(1));
            Assert.That(result.Floors[0], Is.EqualTo(floor));
            Assert.That(result.Events.Count, Is.EqualTo(1));
            Assert.That(_testBuilding.Floors, Is.Empty); // Original unchanged
        }

        [Test]
        public void AddFloor_WithDuplicateFloorNumber_ThrowsInvalidOperationException()
        {
            // Arrange
            var floor1 = CreateTestFloor();
            var floor2 = CreateTestFloor(); // Same floor number

            // Act
            var buildingWithFloor1 = _testBuilding.AddFloor(floor1);

            // Assert
            Assert.Throws<InvalidOperationException>(() => 
                buildingWithFloor1.AddFloor(floor2));
        }

        [Test]
        public void AddFloor_WhenInMaintenance_ThrowsInvalidOperationException()
        {
            // Arrange
            var floor = CreateTestFloor();
            var maintenanceBuilding = _testBuilding.SetMaintenanceMode(true);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                maintenanceBuilding.AddFloor(floor));
        }

        [Test]
        public void RemoveFloor_WithValidFloorId_ReturnsBuildingWithoutFloor()
        {
            // Arrange
            var floor = CreateTestFloor();
            var buildingWithFloor = _testBuilding.AddFloor(floor);

            // Act
            var result = buildingWithFloor.RemoveFloor(floor.Id);

            // Assert
            Assert.That(result.Floors.Count, Is.EqualTo(0));
            Assert.That(result.Events.Count, Is.EqualTo(2)); // Add + Remove
        }

        [Test]
        public void RemoveFloor_WithNonExistentFloorId_ThrowsArgumentException()
        {
            // Arrange
            var nonExistentFloorId = Guid.NewGuid();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _testBuilding.RemoveFloor(nonExistentFloorId));
        }

        [Test]
        public void RemoveFloor_WhenOnlyOneFloor_ThrowsInvalidOperationException()
        {
            // Arrange
            var floor = CreateTestFloor();
            var buildingWithFloor = _testBuilding.AddFloor(floor);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                buildingWithFloor.RemoveFloor(floor.Id));
        }

        [Test]
        public void SetMaintenanceMode_WithValidMode_ReturnsBuildingWithNewStatus()
        {
            // Act
            var result = _testBuilding.SetMaintenanceMode(true);

            // Assert
            Assert.That(result.Status, Is.EqualTo(BuildingStatus.Maintenance));
            Assert.That(result.Events.Count, Is.EqualTo(1));
            Assert.That(_testBuilding.Status, Is.EqualTo(BuildingStatus.Operational)); // Original unchanged
        }

        [Test]
        public void SetMaintenanceMode_ToggleToOperational_ReturnsBuildingWithOperationalStatus()
        {
            // Arrange
            var maintenanceBuilding = _testBuilding.SetMaintenanceMode(true);

            // Act
            var result = maintenanceBuilding.SetMaintenanceMode(false);

            // Assert
            Assert.That(result.Status, Is.EqualTo(BuildingStatus.Operational));
            Assert.That(result.Events.Count, Is.EqualTo(2)); // Enable + Disable
        }

        [Test]
        public void GetFloor_WithExistingFloorId_ReturnsFloor()
        {
            // Arrange
            var floor = CreateTestFloor();
            var buildingWithFloor = _testBuilding.AddFloor(floor);

            // Act
            var result = buildingWithFloor.GetFloor(floor.Id);

            // Assert
            Assert.That(result, Is.EqualTo(floor));
        }

        [Test]
        public void GetFloor_WithNonExistentFloorId_ReturnsNull()
        {
            // Arrange
            var nonExistentFloorId = Guid.NewGuid();

            // Act
            var result = _testBuilding.GetFloor(nonExistentFloorId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetFloorByNumber_WithExistingFloorNumber_ReturnsFloor()
        {
            // Arrange
            var floor = CreateTestFloor();
            var buildingWithFloor = _testBuilding.AddFloor(floor);

            // Act
            var result = buildingWithFloor.GetFloorByNumber(floor.Number);

            // Assert
            Assert.That(result, Is.EqualTo(floor));
        }

        [Test]
        public void GetFloorByNumber_WithNonExistentFloorNumber_ReturnsNull()
        {
            // Arrange
            var nonExistentFloorNumber = 999;

            // Act
            var result = _testBuilding.GetFloorByNumber(nonExistentFloorNumber);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TotalArea_WithFloors_ReturnsSumOfFloorAreas()
        {
            // Arrange
            var floor1 = CreateTestFloor(1, 100m);
            var floor2 = CreateTestFloor(2, 200m);
            var buildingWithFloors = _testBuilding.AddFloor(floor1).AddFloor(floor2);

            // Act
            var result = buildingWithFloors.TotalArea;

            // Assert
            Assert.That(result, Is.EqualTo(300m));
        }

        [Test]
        public void TotalOccupancy_WithFloors_ReturnsSumOfFloorOccupancies()
        {
            // Arrange
            var floor1 = CreateTestFloor(1, 100m, 50);
            var floor2 = CreateTestFloor(2, 200m, 75);
            var buildingWithFloors = _testBuilding.AddFloor(floor1).AddFloor(floor2);

            // Act
            var result = buildingWithFloors.TotalOccupancy;

            // Assert
            Assert.That(result, Is.EqualTo(125));
        }

        [Test]
        public void GetAllEquipment_WithFloorsContainingEquipment_ReturnsAllEquipment()
        {
            // Arrange
            var equipment1 = CreateTestEquipment();
            var equipment2 = CreateTestEquipment();
            var room1 = CreateTestRoom(new[] { equipment1 });
            var room2 = CreateTestRoom(new[] { equipment2 });
            var floor1 = CreateTestFloor(1, 100m, 50, new[] { room1 });
            var floor2 = CreateTestFloor(2, 200m, 75, new[] { room2 });
            var buildingWithFloors = _testBuilding.AddFloor(floor1).AddFloor(floor2);

            // Act
            var result = buildingWithFloors.GetAllEquipment();

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Contains.Item(equipment1));
            Assert.That(result, Contains.Item(equipment2));
        }

        [Test]
        public void GetAllSensors_WithFloorsContainingSensors_ReturnsAllSensors()
        {
            // Arrange
            var sensor1 = CreateTestSensor();
            var sensor2 = CreateTestSensor();
            var room1 = CreateTestRoom(sensors: new[] { sensor1 });
            var room2 = CreateTestRoom(sensors: new[] { sensor2 });
            var floor1 = CreateTestFloor(1, 100m, 50, new[] { room1 });
            var floor2 = CreateTestFloor(2, 200m, 75, new[] { room2 });
            var buildingWithFloors = _testBuilding.AddFloor(floor1).AddFloor(floor2);

            // Act
            var result = buildingWithFloors.GetAllSensors();

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Contains.Item(sensor1));
            Assert.That(result, Contains.Item(sensor2));
        }

        // Helper Methods
        private BuildingMetadata CreateTestMetadata()
        {
            var location = new GeoLocation(40.7128m, -74.0060m, 0m);
            return new BuildingMetadata(
                "Test Building",
                BuildingCategory.Commercial,
                "Test Architect",
                2020,
                10000m,
                "Test Owner",
                "test@example.com",
                location,
                BuildingCertification.None
            );
        }

        private Building CreateTestBuilding()
        {
            var buildingId = Guid.NewGuid();
            return new Building(buildingId, "Test Building", "123 Test St", _testMetadata, DateTime.UtcNow);
        }

        private Floor CreateTestFloor(int number = 1, decimal area = 100m, int maxOccupancy = 50, Room[] rooms = null)
        {
            var floorId = Guid.NewGuid();
            var floorMetadata = new FloorMetadata("Test Floor", FloorMaterial.Concrete, 500m, FireRating.OneHour, true, 2);
            var floor = new Floor(floorId, number, $"Floor {number}", FloorType.Office, 3.5m, floorMetadata);

            if (rooms != null)
            {
                foreach (var room in rooms)
                {
                    floor = floor.AddRoom(room);
                }
            }

            return floor;
        }

        private Room CreateTestRoom(Equipment[] equipment = null, Sensor[] sensors = null)
        {
            var roomId = Guid.NewGuid();
            var roomMetadata = new RoomMetadata("Test Room", RoomMaterial.Drywall, true, 2, VentilationType.Mechanical, AccessibilityFeatures.None);
            var room = new Room(roomId, 1, "Room 1", RoomType.Office, 50m, 10, roomMetadata);

            if (equipment != null)
            {
                foreach (var eq in equipment)
                {
                    room = room.AddEquipment(eq);
                }
            }

            if (sensors != null)
            {
                foreach (var sensor in sensors)
                {
                    room = room.AddSensor(sensor);
                }
            }

            return room;
        }

        private Equipment CreateTestEquipment()
        {
            var equipmentId = Guid.NewGuid();
            var equipmentMetadata = new EquipmentMetadata(
                "Test Equipment",
                "TEST-001",
                "1.0",
                "Test Manufacturer",
                EquipmentCategory.HVAC,
                EnergyEfficiencyClass.B,
                new WarrantyInfo(DateTime.UtcNow, DateTime.UtcNow.AddYears(5), "Test Manufacturer", WarrantyType.Full),
                ComplianceStandards.UL
            );
            return new Equipment(equipmentId, "Test Equipment", "Model X", "Test Manufacturer", EquipmentType.HVAC, 1500m, equipmentMetadata, DateTime.UtcNow);
        }

        private Sensor CreateTestSensor()
        {
            var sensorId = Guid.NewGuid();
            var operatingRange = new OperatingRange(-40m, 80m, "°C");
            var sensorMetadata = new SensorMetadata(
                "Test Sensor",
                "I2C",
                SensorCategory.Environmental,
                95m,
                operatingRange,
                CalibrationInterval.Monthly,
                "Test Sensor Manufacturer"
            );
            return new Sensor(sensorId, "Test Sensor", "Model Y", "Test Sensor Manufacturer", SensorType.Temperature, sensorMetadata, DateTime.UtcNow, TimeSpan.FromMinutes(5));
        }
    }
}