using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Enums;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Infrastructure.Services;

namespace DigitalTwin.Core.Services
{
    /// <summary>
    /// Conversation Service with Memory Integration
    /// Handles emotional conversations with AI companion, including memory and personalization
    /// </summary>
    public class ConversationService : IConversationService
    {
        private readonly IEmotionalStateService _emotionalStateService;
        private readonly IAITwinService _aiTwinService;
        private readonly ILogger<ConversationService> _logger;
        private readonly Dictionary<Guid, ConversationSession> _activeSessions;
        
        public ConversationService(
            IEmotionalStateService emotionalStateService,
            IAITwinService aiTwinService,
            ILogger<ConversationService> logger)
        {
            _emotionalStateService = emotionalStateService;
            _aiTwinService = aiTwinService;
            _logger = logger;
            _activeSessions = new Dictionary<Guid, ConversationSession>();
        }
        
        public async Task<ConversationResponse> StartConversationAsync(Guid userId, string initialMessage = null)
        {
            try
            {
                _logger.LogInformation("Starting conversation for user {UserId}", userId);
                
                // Create new conversation session
                var session = new ConversationSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    StartedAt = DateTime.UtcNow,
                    IsActive = true,
                    CurrentEmotionalState = EmotionType.Neutral,
                    ConversationContext = new Dictionary<string, object>()
                };
                
                _activeSessions[userId] = session;
                
                // Get user's emotional context from memories
                var userMemories = await _emotionalStateService.GetUserMemoriesAsync(userId, 20);
                var emotionalTrend = await _emotionalStateService.AnalyzeEmotionalTrendsAsync(userId, TimeSpan.FromDays(7));
                
                // Initialize AI twin with user context
                await _aiTwinService.InitializeUserSessionAsync(userId, userMemories, emotionalTrend);
                
                // Generate initial response if message provided
                ConversationResponse response;
                if (!string.IsNullOrEmpty(initialMessage))
                {
                    response = await ProcessMessageAsync(userId, initialMessage);
                }
                else
                {
                    // Generate greeting based on user's emotional state
                    response = await GenerateGreetingAsync(userId, emotionalTrend);
                }
                
                // Store initial memory
                if (!string.IsNullOrEmpty(initialMessage))
                {
                    var initialMemory = new EmotionalMemory
                    {
                        UserId = userId,
                        Description = initialMessage,
                        PrimaryEmotion = response.DetectedEmotion,
                        Intensity = response.EmotionalIntensity,
                        CreatedAt = DateTime.UtcNow,
                        ImportanceScore = 5, // Initial conversations are important
                        AssociatedEmotions = new List<EmotionType> { response.DetectedEmotion },
                        EmotionTags = new List<string> { "conversation_start", "initial_message" }
                    };
                    
                    await _emotionalStateService.StoreEmotionalMemoryAsync(initialMemory);
                }
                
                _logger.LogInformation("Conversation started successfully for user {UserId}", userId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation for user {UserId}: {Error}", userId, ex.Message);
                return new ConversationResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to start conversation",
                    Response = "I'm having trouble starting our conversation. Please try again in a moment."
                };
            }
        }
        
