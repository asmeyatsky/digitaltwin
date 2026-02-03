using System;
using System.Collections.Generic;

namespace DigitalTwin.Core.Entities
{
    /// <summary>
    /// AI Twin profile containing personality, knowledge, and learning state
    /// </summary>
    public class AITwinProfile
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
        public Guid BuildingId { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastInteraction { get; set; }
        public DateTime? TrainingLastRun { get; set; }

        public AITwinLearningMode LearningMode { get; set; }
        public AITwinPersonalityTraits PersonalityTraits { get; set; }
        public List<AITwinKnowledge> KnowledgeBase { get; set; } = new();
        public List<AITwinInteraction> InteractionHistory { get; set; } = new();
        public Dictionary<string, double> BehavioralPatterns { get; set; } = new();
        public Dictionary<string, object> Preferences { get; set; } = new();
        public List<AITwinMemory> MemoryStore { get; set; } = new();

        public double ActivationLevel { get; set; }
        public EmotionalState EmotionalState { get; set; }
    }

    /// <summary>
    /// Personality traits for AI Twin behavior
    /// </summary>
    public class AITwinPersonalityTraits
    {
        public double Friendliness { get; set; }
        public double Professionalism { get; set; }
        public double Curiosity { get; set; }
        public double Empathy { get; set; }
        public double Humor { get; set; }
        public double Formality { get; set; }
        public double Adaptability { get; set; }
        public double Proactiveness { get; set; }
        public double Patience { get; set; }
        public double Enthusiasm { get; set; }
        public double AnalyticalThinking { get; set; }
        public double Creativity { get; set; }
    }

    /// <summary>
    /// Knowledge item stored in AI Twin's knowledge base
    /// </summary>
    public class AITwinKnowledge
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public AITwinKnowledgeType Type { get; set; }
        public string Content { get; set; }
        public double Importance { get; set; }
        public double Confidence { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string Source { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Types of knowledge in AI Twin's knowledge base
    /// </summary>
    public enum AITwinKnowledgeType
    {
        Self,
        BuildingLayout,
        BuildingSystems,
        BuildingManagement,
        Technical,
        UserPreference,
        OperationalProcedure,
        SafetyProtocol,
        MaintenanceSchedule,
        EnergyManagement
    }

    /// <summary>
    /// Interaction record between user and AI Twin
    /// </summary>
    public class AITwinInteraction
    {
        public Guid Id { get; set; }
        public Guid TwinId { get; set; }
        public string MessageType { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public EmotionalTone EmotionalTone { get; set; }
        public AITwinInteractionResponse Response { get; set; }
    }

    /// <summary>
    /// Response stored with interaction
    /// </summary>
    public class AITwinInteractionResponse
    {
        public string Content { get; set; }
        public EmotionalTone EmotionalTone { get; set; }
        public double Confidence { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Memory item stored by AI Twin
    /// </summary>
    public class AITwinMemory
    {
        public Guid Id { get; set; }
        public AITwinMemoryType Type { get; set; }
        public string Content { get; set; }
        public double Importance { get; set; }
        public DateTime CreationDate { get; set; }
        public List<Guid> AssociatedInteractions { get; set; } = new();
        public double EmotionalValence { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Types of memories stored by AI Twin
    /// </summary>
    public enum AITwinMemoryType
    {
        Interaction,
        UserPreference,
        BuildingEvent,
        LearningMilestone,
        EmotionalExperience,
        KnowledgeAcquisition
    }

    /// <summary>
    /// Learning mode for AI Twin
    /// </summary>
    public enum AITwinLearningMode
    {
        Adaptive,   // Continuously learns and adapts
        Guided,     // Learns with human feedback
        Fixed       // Maintains fixed behavior
    }

    /// <summary>
    /// Emotional state of AI Twin
    /// </summary>
    public enum EmotionalState
    {
        Neutral,
        Happy,
        Excited,
        Concerned,
        Frustrated,
        Curious,
        Calm,
        Alert
    }

    /// <summary>
    /// Emotional tone detected in messages
    /// </summary>
    public enum EmotionalTone
    {
        Neutral,
        Happy,
        Excited,
        Concerned,
        Frustrated,
        Curious
    }

    /// <summary>
    /// Building entity for building management
    /// </summary>
    public class Building
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double TotalArea { get; set; }
        public int TotalCapacity { get; set; }
        public List<Floor> Floors { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Floor within a building
    /// </summary>
    public class Floor
    {
        public Guid Id { get; set; }
        public Guid BuildingId { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public double Area { get; set; }
        public List<Room> Rooms { get; set; } = new();
    }

    /// <summary>
    /// Room within a floor
    /// </summary>
    public class Room
    {
        public Guid Id { get; set; }
        public Guid FloorId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public double Size { get; set; }
        public int Capacity { get; set; }
        public List<Equipment> Equipment { get; set; } = new();
        public List<Sensor> Sensors { get; set; } = new();
    }

    /// <summary>
    /// Equipment in a room
    /// </summary>
    public class Equipment
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public DateTime LastMaintenance { get; set; }
        public DateTime? NextMaintenance { get; set; }
    }

    /// <summary>
    /// Sensor in a room
    /// </summary>
    public class Sensor
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public string Name { get; set; }
        public SensorType Type { get; set; }
        public string Status { get; set; }
        public double? LastReading { get; set; }
        public DateTime? LastReadingTime { get; set; }
    }

    /// <summary>
    /// Types of sensors
    /// </summary>
    public enum SensorType
    {
        Temperature,
        Humidity,
        Motion,
        Light,
        AirQuality,
        Energy,
        Occupancy,
        Noise,
        CO2
    }
}
