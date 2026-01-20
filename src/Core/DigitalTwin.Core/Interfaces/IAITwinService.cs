using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    /// <summary>
    /// AI Twin service interface for creating and managing AI-powered digital twins
    /// </summary>
    public interface IAITwinService
    {
        /// <summary>
        /// Creates a new AI twin profile
        /// </summary>
        Task<AITwinProfile> CreateAITwinProfileAsync(AITwinCreationRequest request);

        /// <summary>
        /// Processes a user message and generates AI twin response
        /// </summary>
        Task<AITwinResponse> ProcessMessageAsync(AITwinMessage message, Guid twinId);

        /// <summary>
        /// Gets AI twin learning progress
        /// </summary>
        Task<AITwinLearningProgress> GetLearningProgressAsync(Guid twinId);

        /// <summary>
        /// Trains the AI twin on historical data
        /// </summary>
        Task<AITwinTrainingResult> TrainAITwinAsync(Guid twinId, AITwinTrainingRequest trainingRequest);

        /// <summary>
        /// Gets AI twin conversations
        /// </summary>
        Task<List<AITwinConversation>> GetConversationsAsync(Guid twinId, int page = 1, int pageSize = 20);

        /// <summary>
        /// Updates AI twin preferences
        /// </summary>
        Task<bool> UpdatePreferencesAsync(Guid twinId, Dictionary<string, object> preferences);

        /// <summary>
        /// Gets AI twin memory
        /// </summary>
        Task<List<AITwinMemory>> GetMemoryAsync(Guid twinId, AITwinMemoryType? memoryType = null);

        /// <summary>
        /// Deletes AI twin profile
        /// </summary>
        Task<bool> DeleteAITwinProfileAsync(Guid twinId);

        /// <summary>
        /// Gets AI twin profiles for a user
        /// </summary>
        Task<List<AITwinProfile>> GetUserTwinProfilesAsync(string userId);

        /// <summary>
        /// Updates AI twin personality traits
        /// </summary>
        Task<bool> UpdatePersonalityTraitsAsync(Guid twinId, AITwinPersonalityTraits traits);

        /// <summary>
        /// Gets AI twin insights
        /// </summary>
        Task<AITwinInsights> GetTwinInsightsAsync(Guid twinId);

        /// <summary>
        /// Backs up AI twin data
        /// </summary>
        Task<string> BackupTwinDataAsync(Guid twinId);

        /// <summary>
        /// Restores AI twin data from backup
        /// </summary>
        Task<AITwinRestoreResult> RestoreTwinFromBackupAsync(string backupData, string userId);

        /// <summary>
        /// Gets AI twin analytics
        /// </summary>
        Task<AITwinAnalytics> GetTwinAnalyticsAsync(Guid twinId);

        /// <summary>
        /// Sets twin learning mode
        /// </summary>
        Task<bool> SetLearningModeAsync(Guid twinId, AITwinLearningMode mode);

        /// <summary>
        /// Resets AI twin learning
        /// </summary>
        Task<bool> ResetLearningAsync(Guid twinId);

        /// <summary>
        /// Gets AI twin health status
        /// </summary>
        Task<AITwinHealthStatus> GetTwinHealthStatusAsync(Guid twinId);
    }

    /// <summary>
    /// LLM service interface for large language model integration
    /// </summary>
    public interface ILLMService
    {
        /// <summary>
        /// Generates response using LLM
        /// </summary>
        Task<LLMResponse> GenerateResponseAsync(LLMRequest request);

        /// <summary>
        /// Fine-tunes LLM on specific data
        /// </summary>
        Task<LLMTrainingResult> FineTuneAsync(LLMTrainingRequest request);

        /// <summary>
        /// Evaluates LLM response quality
        /// </summary>
        Task<double> EvaluateResponseQualityAsync(string prompt, string response, string expectedResponse = null);

        /// <summary>
        /// Gets available LLM models
        /// </summary>
        Task<List<LLMModel>> GetAvailableModelsAsync();

        /// <summary>
        /// Sets LLM configuration
        /// </summary>
        Task<bool> SetLLMConfigurationAsync(LLMConfiguration config);
    }

    /// <summary>
    /// AI Twin repository interface
    /// </summary>
    public interface IAITwinRepository
    {
        Task<AITwinProfile> GetByIdAsync(Guid id);
        Task<List<AITwinProfile>> GetAllAsync();
        Task<List<AITwinProfile>> GetByUserIdAsync(string userId);
        Task<AITwinProfile> AddAsync(AITwinProfile profile);
        Task<AITwinProfile> UpdateAsync(AITwinProfile profile);
        Task<bool> DeleteAsync(Guid id);
        Task<List<AITwinProfile>> GetByBuildingIdAsync(Guid buildingId);
        Task<int> GetCountByUserIdAsync(string userId);
    }

    /// <summary>
    /// Voice synthesis interface for AI twin voice capabilities
    /// </summary>
    public interface IVoiceSynthesisService
    {
        /// <summary>
        /// Converts text to speech
        /// </summary>
        Task<byte[]> SynthesizeSpeechAsync(string text, AITwinVoiceConfiguration voiceConfig);

        /// <summary>
        /// Gets available voices
        /// </summary>
        Task<List<AITwinVoice>> GetAvailableVoicesAsync();

        /// <summary>
        /// Generates voice with specific emotional tone
        /// </summary>
        Task<byte[]> SynthesizeEmotionalSpeechAsync(string text, EmotionalTone emotionalTone, AITwinVoiceConfiguration voiceConfig);
    }

    /// <summary>
    /// Image generation interface for AI twin visual representation
    /// </summary>
    public interface IAITwinImageService
    {
        /// <summary>
        /// Generates avatar image based on personality
        /// </summary>
        Task<byte[]> GenerateAvatarAsync(AITwinProfile profile);

        /// <summary>
        /// Generates visual representation of building state
        /// </summary>
        Task<byte[]> GenerateBuildingVisualizationAsync(Guid buildingId, AITwinVisualizationStyle style);

        /// <summary>
        /// Generates emotional expression image
        /// </summary>
        Task<byte[]> GenerateEmotionalExpressionAsync(EmotionalState emotionalState);
    }

    /// <summary>
    /// Emotional recognition interface for understanding user sentiment
    /// </summary>
    public interface IEmotionalRecognitionService
    {
        /// <summary>
        /// Analyzes emotional tone from text
        /// </summary>
        Task<EmotionalAnalysis> AnalyzeEmotionalToneAsync(string text);

        /// <summary>
        /// Detects facial expressions from image
        /// </summary>
        Task<FacialExpression> DetectFacialExpressionAsync(byte[] imageData);

        /// <summary>
        /// Analyzes speech patterns for emotion
        /// </summary>
        Task<SpeechEmotion> AnalyzeSpeechEmotionAsync(byte[] audioData);
    }

    /// <summary>
    /// Building repository interface for data access
    /// </summary>
    public interface IBuildingRepository
    {
        Task<Building> GetByIdAsync(Guid id);
        Task<List<Building>> GetAllAsync();
        Task<Building> AddAsync(Building building);
        Task<Building> UpdateAsync(Building building);
        Task<bool> DeleteAsync(Guid id);
        Task<List<Building>> GetByUserIdAsync(string userId);
    }

    /// <summary>
    /// Sensor repository interface for data access
    /// </summary>
    public interface ISensorRepository
    {
        Task<Sensor> GetByIdAsync(Guid id);
        Task<List<Sensor>> GetAllAsync();
        Task<List<Sensor>> GetByRoomIdAsync(Guid roomId);
        Task<List<Sensor>> GetByBuildingIdAsync(Guid buildingId);
        Task<Sensor> AddAsync(Sensor sensor);
        Task<Sensor> UpdateAsync(Sensor sensor);
        Task<bool> DeleteAsync(Guid id);
        Task<List<SensorReading>> GetReadingsAsync(Guid sensorId, DateTime? start = null, DateTime? end = null);
    }

    /// <summary>
    /// Sensor reading data
    /// </summary>
    public class SensorReading
    {
        public Guid Id { get; set; }
        public Guid SensorId { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Data collection service interface
    /// </summary>
    public interface IDataCollectionService
    {
        Task<List<SensorReading>> CollectReadingsAsync(Guid buildingId);
        Task<SensorReading> GetLatestReadingAsync(Guid sensorId);
        Task<bool> StartCollectionAsync(Guid buildingId);
        Task<bool> StopCollectionAsync(Guid buildingId);
    }
}