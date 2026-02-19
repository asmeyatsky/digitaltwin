using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Enums;
using DigitalTwin.Core.Events;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.Plugins;
using DigitalTwin.Core.Telemetry;

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
        private readonly IEventBus? _eventBus;
        private readonly IHttpClientFactory? _httpClientFactory;

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
            IProactiveCheckInService? checkInService = null,
            IEventBus? eventBus = null,
            IHttpClientFactory? httpClientFactory = null)
        {
            _emotionalStateService = emotionalStateService;
            _aiTwinService = aiTwinService;
            _logger = logger;
            _cache = cache;
            _encryptionService = encryptionService;
            _emotionFusionService = emotionFusionService;
            _pluginManager = pluginManager;
            _checkInService = checkInService;
            _eventBus = eventBus;
            _httpClientFactory = httpClientFactory;
        }

        private string SessionCacheKey(Guid userId) => $"conv:session:{userId}";

        public async Task<ConversationResponse> StartConversationAsync(Guid userId, string initialMessage = null)
        {
            try
            {
                _logger.LogInformation("Starting conversation for user {UserId}", userId);
                MetricsRegistry.ConversationsTotal.Inc();
                MetricsRegistry.ActiveConversations.Inc();

                var session = new ConversationSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.ToString(),
                    StartedAt = DateTime.UtcNow,
                    IsActive = true,
                    CurrentEmotionalState = Emotion.Neutral,
                    ConversationContext = new Dictionary<string, object>()
                };

                await _cache.SetStringAsync(SessionCacheKey(userId), JsonSerializer.Serialize(session), SessionCacheOptions);

                if (_eventBus != null)
                    await _eventBus.PublishAsync("conversation.started", new ConversationStarted(userId.ToString(), session.Id, DateTime.UtcNow));

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
                        AssociatedEmotions = new List<Emotion> { response.DetectedEmotion },
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
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                using var activity = DiagnosticConfig.Source.StartActivity("ProcessMessage");
                activity?.SetTag("userId", userId.ToString());
                activity?.SetTag("messageLength", message.Length);

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
                var textEmotion = await AnalyzeMessageEmotionAsync(message);

                // Multi-modal emotion fusion using unified Emotion enum (AD-1)
                var unifiedEmotion = textEmotion;
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
                    AssociatedEmotions = new List<Emotion> { textEmotion },
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

                activity?.SetTag("emotion", textEmotion.ToString());

                _logger.LogInformation("Message processed successfully for user {UserId}", userId);

                if (_eventBus != null)
                    await _eventBus.PublishAsync("message.processed", new MessageProcessed(userId.ToString(), fusedEmotionLabel, fusedConfidence, DateTime.UtcNow));

                stopwatch.Stop();
                MetricsRegistry.MessageLatencySeconds.Observe(stopwatch.Elapsed.TotalSeconds);

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

        public async Task<ConversationResponse> ProcessMessageAsync(Guid userId, Guid conversationId, string message)
        {
            // Ensure a session exists; start one if needed, then process the message
            var sessionJson = await _cache.GetStringAsync(SessionCacheKey(userId));
            if (sessionJson == null)
            {
                // Auto-start a session so the message can be processed
                await StartConversationAsync(userId);
            }

            return await ProcessMessageAsync(userId, message);
        }

        public async Task<ConversationHistoryResult> GetConversationHistoryAsync(Guid userId, Guid conversationId, int page = 1, int pageSize = 50)
        {
            try
            {
                _logger.LogInformation("Getting conversation history for user {UserId}, conversation {ConversationId}", userId, conversationId);

                var allMemories = await _emotionalStateService.GetUserMemoriesAsync(userId, int.MaxValue);

                var conversationMemories = allMemories
                    .Where(m => m.EmotionTags != null && m.EmotionTags.Any(tag =>
                        tag.Contains("conversation") || tag.Contains("message") || tag.Contains("support_request") || tag.Contains("gratitude")))
                    .OrderBy(m => m.CreatedAt)
                    .ToList();

                var totalCount = conversationMemories.Count;
                var paged = conversationMemories
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var items = paged.Select(m => new ConversationHistoryItem
                {
                    Id = m.Id != Guid.Empty ? m.Id : Guid.NewGuid(),
                    Content = m.Description ?? string.Empty,
                    UserEmotion = m.PrimaryEmotion.ToString(),
                    Timestamp = m.CreatedAt,
                    MessageType = m.EmotionTags?.Contains("conversation_start") == true ? "conversation_start"
                                : m.EmotionTags?.Contains("conversation_end") == true ? "conversation_end"
                                : "user_message"
                }).ToList();

                return new ConversationHistoryResult
                {
                    Messages = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for user {UserId}: {Error}", userId, ex.Message);
                return new ConversationHistoryResult
                {
                    Messages = new List<ConversationHistoryItem>(),
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize
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
                    MetricsRegistry.ActiveConversations.Dec();
                    session.EndedAt = DateTime.UtcNow;

                    var summaryMemory = new EmotionalMemory
                    {
                        UserId = userId.ToString(),
                        Description = $"Conversation ended after {session.MessageCount} messages",
                        PrimaryEmotion = session.CurrentEmotionalState,
                        Intensity = 0.5,
                        CreatedAt = DateTime.UtcNow,
                        ImportanceScore = 3,
                        AssociatedEmotions = new List<Emotion> { session.CurrentEmotionalState },
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
                    DetectedEmotion = Emotion.Happy,
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
                    DetectedEmotion = Emotion.Happy,
                    EmotionalIntensity = 0.7,
                    ResponseTime = DateTime.UtcNow
                };
            }
        }

        private async Task<Emotion> AnalyzeMessageEmotionAsync(string message)
        {
            if (_httpClientFactory != null)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("LLM");
                    var serviceKey = Environment.GetEnvironmentVariable("Services__ServiceKey") ?? "dev-service-key";
                    client.DefaultRequestHeaders.Add("X-Service-Key", serviceKey);

                    var response = await client.PostAsJsonAsync("/analyze-emotion-text", new { text = message });
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<NlpEmotionResult>();
                        if (result != null)
                        {
                            return EmotionMapper.FromString(result.Emotion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "NLP emotion analysis failed, falling back to keywords");
                }
            }

            return AnalyzeMessageEmotion(message);
        }

        private class NlpEmotionResult
        {
            public string Emotion { get; set; } = "neutral";
            public double Confidence { get; set; }
        }

        private Emotion AnalyzeMessageEmotion(string message)
        {
            var messageLower = message.ToLower();

            var emotionKeywords = new Dictionary<Emotion, List<string>>
            {
                { Emotion.Happy, new List<string> { "happy", "excited", "joy", "wonderful", "great", "amazing", "love", "fantastic" } },
                { Emotion.Sad, new List<string> { "sad", "depressed", "down", "unhappy", "terrible", "awful", "hate", "disappointed" } },
                { Emotion.Angry, new List<string> { "angry", "mad", "furious", "annoyed", "frustrated", "irritated", "upset", "rage" } },
                { Emotion.Anxious, new List<string> { "scared", "afraid", "fearful", "anxious", "worried", "nervous", "panic", "terrified" } },
                { Emotion.Surprised, new List<string> { "surprised", "shocked", "amazed", "astonished", "wow", "incredible", "unbelievable" } }
            };

            foreach (var emotion in emotionKeywords)
            {
                if (emotion.Value.Any(keyword => messageLower.Contains(keyword)))
                {
                    return emotion.Key;
                }
            }

            return Emotion.Neutral;
        }

        private int CalculateMessageImportance(string message, Emotion emotion)
        {
            var importance = 3;

            if (emotion != Emotion.Neutral)
                importance += 2;

            if (message.Length > 50)
                importance += 1;

            var personalKeywords = new[] { "I", "me", "my", "feel", "think", "believe", "want", "need" };
            if (personalKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                importance += 1;

            return Math.Min(10, importance);
        }

        private List<string> ExtractEmotionTags(string message, Emotion emotion)
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
