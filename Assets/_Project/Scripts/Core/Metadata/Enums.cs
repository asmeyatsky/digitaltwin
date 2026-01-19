namespace DigitalTwin.Core.Metadata
{
    /// <summary>
    /// Building Classification Enums
    /// </summary>
    public enum BuildingCategory
    {
        Residential,
        Commercial,
        Industrial,
        Institutional,
        MixedUse,
        Retail,
        Healthcare,
        Education,
        Hospitality,
        Government,
        Recreation,
        Storage,
        Utility,
        Custom
    }

    public enum BuildingCertification
    {
        None,
        LEED_Certified,
        LEED_Silver,
        LEED_Gold,
        LEED_Platinum,
        BREEAM_Pass,
        BREEAM_Good,
        BREEAM_VeryGood,
        BREEAM_Excellent,
        BREEAM_Outstanding,
        ENERGY_STAR,
        Passive_House,
        Net_Zero,
        WELL_Certified,
        WELL_Silver,
        WELL_Gold,
        WELL_Platinum
    }

    /// <summary>
    /// Floor Classification Enums
    /// </summary>
    public enum FloorMaterial
    {
        Concrete,
        Steel,
        Wood,
        Composite,
        Carpet,
        Tile,
        Vinyl,
        Linoleum,
        Rubber,
        Stone,
        Glass,
        Custom
    }

    public enum FireRating
    {
        None,
        ThirtyMinute,
        OneHour,
        TwoHour,
        ThreeHour,
        FourHour
    }

    /// <summary>
    /// Room Classification Enums
    /// </summary>
    public enum RoomMaterial
    {
        Drywall,
        Concrete,
        Brick,
        Wood,
        Glass,
        Metal,
        Plaster,
        Panel,
        Custom
    }

    public enum VentilationType
    {
        Natural,
        Mechanical,
        Hybrid,
        None
    }

    public enum AccessibilityFeatures
    {
        None,
        WheelchairAccessible,
        VisualAssistance,
        HearingAssistance,
        FullAccessibility,
        Custom
    }

    /// <summary>
    /// Equipment Classification Enums
    /// </summary>
    public enum EquipmentCategory
    {
        HVAC,
        Lighting,
        Electrical,
        Plumbing,
        Safety,
        Security,
        Communication,
        Computing,
        AudioVisual,
        Kitchen,
        Medical,
        Industrial,
        Custom
    }

    public enum EnergyEfficiencyClass
    {
        Unknown,
        A_Plus_Plus_Plus,
        A_Plus_Plus,
        A_Plus,
        A,
        B,
        C,
        D,
        E,
        F,
        G
    }

    public enum ComplianceStandards
    {
        None,
        OSHA,
        ISO_9001,
        ISO_14001,
        ISO_50001,
        ASHRAE,
        NEC,
        NFPA,
        ADA,
        HIPAA,
        GDPR,
        FCC,
        UL,
        CE,
        RoHS,
        WEEE,
        ENERGY_STAR,
        Custom
    }

    /// <summary>
    /// Sensor Classification Enums
    /// </summary>
    public enum SensorCategory
    {
        Environmental,
        Energy,
        Security,
        Safety,
        Operational,
        Structural,
        Custom
    }

    public enum CalibrationInterval
    {
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        SemiAnnually,
        Annually,
        Biennially,
        Custom
    }

    /// <summary>
    /// Warranty Classification Enums
    /// </summary>
    public enum WarrantyType
    {
        None,
        Limited,
        Full,
        Extended,
        Manufacturer,
        Service,
        Parts,
        Labor,
        Comprehensive,
        Custom
    }
}