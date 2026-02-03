using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace DigitalTwin.API.Controllers
{
    /// <summary>
    /// Conversation Controller for Emotional Companion AI
    /// 
    /// Architectural Intent:
    /// - Provides REST API for conversational interactions
    /// - Integrates with emotion detection and LLM services
    /// - Maintains conversation context and memory
    /// - Supports real-time emotional responses
    /// 
    /// Key Features:
    /// 1. Start/stop conversation sessions
    /// 2. Send messages with emotion detection
    /// 3. Get conversation history
    /// 4. Manage conversation memory
    /// 5. Real-time emotional responses
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConversationController : ControllerBase
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<ConversationController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ConversationController(
            DigitalTwinDbContext context,
            ILogger<ConversationController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Start a new conversation session
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<ConversationStartResponse>> StartConversation(
            [FromBody] ConversationStartRequest request)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Create conversation session
                var sessionId = Guid.NewGuid();
                var interaction = new AITwinInteraction
                {
                    Id = sessionId,
                    TwinId = Guid.NewGuid(), // This would be the AI Twin ID
                    MessageType = "conversation_start",
                    Content = request.Message,
                    Timestamp = DateTime.UtcNow,
                    Context = new Dictionary<string, object>
                    {
                        ["user_id"] = userId,
                        ["session_id"] = sessionId,
                        ["platform"] = "web",
                        ["start_time"] = DateTime.UtcNow
                    },
                    EmotionalTone = EmotionalTone.Neutral,
                    Response = new AITwinInteractionResponse
                    {
                        Content = "Hello! I'm here to help and support you. How are you feeling today?",
                        EmotionalTone = EmotionalTone.Happy,
                        Confidence = 0.9,
                        Timestamp = DateTime.UtcNow
                    }
                };

                _context.AITwinInteractions.Add(interaction);
                await _context.SaveChangesAsync();

                return Ok(new ConversationStartResponse
                {
                    SessionId = sessionId,
                    Response = interaction.Response.Content,
                    EmotionalTone = interaction.Response.EmotionalTone.ToString(),
                    Timestamp = interaction.Response.Timestamp
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Send a message in the conversation
        /// </summary>
        [HttpPost("message")]
        public async Task<ActionResult<ConversationMessageResponse>> SendMessage(
            [FromBody] ConversationMessageRequest request)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Get detected emotion from DeepFace service (placeholder for now)
                var detectedEmotion = await DetectEmotionAsync(request.Message);
                
                // Generate AI response using LLM service (placeholder for now)
                var aiResponse = await GenerateAIResponseAsync(request.Message, detectedEmotion);

                // Store interaction
                var interaction = new AITwinInteraction
                {
                    Id = Guid.NewGuid(),
                    TwinId = Guid.NewGuid(), // This would be the AI Twin ID
                    MessageType = "user_message",
                    Content = request.Message,
                    Timestamp = DateTime.UtcNow,
                    Context = new Dictionary<string, object>
                    {
                        ["user_id"] = userId,
                        ["conversation_id"] = request.ConversationId,
                        ["detected_emotion"] = detectedEmotion,
                        ["response_time"] = DateTime.UtcNow
                    },
                    EmotionalTone = ParseEmotionalTone(detectedEmotion),
                    Response = new AITwinInteractionResponse
                    {
                        Content = aiResponse,
                        EmotionalTone = DetermineResponseEmotion(detectedEmotion),
                        Confidence = 0.85,
                        Timestamp = DateTime.UtcNow
                    }
                };

                _context.AITwinInteractions.Add(interaction);
                await _context.SaveChangesAsync();

                return Ok(new ConversationMessageResponse
                {
                    Response = aiResponse,
                    DetectedEmotion = detectedEmotion,
                    AIEmotionalTone = interaction.Response.EmotionalTone.ToString(),
                    ResponseTime = DateTime.UtcNow,
                    ConversationId = request.ConversationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get conversation history
        /// </summary>
        [HttpGet("history/{conversationId}")]
        public async Task<ActionResult<ConversationHistoryResponse>> GetConversationHistory(
            Guid conversationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var interactions = await _context.AITwinInteractions
                    .Where(i => i.Context.ContainsKey("conversation_id") && 
                                i.Context.ContainsKey("user_id") &&
                                i.Context["user_id"].ToString() == userId)
                    .OrderBy(i => i.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var history = interactions.Select(i => new ConversationMessage
                {
                    Id = i.Id,
                    Content = i.Content,
                    Response = i.Response?.Content,
                    UserEmotion = i.EmotionalTone.ToString(),
                    AIEmotion = i.Response?.EmotionalTone.ToString(),
                    Timestamp = i.Timestamp,
                    MessageType = i.MessageType
                }).ToList();

                return Ok(new ConversationHistoryResponse
                {
                    Messages = history,
                    TotalCount = interactions.Count,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// End conversation session
        /// </summary>
        [HttpPost("end")]
        public async Task<ActionResult> EndConversation(
            [FromBody] ConversationEndRequest request)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Log conversation end
                var interaction = new AITwinInteraction
                {
                    Id = Guid.NewGuid(),
                    TwinId = Guid.NewGuid(),
                    MessageType = "conversation_end",
                    Content = "Conversation ended",
                    Timestamp = DateTime.UtcNow,
                    Context = new Dictionary<string, object>
                    {
                        ["user_id"] = userId,
                        ["conversation_id"] = request.ConversationId,
                        ["end_time"] = DateTime.UtcNow,
                        ["session_duration"] = request.SessionDuration?.TotalSeconds
                    },
                    EmotionalTone = EmotionalTone.Neutral
                };

                _context.AITwinInteractions.Add(interaction);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Conversation ended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending conversation");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<string> DetectEmotionAsync(string message)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync("http://localhost:8001/detect-emotion", 
                    new { text = message });
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<EmotionDetectionResult>();
                    return result?.Emotion ?? "neutral";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect emotion via ML service");
            }

            // Fallback: simple keyword-based emotion detection
            var lowerMessage = message.ToLower();
            if (lowerMessage.Contains("happy") || lowerMessage.Contains("excited")) return "happy";
            if (lowerMessage.Contains("sad") || lowerMessage.Contains("depressed")) return "sad";
            if (lowerMessage.Contains("angry") || lowerMessage.Contains("frustrated")) return "angry";
            if (lowerMessage.Contains("worried") || lowerMessage.Contains("anxious")) return "anxious";
            
            return "neutral";
        }

        private async Task<string> GenerateAIResponseAsync(string userMessage, string detectedEmotion)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync("http://localhost:8002/generate-response",
                    new 
                    { 
                        message = userMessage,
                        emotion = detectedEmotion,
                        context = "emotional_companion"
                    });
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LLMResponse>();
                    return result?.Response ?? GenerateFallbackResponse(userMessage, detectedEmotion);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate AI response via LLM service");
            }

            return GenerateFallbackResponse(userMessage, detectedEmotion);
        }

        private string GenerateFallbackResponse(string userMessage, string detectedEmotion)
        {
            return detectedEmotion switch
            {
                "happy" => "I'm so glad to hear you're feeling happy! Your positive energy is wonderful. What's bringing you joy today?",
                "sad" => "I hear that you're feeling sad. I'm here for you, and we'll get through this together. Would you like to talk about what's on your mind?",
                "angry" => "I understand you're feeling angry. Your feelings are valid. Let's take a deep breath together. What's been frustrating you?",
                "anxious" => "I can sense you're feeling anxious. That's completely understandable. Let's work through this step by step. What's worrying you?",
                "worried" => "I hear your concern. Let's break this down and look at it together. What specific aspect is troubling you most?",
                _ => "Thank you for sharing that with me. I'm here to listen and support you. What would be most helpful right now?"
            };
        }

        private EmotionalTone ParseEmotionalTone(string emotion)
        {
            return emotion.ToLower() switch
            {
                "happy" => EmotionalTone.Happy,
                "excited" => EmotionalTone.Excited,
                "sad" => EmotionalTone.Neutral,
                "angry" => EmotionalTone.Frustrated,
                "anxious" or "worried" => EmotionalTone.Concerned,
                "curious" => EmotionalTone.Curious,
                _ => EmotionalTone.Neutral
            };
        }

        private EmotionalTone DetermineResponseEmotion(string userEmotion)
        {
            return userEmotion.ToLower() switch
            {
                "sad" or "angry" => EmotionalTone.Concerned,
                "happy" or "excited" => EmotionalTone.Happy,
                "anxious" or "worried" => EmotionalTone.Calm,
                _ => EmotionalTone.Neutral
            };
        }
    }

    // DTOs
    public class ConversationStartRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ConversationStartResponse
    {
        public Guid SessionId { get; set; }
        public string Response { get; set; } = string.Empty;
        public string EmotionalTone { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ConversationMessageRequest
    {
        public Guid ConversationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ConversationMessageResponse
    {
        public string Response { get; set; } = string.Empty;
        public string DetectedEmotion { get; set; } = string.Empty;
        public string AIEmotionalTone { get; set; } = string.Empty;
        public DateTime ResponseTime { get; set; }
        public Guid ConversationId { get; set; }
    }

    public class ConversationHistoryResponse
    {
        public List<ConversationMessage> Messages { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class ConversationMessage
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Response { get; set; }
        public string UserEmotion { get; set; } = string.Empty;
        public string? AIEmotion { get; set; }
        public DateTime Timestamp { get; set; }
        public string MessageType { get; set; } = string.Empty;
    }

    public class ConversationEndRequest
    {
        public Guid ConversationId { get; set; }
        public TimeSpan? SessionDuration { get; set; }
    }

    public class EmotionDetectionResult
    {
        public string Emotion { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }

    public class LLMResponse
    {
        public string Response { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }
}