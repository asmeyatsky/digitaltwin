using System;

namespace DigitalTwin.Core.Metadata
{
    /// <summary>
    /// Building Metadata Value Object
    /// 
    /// Architectural Intent:
    /// - Represents descriptive and classification metadata for buildings
    /// - Provides immutable metadata for search and categorization
    /// - Encapsulates building characteristics and properties
    /// </summary>
    public readonly struct BuildingMetadata : IEquatable<BuildingMetadata>
    {
        public string Description { get; }
        public BuildingCategory Category { get; }
        public string Architect { get; }
        public int YearBuilt { get; }
        public decimal SquareFootage { get; }
        public string Owner { get; }
        public string ContactInfo { get; }
        public GeoLocation Location { get; }
        public BuildingCertification Certification { get; }

        public BuildingMetadata(string description, BuildingCategory category, string architect, 
                               int yearBuilt, decimal squareFootage, string owner, string contactInfo, 
                               GeoLocation location, BuildingCertification certification)
        {
            if (yearBuilt < 1800 || yearBuilt > DateTime.UtcNow.Year + 10)
                throw new ArgumentException("Invalid year built", nameof(yearBuilt));
            if (squareFootage <= 0)
                throw new ArgumentException("Square footage must be positive", nameof(squareFootage));

            Description = description ?? throw new ArgumentNullException(nameof(description));
            Category = category;
            Architect = architect ?? throw new ArgumentNullException(nameof(architect));
            YearBuilt = yearBuilt;
            SquareFootage = squareFootage;
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            ContactInfo = contactInfo ?? throw new ArgumentNullException(nameof(contactInfo));
            Location = location;
            Certification = certification;
        }

        public int GetBuildingAge() 
            => DateTime.UtcNow.Year - YearBuilt;

        public bool IsHistorical() 
            => GetBuildingAge() >= 50;

        public bool IsCertified() 
            => Certification != BuildingCertification.None;

        public BuildingMetadata WithOwner(string newOwner) 
            => new BuildingMetadata(Description, Category, Architect, YearBuilt, SquareFootage, newOwner, ContactInfo, Location, Certification);

        public bool Equals(BuildingMetadata other) 
            => Description == other.Description && Category == other.Category && Architect == other.Architect && 
               YearBuilt == other.YearBuilt && SquareFootage == other.SquareFootage && Owner == other.Owner && 
               ContactInfo == other.ContactInfo && Location.Equals(other.Location) && Certification == other.Certification;

        public override bool Equals(object obj) 
            => obj is BuildingMetadata other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Description, Category, Architect, YearBuilt, SquareFootage, Owner, ContactInfo, Location, Certification);

