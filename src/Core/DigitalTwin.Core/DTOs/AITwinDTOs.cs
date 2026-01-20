using System;
using System.Collections.Generic;
using DigitalTwin.Core.Entities;

// Re-export commonly used enums for convenience
using EmotionalTone = DigitalTwin.Core.Entities.EmotionalTone;
using EmotionalState = DigitalTwin.Core.Entities.EmotionalState;
using AITwinMemoryType = DigitalTwin.Core.Entities.AITwinMemoryType;
using AITwinKnowledgeType = DigitalTwin.Core.Entities.AITwinKnowledgeType;
using AITwinLearningMode = DigitalTwin.Core.Entities.AITwinLearningMode;
using AITwinPersonalityTraits = DigitalTwin.Core.Entities.AITwinPersonalityTraits;
using AITwinProfile = DigitalTwin.Core.Entities.AITwinProfile;
using AITwinMemory = DigitalTwin.Core.Entities.AITwinMemory;

namespace DigitalTwin.Core.DTOs
{
    /// <summary>
    /// Training data container for AI Twin learning
    /// </summary>
    public class AITwinTrainingData
    {
        public Guid ProfileId { get; set; }
        public DateTime TrainingStartDate { get; set; }
        public AITwinPersonalityTrainingData PersonalityData { get; set; }
        public AITwinKnowledgeTrainingData KnowledgeData { get; set; }
        public AITwinConversationTrainingData ConversationData { get; set; }
    }

    /// <summary>
    /// Training data for personality model
    /// </summary>
    public class AITwinPersonalityTrainingData
    {
        public List<PersonalityFeedbackSignal> FeedbackSignals { get; set; } = new();
        public List<AITwinInteraction> SuccessfulInteractions { get; set; } = new();
    }

    /// <summary>
    /// Feedback signal for adjusting personality traits
    /// </summary>
    public class PersonalityFeedbackSignal
    {
        public string TraitName { get; set; }
        public double Adjustment { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; }
    }

    /// <summary>
    /// Training data for knowledge base
    /// </summary>
    public class AITwinKnowledgeTrainingData
    {
        public List<AITwinKnowledgeItem> KnowledgeItems { get; set; } = new();
    }

