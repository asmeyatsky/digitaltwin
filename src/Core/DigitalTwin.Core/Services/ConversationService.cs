using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Enums;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.Plugins;

namespace DigitalTwin.Core.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IEmotionalStateService _emotionalStateService;
        private readonly IAITwinService _aiTwinService;
        private readonly ILogger<ConversationService> _logger;
        private readonly IDistributedCache _cache;
        private readonly IEncryptionService? _encryptionService;
        private readonly IEmotionFusionService? _emotionFusionService;
        private readonly IPluginManager? _pluginManager;
        private readonly IProactiveCheckInService? _checkInService;

        private static readonly DistributedCacheEntryOptions SessionCacheOptions = new()
        {
            SlidingExpiration = TimeSpan.FromHours(1)
        };

        public ConversationService(
            IEmotionalStateService emotionalStateService,
            IAITwinService aiTwinService,
            ILogger<ConversationService> logger,
            IDistributedCache cache,
            IEncryptionService? encryptionService = null,
            IEmotionFusionService? emotionFusionService = null,
            IPluginManager? pluginManager = null,
            IProactiveCheckInService? checkInService = null)
        {
            _emotionalStateService = emotionalStateService;
            _aiTwinService = aiTwinService;
            _logger = logger;
            _cache = cache;
            _encryptionService = encryptionService;
            _emotionFusionService = emotionFusionService;
            _pluginManager = pluginManager;
            _checkInService = checkInService;
        }

        private string SessionCacheKey(Guid userId) => $"conv:session:{userId}";

        public async Task<ConversationResponse> StartConversationAsync(Guid userId, string initialMessage = null)
        {
            try
            {
                _logger.LogInformation("Starting conversation for user {UserId}", userId);

                var session = new ConversationSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.ToString(),
                    StartedAt = DateTime.UtcNow,
                    IsActive = true,
                    CurrentEmotionalState = EmotionType.Neutral,
                    ConversationContext = new Dictionary<string, object>()
                };

                await _cache.SetStringAsync(SessionCacheKey(userId), JsonSerializer.Serialize(session), SessionCacheOptions);

                var userMemories = await _emotionalStateService.GetUserMemoriesAsync(userId, 20);
                var emotionalTrend = await _emotionalStateService.AnalyzeEmotionalTrendsAsync(userId, TimeSpan.FromDays(7));

                await _aiTwinService.InitializeUserSessionAsync(userId.ToString());

                ConversationResponse response;
                if (!string.IsNullOrEmpty(initialMessage))
                {
                    response = await ProcessMessageAsync(userId, initialMessage);
                }
                else
                {
                    response = await GenerateGreetingAsync(userId, emotionalTrend);
                }

                if (!string.IsNullOrEmpty(initialMessage))
                {
                    var initialMemory = new EmotionalMemory
                    {
                        UserId = userId.ToString(),
                        Description = initialMessage,
                        PrimaryEmotion = response.DetectedEmotion,
                        Intensity = response.EmotionalIntensity,
                        CreatedAt = DateTime.UtcNow,
                        ImportanceScore = 5,
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
                _logger.LogInformation("Processing message for user {UserId}", userId);

                var sessionJson = await _cache.GetStringAsync(SessionCacheKey(userId));
                if (sessionJson == null)
                {
                    return new ConversationResponse
                    {
                        Success = false,
                        ErrorMessage = "No active conversation session",
                        Response = "Let's start a new conversation first!"
                    };
                }

                var session = JsonSerializer.Deserialize<ConversationSession>(sessionJson);

                // Get relevant memories for context
                var relevantMemories = await _emotionalStateService.GetRelevantMemoriesAsync(userId, message, 10);

                // Analyze text emotion (keyword-based)
                var textEmotion = AnalyzeMessageEmotion(message);

                // Multi-modal emotion fusion using unified Emotion enum (AD-1)
                var unifiedEmotion = EmotionMapper.FromEmotionType(textEmotion);
                var fusedEmotionLabel = EmotionMapper.ToExternalString(unifiedEmotion);
                double fusedConfidence = 0.7;

                if (_emotionFusionService != null)
                {
                    var signals = new List<EmotionSignal>
                    {
                        new EmotionSignal
                        {
                            Source = EmotionSource.Text,
                            Emotion = unifiedEmotion,
                            Confidence = 0.6,
                            Timestamp = DateTime.UtcNow
                        }
                    };

                    // Check session metadata for voice/face signals — map strings via EmotionMapper
                    if (session.ConversationContext.TryGetValue("voice_emotion", out var voiceEmo) && voiceEmo is JsonElement voiceEl)
                    {
                        signals.Add(new EmotionSignal
                        {
                            Source = EmotionSource.Voice,
                            Emotion = EmotionMapper.FromString(voiceEl.GetString() ?? "neutral"),
                            Confidence = 0.7,
                            Timestamp = DateTime.UtcNow
                        });
                    }

                    if (session.ConversationContext.TryGetValue("face_emotion", out var faceEmo) && faceEmo is JsonElement faceEl)
                    {
                        signals.Add(new EmotionSignal
                        {
                            Source = EmotionSource.Face,
                            Emotion = EmotionMapper.FromString(faceEl.GetString() ?? "neutral"),
                            Confidence = 0.8,
                            Timestamp = DateTime.UtcNow
                        });
                    }

                    var fused = await _emotionFusionService.FuseEmotionsAsync(signals.ToArray());
                    fusedEmotionLabel = EmotionMapper.ToExternalString(fused.PrimaryEmotion);
                    fusedConfidence = fused.Confidence;
                }

                // Run plugin pipeline
                var aiContext = new Dictionary<string, object>
                {
                    { "currentEmotion", fusedEmotionLabel },
                    { "relevantMemories", relevantMemories.Count }
                };

                if (_pluginManager != null)
                {
                    var pluginCtx = new PluginContext
                    {
                        UserId = userId.ToString(),
                        Message = message,
                        Emotion = fusedEmotionLabel,
                        Memories = relevantMemories,
                        SessionId = session.Id.ToString()
                    };

                    pluginCtx = await _pluginManager.ExecutePipelineAsync(pluginCtx);
                    message = pluginCtx.Message;

                    // Merge plugin context into AI context
                    foreach (var (key, value) in pluginCtx.AdditionalContext)
                        aiContext[key] = value;
                }

                // Generate AI response
                var aiResponseText = await _aiTwinService.GenerateResponseAsync(
                    userId.ToString(), message, aiContext);

                // Append safety resources if crisis detected
                if (aiContext.TryGetValue("safety_resources", out var safetyRes))
                    aiResponseText += safetyRes.ToString();

                // Encrypt message content if encryption is available
                var conversationMessage = new ConversationMessage
                {
                    Role = "user",
                    Timestamp = DateTime.UtcNow,
                    DetectedEmotion = textEmotion
                };

                if (_encryptionService != null)
                {
                    var (ciphertext, iv, tag) = _encryptionService.Encrypt(message);
                    conversationMessage.EncryptedContent = ciphertext;
                    conversationMessage.IV = iv;
                    conversationMessage.AuthTag = tag;
                    conversationMessage.IsEncrypted = true;
                }
                else
                {
                    conversationMessage.Content = message;
                }

                session.CurrentEmotionalState = textEmotion;

                // Store conversation memory
                var memory = new EmotionalMemory
                {
                    UserId = userId.ToString(),
                    Description = message,
                    PrimaryEmotion = textEmotion,
                    Intensity = fusedConfidence,
                    CreatedAt = DateTime.UtcNow,
                    ImportanceScore = CalculateMessageImportance(message, textEmotion),
                    AssociatedEmotions = new List<EmotionType> { textEmotion },
                    EmotionTags = ExtractEmotionTags(message, textEmotion)
                };

                await _emotionalStateService.StoreEmotionalMemoryAsync(memory);

                // Update session context
                session.MessageCount++;
                session.LastMessageAt = DateTime.UtcNow;
                session.ConversationContext["last_emotion"] = textEmotion;
                session.ConversationContext["message_count"] = session.MessageCount;

                await _cache.SetStringAsync(SessionCacheKey(userId), JsonSerializer.Serialize(session), SessionCacheOptions);

                // Evaluate proactive check-ins based on emotional state
                if (_checkInService != null)
                {
                    try
                    {
                        await _checkInService.EvaluateCheckInAsync(userId.ToString());
                    }
                    catch (Exception checkEx)
                    {
                        _logger.LogWarning(checkEx, "Check-in evaluation failed, continuing");
                    }
                }

                var response = new ConversationResponse
                {
                    Success = true,
                    Response = aiResponseText,
                    DetectedEmotion = textEmotion,
                    EmotionalIntensity = fusedConfidence,
                    PersonalizationLevel = 0.5,
                    MemoryReferences = new List<string>(),
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

                var cachedJson = await _cache.GetStringAsync(SessionCacheKey(userId));
                if (cachedJson != null)
                {
                    var session = JsonSerializer.Deserialize<ConversationSession>(cachedJson);
                    session.IsActive = false;
                    session.EndedAt = DateTime.UtcNow;

                    var summaryMemory = new EmotionalMemory
                    {
                        UserId = userId.ToString(),
                        Description = $"Conversation ended after {session.MessageCount} messages",
                        PrimaryEmotion = session.CurrentEmotionalState,
                        Intensity = 0.5,
                        CreatedAt = DateTime.UtcNow,
                        ImportanceScore = 3,
                        AssociatedEmotions = new List<EmotionType> { session.CurrentEmotionalState },
                        EmotionTags = new List<string> { "conversation_end", "summary" }
                    };

                    await _emotionalStateService.StoreEmotionalMemoryAsync(summaryMemory);
                    await _cache.RemoveAsync(SessionCacheKey(userId));
                    await _aiTwinService.EndUserSessionAsync(userId.ToString());

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
                var json = await _cache.GetStringAsync(SessionCacheKey(userId));
                if (json != null)
                {
                    return JsonSerializer.Deserialize<ConversationSession>(json);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active session for user {UserId}: {Error}", userId, ex.Message);
                return null;
            }
        }

        public async Task<List<EmotionalMemory>> GetConversationMemoriesAsync(Guid userId, int limit = 50)
        {
            try
            {
                var memories = await _emotionalStateService.GetUserMemoriesAsync(userId, limit);

                var conversationMemories = memories
                    .Where(m => m.EmotionTags != null && m.EmotionTags.Any(tag => tag.Contains("conversation") || tag.Contains("message")))
                    .OrderByDescending(m => m.CreatedAt)
                    .ToList();

                return conversationMemories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation memories for user {UserId}: {Error}", userId, ex.Message);
                return new List<EmotionalMemory>();
            }
        }

        /// <summary>
        /// Decrypts a conversation message if encrypted, returns content either way.
        /// </summary>
        public string GetMessageContent(ConversationMessage msg)
        {
            if (msg.IsEncrypted && _encryptionService != null
                && msg.EncryptedContent != null && msg.IV != null && msg.AuthTag != null)
            {
                return _encryptionService.Decrypt(msg.EncryptedContent, msg.IV, msg.AuthTag);
            }
            return msg.Content ?? string.Empty;
        }

        private async Task<ConversationResponse> GenerateGreetingAsync(Guid userId, EmotionalTrend emotionalTrend)
        {
            try
            {
                var greetingText = await _aiTwinService.GenerateGreetingAsync(userId.ToString());

                return new ConversationResponse
                {
                    Success = true,
                    Response = greetingText,
                    DetectedEmotion = EmotionType.Happy,
                    EmotionalIntensity = 0.7,
                    PersonalizationLevel = 0.5,
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
            var messageLower = message.ToLower();

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

            return EmotionType.Neutral;
        }

        private int CalculateMessageImportance(string message, EmotionType emotion)
        {
            var importance = 3;

            if (emotion != EmotionType.Neutral)
                importance += 2;

            if (message.Length > 50)
                importance += 1;

            var personalKeywords = new[] { "I", "me", "my", "feel", "think", "believe", "want", "need" };
            if (personalKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                importance += 1;

            return Math.Min(10, importance);
        }

        private List<string> ExtractEmotionTags(string message, EmotionType emotion)
        {
            var tags = new List<string> { emotion.ToString().ToLower() };

            if (message.Contains("building", StringComparison.OrdinalIgnoreCase))
                tags.Add("building_context");

            if (message.Contains("help", StringComparison.OrdinalIgnoreCase))
                tags.Add("support_request");

            if (message.Contains("thank", StringComparison.OrdinalIgnoreCase))
                tags.Add("gratitude");

            return tags;
        }
    }
}