        public override string ToString() 
            => $"{Category} - {Owner} ({YearBuilt})";
    }

    public readonly struct FloorMetadata : IEquatable<FloorMetadata>
    {
        public string Description { get; }
        public FloorMaterial Material { get; }
        public decimal LoadCapacity { get; } // kg per square meter
        public FireRating FireRating { get; }
        public bool HasSprinklers { get; }
        public int EmergencyExits { get; }

        public FloorMetadata(string description, FloorMaterial material, decimal loadCapacity, 
                           FireRating fireRating, bool hasSprinklers, int emergencyExits)
        {
            if (loadCapacity <= 0)
                throw new ArgumentException("Load capacity must be positive", nameof(loadCapacity));
            if (emergencyExits < 0)
                throw new ArgumentException("Emergency exits cannot be negative", nameof(emergencyExits));

            Description = description ?? throw new ArgumentNullException(nameof(description));
            Material = material;
            LoadCapacity = loadCapacity;
            FireRating = fireRating;
            HasSprinklers = hasSprinklers;
            EmergencyExits = emergencyExits;
        }

        public bool IsFireSafe() 
            => FireRating >= FireRating.TwoHour && (HasSprinklers || EmergencyExits >= 2);

        public FloorMetadata WithSprinklers(bool hasSprinklers) 
            => new FloorMetadata(Description, Material, LoadCapacity, FireRating, hasSprinklers, EmergencyExits);

        public bool Equals(FloorMetadata other) 
            => Description == other.Description && Material == other.Material && 
               LoadCapacity == other.LoadCapacity && FireRating == other.FireRating && 
               HasSprinklers == other.HasSprinklers && EmergencyExits == other.EmergencyExits;

        public override bool Equals(object obj) 
            => obj is FloorMetadata other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Description, Material, LoadCapacity, FireRating, HasSprinklers, EmergencyExits);

        public override string ToString() 
            => $"{Material} floor, {FireRating} fire rating";
    }

    public readonly struct RoomMetadata : IEquatable<RoomMetadata>
    {
        public string Description { get; }
        public RoomMaterial Material { get; }
        public bool HasWindows { get; }
        public int WindowCount { get; }
        public VentilationType Ventilation { get; }
        public AccessibilityFeatures Accessibility { get; }

        public RoomMetadata(string description, RoomMaterial material, bool hasWindows, int windowCount, 
                          VentilationType ventilation, AccessibilityFeatures accessibility)
        {
            if (windowCount < 0)
                throw new ArgumentException("Window count cannot be negative", nameof(windowCount));

            Description = description ?? throw new ArgumentNullException(nameof(description));
            Material = material;
            HasWindows = hasWindows;
            WindowCount = hasWindows ? windowCount : 0;
            Ventilation = ventilation;
            Accessibility = accessibility;
        }

        public bool IsAccessible() 
            => Accessibility != AccessibilityFeatures.None;

        public bool HasNaturalLight() 
            => HasWindows && WindowCount > 0;

        public bool IsWellVentilated() 
            => Ventilation == VentilationType.Mechanical || (HasWindows && WindowCount >= 2);

        public RoomMetadata WithAccessibility(AccessibilityFeatures features) 
            => new RoomMetadata(Description, Material, HasWindows, WindowCount, Ventilation, features);

        public bool Equals(RoomMetadata other) 
            => Description == other.Description && Material == other.Material && 
               HasWindows == other.HasWindows && WindowCount == other.WindowCount && 
               Ventilation == other.Ventilation && Accessibility == other.Accessibility;

        public override bool Equals(object obj) 
            => obj is RoomMetadata other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Description, Material, HasWindows, WindowCount, Ventilation, Accessibility);

        public override string ToString() 
            => $"{Material} room, {Ventilation} ventilation";
    }

    public readonly struct EquipmentMetadata : IEquatable<EquipmentMetadata>
    {
        public string Description { get; }
        public string SerialNumber { get; }
        public string FirmwareVersion { get; }
        public EquipmentCategory Category { get; }
        public EnergyEfficiencyClass EfficiencyClass { get; }
        public WarrantyInfo Warranty { get; }
        public ComplianceStandards Compliance { get; }

        public EquipmentMetadata(string description, string serialNumber, string firmwareVersion, 
                                EquipmentCategory category, EnergyEfficiencyClass efficiencyClass, 
                                WarrantyInfo warranty, ComplianceStandards compliance)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            SerialNumber = serialNumber ?? throw new ArgumentNullException(nameof(serialNumber));
            FirmwareVersion = firmwareVersion ?? throw new ArgumentNullException(nameof(firmwareVersion));
            Category = category;
            EfficiencyClass = efficiencyClass;
            Warranty = warranty;
            Compliance = compliance;
        }

        public bool IsUnderWarranty() 
            => Warranty.ExpiryDate > DateTime.UtcNow;

        public bool IsCompliant() 
            => Compliance != ComplianceStandards.None;

        public EquipmentMetadata WithFirmwareVersion(string newVersion) 
            => new EquipmentMetadata(Description, SerialNumber, newVersion, Category, EfficiencyClass, Warranty, Compliance);

        public bool Equals(EquipmentMetadata other) 
            => Description == other.Description && SerialNumber == other.SerialNumber && 
               FirmwareVersion == other.FirmwareVersion && Category == other.Category && 
               EfficiencyClass == other.EfficiencyClass && Warranty.Equals(other.Warranty) && Compliance == other.Compliance;

        public override bool Equals(object obj) 
            => obj is EquipmentMetadata other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Description, SerialNumber, FirmwareVersion, Category, EfficiencyClass, Warranty, Compliance);

        public override string ToString() 
            => $"{Category} - {SerialNumber}";
    }

    public readonly struct SensorMetadata : IEquatable<SensorMetadata>
    {
        public string Description { get; }
        public string Protocol { get; }
        public SensorCategory Category { get; }
        public decimal Accuracy { get; }
        public OperatingRange Range { get; }
        public CalibrationInterval CalibrationInterval { get; }
        public string Manufacturer { get; }

        public SensorMetadata(string description, string protocol, SensorCategory category, 
                             decimal accuracy, OperatingRange range, CalibrationInterval calibrationInterval, 
                             string manufacturer)
        {
            if (accuracy < 0 || accuracy > 100)
                throw new ArgumentException("Accuracy must be between 0 and 100%", nameof(accuracy));

            Description = description ?? throw new ArgumentNullException(nameof(description));
            Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            Category = category;
            Accuracy = accuracy;
            Range = range;
            CalibrationInterval = calibrationInterval;
            Manufacturer = manufacturer ?? throw new ArgumentNullException(nameof(manufacturer));
        }

        public bool IsHighAccuracy() 
            => Accuracy >= 95;

        public bool RequiresFrequentCalibration() 
            => CalibrationInterval == CalibrationInterval.Daily || CalibrationInterval == CalibrationInterval.Weekly;

        public SensorMetadata WithAccuracy(decimal newAccuracy) 
            => new SensorMetadata(Description, Protocol, Category, newAccuracy, Range, CalibrationInterval, Manufacturer);

        public bool Equals(SensorMetadata other) 
            => Description == other.Description && Protocol == other.Protocol && Category == other.Category && 
               Accuracy == other.Accuracy && Range.Equals(other.Range) && CalibrationInterval == other.CalibrationInterval && 
               Manufacturer == other.Manufacturer;

        public override bool Equals(object obj) 
            => obj is SensorMetadata other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Description, Protocol, Category, Accuracy, Range, CalibrationInterval, Manufacturer);

        public override string ToString() 
            => $"{Category} sensor, {Accuracy}% accuracy";
    }

    // Supporting enums and value objects
    public readonly struct GeoLocation : IEquatable<GeoLocation>
    {
        public decimal Latitude { get; }
        public decimal Longitude { get; }
        public decimal Altitude { get; }

        public GeoLocation(decimal latitude, decimal longitude, decimal altitude = 0)
        {
            if (latitude < -90 || latitude > 90)
                throw new ArgumentException("Latitude must be between -90 and 90", nameof(latitude));
            if (longitude < -180 || longitude > 180)
                throw new ArgumentException("Longitude must be between -180 and 180", nameof(longitude));

            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }

        public bool Equals(GeoLocation other) 
            => Latitude == other.Latitude && Longitude == other.Longitude && Altitude == other.Altitude;

        public override bool Equals(object obj) 
            => obj is GeoLocation other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Latitude, Longitude, Altitude);

        public override string ToString() 
            => $"{Latitude}, {Longitude}";
    }

    public readonly struct WarrantyInfo : IEquatable<WarrantyInfo>
    {
        public DateTime StartDate { get; }
        public DateTime ExpiryDate { get; }
        public string Provider { get; }
        public WarrantyType Type { get; }

        public WarrantyInfo(DateTime startDate, DateTime expiryDate, string provider, WarrantyType type)
        {
            if (expiryDate <= startDate)
                throw new ArgumentException("Expiry date must be after start date", nameof(expiryDate));

            StartDate = startDate;
            ExpiryDate = expiryDate;
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Type = type;
        }

        public TimeSpan RemainingDuration() 
            => ExpiryDate > DateTime.UtcNow ? ExpiryDate - DateTime.UtcNow : TimeSpan.Zero;

        public bool Equals(WarrantyInfo other) 
            => StartDate == other.StartDate && ExpiryDate == other.ExpiryDate && Provider == other.Provider && Type == other.Type;

        public override bool Equals(object obj) 
            => obj is WarrantyInfo other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(StartDate, ExpiryDate, Provider, Type);

        public override string ToString() 
            => $"{Type} warranty from {Provider} until {ExpiryDate:yyyy-MM-dd}";
    }

    public readonly struct OperatingRange : IEquatable<OperatingRange>
    {
        public decimal Minimum { get; }
        public decimal Maximum { get; }
        public string Unit { get; }

        public OperatingRange(decimal minimum, decimal maximum, string unit)
        {
            if (maximum <= minimum)
                throw new ArgumentException("Maximum must be greater than minimum", nameof(maximum));
            if (string.IsNullOrEmpty(unit))
                throw new ArgumentNullException(nameof(unit));

            Minimum = minimum;
            Maximum = maximum;
            Unit = unit;
        }

        public bool IsInRange(decimal value) 
            => value >= Minimum && value <= Maximum;

        public bool Equals(OperatingRange other) 
            => Minimum == other.Minimum && Maximum == other.Maximum && Unit == other.Unit;

        public override bool Equals(object obj) 
            => obj is OperatingRange other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Minimum, Maximum, Unit);

        public override string ToString() 
            => $"{Minimum} - {Maximum} {Unit}";
    }
}