        public async Task<ConversationResponse> ProcessMessageAsync(Guid userId, string message)
        {
            try
            {
                _logger.LogInformation("Processing message for user {UserId}: {Message}", userId, message);
                
                if (!_activeSessions.ContainsKey(userId))
                {
                    return new ConversationResponse
                    {
                        Success = false,
                        ErrorMessage = "No active conversation session",
                        Response = "Let's start a new conversation first!"
                    };
                }
                
                var session = _activeSessions[userId];
                
                // Get relevant memories for context
                var relevantMemories = await _emotionalStateService.GetRelevantMemoriesAsync(userId, message, 10);
                
                // Generate AI response with emotional context
                var aiResponse = await _aiTwinService.GenerateResponseAsync(
                    userId, 
                    message, 
                    session.CurrentEmotionalState,
                    relevantMemories
                );
                
                // Analyze emotional content of message
                var detectedEmotion = AnalyzeMessageEmotion(message);
                session.CurrentEmotionalState = detectedEmotion;
                
                // Store conversation memory
                var memory = new EmotionalMemory
                {
                    UserId = userId,
                    Description = message,
                    PrimaryEmotion = detectedEmotion,
                    Intensity = aiResponse.EmotionalIntensity,
                    CreatedAt = DateTime.UtcNow,
                    ImportanceScore = CalculateMessageImportance(message, detectedEmotion),
                    AssociatedEmotions = new List<EmotionType> { detectedEmotion },
                    EmotionTags = ExtractEmotionTags(message, detectedEmotion)
                };
                
                await _emotionalStateService.StoreEmotionalMemoryAsync(memory);
                
                // Update session context
                session.MessageCount++;
                session.LastMessageAt = DateTime.UtcNow;
                session.ConversationContext["last_emotion"] = detectedEmotion;
                session.ConversationContext["message_count"] = session.MessageCount;
                
                // Create response
                var response = new ConversationResponse
                {
                    Success = true,
                    Response = aiResponse.Response,
                    DetectedEmotion = detectedEmotion,
                    EmotionalIntensity = aiResponse.EmotionalIntensity,
                    PersonalizationLevel = aiResponse.PersonalizationLevel,
                    MemoryReferences = aiResponse.MemoryReferences,
                    ResponseTime = DateTime.UtcNow,
                    SessionId = session.Id
                };
                
                _logger.LogInformation("Message processed successfully for user {UserId}", userId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message for user {UserId}: {Error}", userId, ex.Message);
                return new ConversationResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to process message",
                    Response = "I'm having trouble understanding that. Could you try rephrasing?"
                };
            }
        }
        
        public async Task<bool> EndConversationAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Ending conversation for user {UserId}", userId);
                