    /// <summary>
    /// Individual knowledge item for training
    /// </summary>
    public class AITwinKnowledgeItem
    {
        public AITwinKnowledgeType Type { get; set; }
        public string KeyConcept { get; set; }
        public string Content { get; set; }
        public double Importance { get; set; }
        public double InitialConfidence { get; set; }
        public string Source { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Training data for conversation patterns
    /// </summary>
    public class AITwinConversationTrainingData
    {
        public List<ConversationPattern> ConversationPatterns { get; set; } = new();
        public List<TopicTransition> TopicTransitions { get; set; } = new();
        public List<ResponseTiming> ResponseTimings { get; set; } = new();
    }

    /// <summary>
    /// Conversation pattern for learning dialogue styles
    /// </summary>
    public class ConversationPattern
    {
        public string Intent { get; set; }
        public int Frequency { get; set; }
        public double AvgResponseLength { get; set; }
        public string ResponseTemplate { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Topic transition pattern for natural dialogue flow
    /// </summary>
    public class TopicTransition
    {
        public string FromTopic { get; set; }
        public string ToTopic { get; set; }
        public double SmoothnesScore { get; set; }
        public string TransitionPhrase { get; set; }
    }

    /// <summary>
    /// Response timing data for learning response patterns
    /// </summary>
    public class ResponseTiming
    {
        public int ResponseLength { get; set; }
        public DateTime Timestamp { get; set; }
        public double ResponseTime { get; set; }
        public string Intent { get; set; }
    }

    /// <summary>
    /// Conversation session grouping multiple interactions
    /// </summary>
    public class AITwinConversation
    {
        public Guid Id { get; set; }
        public Guid TwinId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastMessageTime { get; set; }
        public List<AITwinConversationMessage> Messages { get; set; } = new();
        public List<string> Topics { get; set; } = new();
        public EmotionalTone OverallSentiment { get; set; }
        public string Summary { get; set; }
        public int MessageCount => Messages?.Count ?? 0;
    }

    /// <summary>
    /// Individual message within a conversation
    /// </summary>
    public class AITwinConversationMessage
    {
        public Guid InteractionId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsUserMessage { get; set; }
        public EmotionalTone EmotionalTone { get; set; }
        public double? Confidence { get; set; }
    }

    /// <summary>
    /// Learning indicator showing what the AI Twin has learned
    /// </summary>
    public class LearningIndicator
    {
        public LearningIndicatorType Type { get; set; }
        public string Description { get; set; }
        public double Strength { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of learning indicators
    /// </summary>
    public enum LearningIndicatorType
    {
        NewTopicDiscovered,
        EmotionalPatternShift,
        PreferenceReinforced,
        HighConfidenceResponse,
        KnowledgeExpanded,
        BehavioralPatternLearned,
        PersonalityAdjusted,
        MemoryConsolidated
    }

    /// <summary>
    /// Request to create a new AI Twin profile
    /// </summary>
    public class AITwinCreationRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
        public Guid BuildingId { get; set; }
        public AITwinLearningMode? LearningMode { get; set; }
        public AITwinPersonalityTraits PersonalityTraits { get; set; }
        public Dictionary<string, object> InitialPreferences { get; set; }
    }

    /// <summary>
    /// Request to train an AI Twin
    /// </summary>
    public class AITwinTrainingRequest
    {
        public Guid TwinId { get; set; }
        public bool IncludeKnowledgeTraining { get; set; } = true;
        public bool IncludePersonalityTraining { get; set; } = true;
        public bool IncludeConversationTraining { get; set; } = true;
        public DateTime? TrainingDataStartDate { get; set; }
        public DateTime? TrainingDataEndDate { get; set; }
        public Dictionary<string, object> TrainingParameters { get; set; } = new();
    }

    /// <summary>
    /// Result of AI Twin training
    /// </summary>
    public class AITwinTrainingResult
    {
        public Guid TwinId { get; set; }
        public DateTime TrainingStartTime { get; set; }
        public DateTime? TrainingEndTime { get; set; }
        public AITwinTrainingStatus Status { get; set; }
        public string[] ModelsTrained { get; set; }
        public Dictionary<string, double> AccuracyMetrics { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration => TrainingEndTime.HasValue
            ? TrainingEndTime.Value - TrainingStartTime
            : TimeSpan.Zero;
    }

    /// <summary>
    /// Training status enum
    /// </summary>
    public enum AITwinTrainingStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Message sent to AI Twin
    /// </summary>
    public class AITwinMessage
    {
        public string Type { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public string SessionId { get; set; }
    }

    /// <summary>
    /// Response from AI Twin
    /// </summary>
    public class AITwinResponse
    {
        public Guid TwinId { get; set; }
        public Guid InteractionId { get; set; }
        public string Content { get; set; }
        public EmotionalTone EmotionalTone { get; set; }
        public double Confidence { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public List<Guid> MemoryReferences { get; set; } = new();
        public List<LearningIndicator> LearningIndicators { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Learning progress metrics for AI Twin
    /// </summary>
    public class AITwinLearningProgress
    {
        public Guid TwinId { get; set; }
        public int TotalInteractions { get; set; }
        public int LearnedPatterns { get; set; }
        public double ActivationLevel { get; set; }
        public double EmotionalDevelopment { get; set; }
        public double KnowledgeCompleteness { get; set; }
        public int MemoryCapacity { get; set; }
        public double LearningRate { get; set; }
        public double PersonalizationScore { get; set; }
        public List<string> RecentActivity { get; set; } = new();
        public List<string> DevelopmentalMilestones { get; set; } = new();

        // Delegate types for backward compatibility (if used elsewhere)
        public delegate double CalculateEmotionalDevelopmentDelegate(AITwinProfile profile);
        public delegate double CalculateKnowledgeCompletenessDelegate(AITwinProfile profile);
        public delegate double CalculateLearningRateDelegate(AITwinProfile profile);
        public delegate double CalculatePersonalizationScoreDelegate(AITwinProfile profile);
        public delegate List<string> GetRecentActivityDelegate(AITwinProfile profile);
        public delegate List<string> GetDevelopmentalMilestonesDelegate(AITwinProfile profile);
    }

    /// <summary>
    /// LLM request for generating responses
    /// </summary>
    public class LLMRequest
    {
        public string SystemPrompt { get; set; }
        public string UserPrompt { get; set; }
        public Dictionary<string, object> Context { get; set; }
        public AITwinPersonalityTraits Personality { get; set; }
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 1000;
    }

    /// <summary>
    /// LLM response from the language model
    /// </summary>
    public class LLMResponse
    {
        public string Content { get; set; }
        public EmotionalTone EmotionalTone { get; set; }
        public double Confidence { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public int TokensUsed { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Voice configuration for AI Twin
    /// </summary>
    public class AITwinVoiceConfiguration
    {
        public string VoiceId { get; set; }
        public string VoiceName { get; set; }
        public string Language { get; set; } = "en-US";
        public double Speed { get; set; } = 1.0;
        public double Pitch { get; set; } = 1.0;
        public double Volume { get; set; } = 1.0;
    }

    /// <summary>
    /// Insights generated by AI Twin
    /// </summary>
    public class AITwinInsights
    {
        public Guid TwinId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<string> KeyObservations { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public List<string> PredictedNeeds { get; set; } = new();
        public Dictionary<string, double> ConfidenceScores { get; set; } = new();
    }

    /// <summary>
    /// Result of restoring AI Twin from backup
    /// </summary>
    public class AITwinRestoreResult
    {
        public bool Success { get; set; }
        public Guid RestoredTwinId { get; set; }
        public string Message { get; set; }
        public DateTime RestoredAt { get; set; }
        public int InteractionsRestored { get; set; }
        public int MemoriesRestored { get; set; }
    }

    /// <summary>
    /// Analytics data for AI Twin
    /// </summary>
    public class AITwinAnalytics
    {
        public Guid TwinId { get; set; }
        public DateTime AnalysisDate { get; set; }
        public int TotalInteractions { get; set; }
        public int InteractionsThisWeek { get; set; }
        public int InteractionsThisMonth { get; set; }
        public double AverageResponseTime { get; set; }
        public double AverageConfidence { get; set; }
        public Dictionary<string, int> TopicDistribution { get; set; } = new();
        public Dictionary<string, int> EmotionalToneDistribution { get; set; } = new();
        public List<string> MostFrequentQueries { get; set; } = new();
        public double UserSatisfactionScore { get; set; }
    }

    /// <summary>
    /// Health status of AI Twin
    /// </summary>
    public class AITwinHealthStatus
    {
        public Guid TwinId { get; set; }
        public string Status { get; set; } // Healthy, Degraded, Unhealthy
        public DateTime LastCheckTime { get; set; }
        public bool IsResponsive { get; set; }
        public bool KnowledgeBaseHealthy { get; set; }
        public bool MemorySystemHealthy { get; set; }
        public bool LLMConnectionHealthy { get; set; }
        public double OverallHealthScore { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// LLM training request
    /// </summary>
    public class LLMTrainingRequest
    {
        public string ModelId { get; set; }
        public List<LLMTrainingExample> TrainingExamples { get; set; } = new();
        public Dictionary<string, object> Hyperparameters { get; set; } = new();
        public int Epochs { get; set; } = 3;
        public double LearningRate { get; set; } = 0.0001;
    }

    /// <summary>
    /// Training example for LLM
    /// </summary>
    public class LLMTrainingExample
    {
        public string Prompt { get; set; }
        public string Response { get; set; }
        public string Context { get; set; }
    }

    /// <summary>
    /// Result of LLM training
    /// </summary>
    public class LLMTrainingResult
    {
        public bool Success { get; set; }
        public string TrainedModelId { get; set; }
        public double TrainingLoss { get; set; }
        public double ValidationLoss { get; set; }
        public TimeSpan TrainingTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// LLM model information
    /// </summary>
    public class LLMModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Provider { get; set; }
        public string Version { get; set; }
        public int MaxTokens { get; set; }
        public bool SupportsFineTuning { get; set; }
        public Dictionary<string, object> Capabilities { get; set; } = new();
    }

    /// <summary>
    /// LLM configuration
    /// </summary>
    public class LLMConfiguration
    {
        public string ModelId { get; set; }
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 1000;
        public double TopP { get; set; } = 1.0;
        public double FrequencyPenalty { get; set; } = 0.0;
        public double PresencePenalty { get; set; } = 0.0;
        public List<string> StopSequences { get; set; } = new();
    }

    /// <summary>
    /// Available voice for AI Twin
    /// </summary>
    public class AITwinVoice
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Language { get; set; }
        public string Gender { get; set; }
        public string Accent { get; set; }
        public string SampleUrl { get; set; }
    }

    /// <summary>
    /// Visualization style for AI Twin building rendering
    /// </summary>
    public enum AITwinVisualizationStyle
    {
        Realistic,
        Schematic,
        Heatmap,
        DataOverlay,
        Simplified
    }

    /// <summary>
    /// Emotional analysis result
    /// </summary>
    public class EmotionalAnalysis
    {
        public EmotionalTone PrimaryEmotion { get; set; }
        public Dictionary<EmotionalTone, double> EmotionScores { get; set; } = new();
        public double Intensity { get; set; }
        public double Confidence { get; set; }
        public string Sentiment { get; set; } // Positive, Negative, Neutral
        public double SentimentScore { get; set; }
    }

    /// <summary>
    /// Facial expression detection result
    /// </summary>
    public class FacialExpression
    {
        public string Expression { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, double> ExpressionScores { get; set; } = new();
        public bool FaceDetected { get; set; }
    }

    /// <summary>
    /// Speech emotion analysis result
    /// </summary>
    public class SpeechEmotion
    {
        public EmotionalTone Emotion { get; set; }
        public double Confidence { get; set; }
        public double ArousalLevel { get; set; }
        public double ValenceLevel { get; set; }
        public Dictionary<string, double> EmotionProbabilities { get; set; } = new();
    }
}