                if (_activeSessions.ContainsKey(userId))
                {
                    var session = _activeSessions[userId];
                    session.IsActive = false;
                    session.EndedAt = DateTime.UtcNow;
                    
                    // Store conversation summary memory
                    var summaryMemory = new EmotionalMemory
                    {
                        UserId = userId,
                        Description = $"Conversation ended after {session.MessageCount} messages",
                        PrimaryEmotion = session.CurrentEmotionalState,
                        Intensity = 0.5, // Neutral intensity for summary
                        CreatedAt = DateTime.UtcNow,
                        ImportanceScore = 3, // Conversation summaries are moderately important
                        AssociatedEmotions = new List<EmotionType> { session.CurrentEmotionalState },
                        EmotionTags = new List<string> { "conversation_end", "summary" }
                    };
                    
                    await _emotionalStateService.StoreEmotionalMemoryAsync(summaryMemory);
                    
                    _activeSessions.Remove(userId);
                    
                    // Clean up AI twin session
                    await _aiTwinService.EndUserSessionAsync(userId);
                    
                    _logger.LogInformation("Conversation ended successfully for user {UserId}", userId);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending conversation for user {UserId}: {Error}", userId, ex.Message);
                return false;
            }
        }
        
        public async Task<ConversationSession> GetActiveSessionAsync(Guid userId)
        {
            try
            {
                if (_activeSessions.ContainsKey(userId))
                {
                    return _activeSessions[userId];
                }
                
                // Try to restore from database if exists
                // Implementation would depend on your database schema
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active session for user {UserId}: {Error}", userId, ex.Message);
                return null;
            }
        }
        
        public async Task<List<ConversationMemory>> GetConversationMemoriesAsync(Guid userId, int limit = 50)
        {
            try
            {
                var memories = await _emotionalStateService.GetUserMemoriesAsync(userId, limit);
                
                // Filter for conversation-related memories
                var conversationMemories = memories
                    .Where(m => m.EmotionTags.Any(tag => tag.Contains("conversation") || tag.Contains("message")))
                    .OrderByDescending(m => m.CreatedAt)
                    .ToList();
                
                return conversationMemories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation memories for user {UserId}: {Error}", userId, ex.Message);
                return new List<ConversationMemory>();
            }
        }
        
        private async Task<ConversationResponse> GenerateGreetingAsync(Guid userId, EmotionalTrend emotionalTrend)
        {
            try
            {
                // Generate personalized greeting based on user's emotional state
                var greeting = await _aiTwinService.GenerateGreetingAsync(userId, emotionalTrend);
                
                return new ConversationResponse
                {
                    Success = true,
                    Response = greeting.Response,
                    DetectedEmotion = EmotionType.Happy, // Greetings are typically positive
                    EmotionalIntensity = 0.7,
                    PersonalizationLevel = greeting.PersonalizationLevel,
                    ResponseTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating greeting for user {UserId}: {Error}", userId, ex.Message);
                
                return new ConversationResponse
                {
                    Success = true,
                    Response = "Hello! I'm excited to talk with you today. How are you feeling?",
                    DetectedEmotion = EmotionType.Happy,
                    EmotionalIntensity = 0.7,
                    ResponseTime = DateTime.UtcNow
                };
            }
        }
        
        private EmotionType AnalyzeMessageEmotion(string message)
        {
            // Simple emotion detection from text
            var messageLower = message.ToLower();
            
            // Check for emotion keywords
            var emotionKeywords = new Dictionary<EmotionType, List<string>>
            {
                { EmotionType.Happy, new List<string> { "happy", "excited", "joy", "wonderful", "great", "amazing", "love", "fantastic" } },
                { EmotionType.Sad, new List<string> { "sad", "depressed", "down", "unhappy", "terrible", "awful", "hate", "disappointed" } },
                { EmotionType.Angry, new List<string> { "angry", "mad", "furious", "annoyed", "frustrated", "irritated", "upset", "rage" } },
                { EmotionType.Fear, new List<string> { "scared", "afraid", "fearful", "anxious", "worried", "nervous", "panic", "terrified" } },
                { EmotionType.Disgust, new List<string> { "disgusted", "gross", "awful", "terrible", "sick", "nasty", "revolting" } },
                { EmotionType.Surprise, new List<string> { "surprised", "shocked", "amazed", "astonished", "wow", "incredible", "unbelievable" } }
            };
            
            foreach (var emotion in emotionKeywords)
            {
                if (emotion.Value.Any(keyword => messageLower.Contains(keyword)))
                {
                    return emotion.Key;
                }
            }
            
            // Default to neutral if no emotion detected
            return EmotionType.Neutral;
        }
        
        private int CalculateMessageImportance(string message, EmotionType emotion)
        {
            var importance = 3; // Base importance
            
            // Increase importance for emotional messages
            if (emotion != EmotionType.Neutral)
            {
                importance += 2;
            }
            
            // Increase importance for longer messages
            if (message.Length > 50)
            {
                importance += 1;
            }
            
            // Increase importance for personal messages
            var personalKeywords = new[] { "I", "me", "my", "feel", "think", "believe", "want", "need" };
            if (personalKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                importance += 1;
            }
            
            return Math.Min(10, importance);
        }
        
        private List<string> ExtractEmotionTags(string message, EmotionType emotion)
        {
            var tags = new List<string> { emotion.ToString().ToLower() };
            
            // Add contextual tags
            if (message.Contains("building", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add("building_context");
            }
            
            if (message.Contains("help", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add("support_request");
            }
            
            if (message.Contains("thank", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add("gratitude");
            }
            
            return tags;
        }
    }
    
    public interface IConversationService
    {
        Task<ConversationResponse> StartConversationAsync(Guid userId, string initialMessage = null);
        Task<ConversationResponse> ProcessMessageAsync(Guid userId, string message);
        Task<bool> EndConversationAsync(Guid userId);
        Task<ConversationSession> GetActiveSessionAsync(Guid userId);
        Task<List<ConversationMemory>> GetConversationMemoriesAsync(Guid userId, int limit = 50);
    }
}