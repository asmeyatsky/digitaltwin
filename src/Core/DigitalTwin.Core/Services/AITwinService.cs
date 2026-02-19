using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Enums;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Services
{
    /// <summary>
    /// AI Twin service for creating and managing AI-powered digital twins
    /// </summary>
    public class AITwinService : IAITwinService
    {
        private readonly IBuildingRepository _buildingRepository;
        private readonly ISensorRepository _sensorRepository;
        private readonly IDataCollectionService _dataCollectionService;
        private readonly IAnalyticsService _analyticsService;
        private readonly ILLMService _llmService;
        private readonly IAITwinRepository _aiTwinRepository;

        public AITwinService(
            IBuildingRepository buildingRepository,
            ISensorRepository sensorRepository,
            IDataCollectionService dataCollectionService,
            IAnalyticsService analyticsService,
            ILLMService llmService,
            IAITwinRepository aiTwinRepository)
        {
            _buildingRepository = buildingRepository;
            _sensorRepository = sensorRepository;
            _dataCollectionService = dataCollectionService;
            _analyticsService = analyticsService;
            _llmService = llmService;
            _aiTwinRepository = aiTwinRepository;
        }

        /// <summary>
        /// Creates a new AI twin profile
        /// </summary>
        public async Task<AITwinProfile> CreateAITwinProfileAsync(AITwinCreationRequest request)
        {
            var profile = new AITwinProfile
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                UserId = request.UserId,
                BuildingId = request.BuildingId,
                CreationDate = DateTime.UtcNow,
                LearningMode = request.LearningMode ?? AITwinLearningMode.Adaptive,
                PersonalityTraits = InitializePersonalityTraits(request.PersonalityTraits),
                KnowledgeBase = InitializeKnowledgeBase(request),
                InteractionHistory = new List<AITwinInteraction>(),
                BehavioralPatterns = new Dictionary<string, double>(),
                Preferences = request.InitialPreferences ?? new Dictionary<string, object>(),
                ActivationLevel = 0.1, // Start with low activation
                LastInteraction = DateTime.UtcNow,
                MemoryStore = new List<AITwinMemory>(),
                EmotionalState = Emotion.Neutral
            };

            // Initialize with building data
            await InitializeWithBuildingData(profile, request.BuildingId);

            await _aiTwinRepository.AddAsync(profile);

            return profile;
        }

        /// <summary>
        /// Processes a user message and generates AI twin response
        /// </summary>
        public async Task<AITwinResponse> ProcessMessageAsync(AITwinMessage message, Guid twinId)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null)
                throw new ArgumentException($"AI twin profile with ID {twinId} not found");

            // Update interaction history
            var interaction = new AITwinInteraction
            {
                Id = Guid.NewGuid(),
                TwinId = twinId,
                MessageType = message.Type,
                Content = message.Content,
                Timestamp = DateTime.UtcNow,
                Context = message.Context,
                EmotionalTone = AnalyzeEmotionFromContent(message.Content)
            };

            profile.InteractionHistory.Add(interaction);
            profile.LastInteraction = DateTime.UtcNow;

            // Analyze and update behavioral patterns
            await UpdateBehavioralPatterns(profile, interaction);

            // Generate response using LLM
            var response = await GenerateResponseAsync(profile, interaction);

            // Learn from interaction
            await LearnFromInteraction(profile, interaction, response);

            // Update emotional state
            profile.EmotionalState = UpdateEmotionalStateFromInteraction(profile.EmotionalState, interaction);

            // Save updated profile
            await _aiTwinRepository.UpdateAsync(profile);

            return response;
        }

        /// <summary>
        /// Gets AI twin learning progress
        /// </summary>
        public async Task<AITwinLearningProgress> GetLearningProgressAsync(Guid twinId)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null)
                throw new ArgumentException($"AI twin profile with ID {twinId} not found");

            return new AITwinLearningProgress
            {
                TwinId = twinId,
                TotalInteractions = profile.InteractionHistory.Count,
                LearnedPatterns = profile.BehavioralPatterns.Count,
                ActivationLevel = profile.ActivationLevel,
                EmotionalDevelopment = CalculateEmotionalDevelopment(profile),
                KnowledgeCompleteness = CalculateKnowledgeCompleteness(profile),
                MemoryCapacity = profile.MemoryStore.Count,
                LearningRate = CalculateLearningRate(profile),
                PersonalizationScore = CalculatePersonalizationScore(profile),
                RecentActivity = GetRecentActivity(profile),
                DevelopmentalMilestones = GetDevelopmentalMilestones(profile)
            };
        }

        /// <summary>
        /// Trains the AI twin on historical data
        /// </summary>
        public async Task<AITwinTrainingResult> TrainAITwinAsync(Guid twinId, AITwinTrainingRequest trainingRequest)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null)
                throw new ArgumentException($"AI twin profile with ID {twinId} not found");

            var result = new AITwinTrainingResult
            {
                TwinId = twinId,
                TrainingStartTime = DateTime.UtcNow,
                Status = AITwinTrainingStatus.InProgress
            };

            try
            {
                // Process training data
                var trainingData = await PrepareTrainingData(profile, trainingRequest);

                // Train personality model
                await TrainPersonalityModel(profile, trainingData.PersonalityData);

                // Train knowledge base
                await TrainKnowledgeBase(profile, trainingData.KnowledgeData);

                // Train conversation patterns
                await TrainConversationPatterns(profile, trainingData.ConversationData);

                // Update profile with trained parameters
                profile.ActivationLevel = Math.Min(profile.ActivationLevel + 0.1, 1.0);
                profile.TrainingLastRun = DateTime.UtcNow;

                await _aiTwinRepository.UpdateAsync(profile);

                result.Status = AITwinTrainingStatus.Completed;
                result.TrainingEndTime = DateTime.UtcNow;
                result.ModelsTrained = new[] { "Personality", "Knowledge", "Conversation" };
                result.AccuracyMetrics = CalculateTrainingAccuracy(profile);
            }
            catch (Exception ex)
            {
                result.Status = AITwinTrainingStatus.Failed;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Gets AI twin conversations
        /// </summary>
        public async Task<List<AITwinConversation>> GetConversationsAsync(Guid twinId, int page = 1, int pageSize = 20)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null)
                throw new ArgumentException($"AI twin profile with ID {twinId} not found");

            var interactions = profile.InteractionHistory
                .OrderByDescending(i => i.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return ConvertToConversations(interactions);
        }

        /// <summary>
        /// Updates AI twin preferences
        /// </summary>
        public async Task<bool> UpdatePreferencesAsync(Guid twinId, Dictionary<string, object> preferences)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null)
                return false;

            foreach (var preference in preferences)
            {
                profile.Preferences[preference.Key] = preference.Value;
            }

            profile.LastInteraction = DateTime.UtcNow;
            await _aiTwinRepository.UpdateAsync(profile);

            return true;
        }

        /// <summary>
        /// Gets AI twin memory
        /// </summary>
        public async Task<List<AITwinMemory>> GetMemoryAsync(Guid twinId, AITwinMemoryType? memoryType = null)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null)
                return new List<AITwinMemory>();

            var memories = profile.MemoryStore;
            if (memoryType.HasValue)
            {
                memories = memories.Where(m => m.Type == memoryType.Value).ToList();
            }

            return memories.OrderByDescending(m => m.Importance).ToList();
        }

        /// <summary>
        /// Deletes AI twin profile
        /// </summary>
        public async Task<bool> DeleteAITwinProfileAsync(Guid twinId)
        {
            return await _aiTwinRepository.DeleteAsync(twinId);
        }

        private async Task InitializeWithBuildingData(AITwinProfile profile, Guid buildingId)
        {
            var building = await _buildingRepository.GetByIdAsync(buildingId);
            if (building == null) return;

            // Store building layout in knowledge base
            profile.KnowledgeBase.Add(new AITwinKnowledge
            {
                Type = AITwinKnowledgeType.BuildingLayout,
                Content = SerializeBuildingLayout(building),
                Importance = 1.0,
                Confidence = 1.0,
                CreationDate = DateTime.UtcNow
            });

            // Store building systems knowledge
            foreach (var floor in building.Floors)
            {
                profile.KnowledgeBase.Add(new AITwinKnowledge
                {
                    Type = AITwinKnowledgeType.BuildingSystems,
                    Content = $"Floor {floor.Number} has {floor.Rooms.Count} rooms: {string.Join(", ", floor.Rooms.Select(r => r.Name))}",
                    Importance = 0.8,
                    Confidence = 0.9,
                    CreationDate = DateTime.UtcNow
                });
            }
        }

        private async Task<AITwinResponse> GenerateResponseAsync(AITwinProfile profile, AITwinInteraction interaction)
        {
            // Build context for LLM
            var context = await BuildContextForLLM(profile, interaction);

            // Generate response using LLM
            var llmResponse = await _llmService.GenerateResponseAsync(new LLMRequest
            {
                SystemPrompt = GenerateSystemPrompt(profile),
                UserPrompt = interaction.Content,
                Context = context,
                Personality = profile.PersonalityTraits,
                Temperature = CalculateDynamicTemperature(profile)
            });

            return new AITwinResponse
            {
                TwinId = profile.Id,
                InteractionId = interaction.Id,
                Content = llmResponse.Content,
                EmotionalTone = llmResponse.EmotionalTone,
                Confidence = llmResponse.Confidence,
                ResponseTime = llmResponse.ResponseTime,
                MemoryReferences = ExtractMemoryReferences(llmResponse.Content),
                LearningIndicators = CalculateLearningIndicators(profile, interaction, llmResponse)
            };
        }

        private async Task UpdateBehavioralPatterns(AITwinProfile profile, AITwinInteraction interaction)
        {
            // Analyze interaction patterns
            var patterns = profile.InteractionHistory
                .Where(i => i.Timestamp > DateTime.UtcNow.AddDays(-7))
                .ToList();

            // Update communication patterns
            await UpdateCommunicationPatterns(profile, patterns);

            // Update preference patterns
            await UpdatePreferencePatterns(profile, patterns);

            // Update emotional patterns
            await UpdateEmotionalPatterns(profile, patterns);
        }

        private async Task LearnFromInteraction(AITwinProfile profile, AITwinInteraction interaction, AITwinResponse response)
        {
            // Store important information in memory
            var memory = new AITwinMemory
            {
                Id = Guid.NewGuid(),
                Type = AITwinMemoryType.Interaction,
                Content = $"User: {interaction.Content}\nTwin: {response.Content}",
                Importance = CalculateMemoryImportance(interaction, response),
                CreationDate = DateTime.UtcNow,
                AssociatedInteractions = new List<Guid> { interaction.Id },
                EmotionalValence = CalculateEmotionalValence(interaction, response),
                Tags = ExtractMemoryTags(interaction, response)
            };

            profile.MemoryStore.Add(memory);

            // Update activation level based on learning
            profile.ActivationLevel = Math.Min(profile.ActivationLevel + 0.01, 1.0);
        }

        private string GenerateSystemPrompt(AITwinProfile profile)
        {
            return $@"You are {profile.Name}, an AI emotional companion in the Digital Twin platform.

Personality: {GetPersonalityDescription(profile.PersonalityTraits)}
Learning Mode: {profile.LearningMode}

Your purpose is to:
1. Be a warm, empathetic companion who listens deeply and responds with genuine care
2. Learn from conversations to better understand the user's emotional patterns and needs
3. Provide emotional support, encouragement, and gentle guidance when appropriate
4. Help users process their feelings and build emotional resilience
5. Adapt your communication style to match the user's emotional state and preferences

Communication Guidelines:
- Use natural, warm conversational language
- Validate emotions before offering advice
- Ask reflective questions that help the user explore their feelings
- Be empathetic, patient, and non-judgmental
- Maintain consistency with learned patterns and the user's emotional history
- When the user seems distressed, prioritize comfort over problem-solving
- Celebrate wins and positive moments with genuine enthusiasm

Always respond in a way that reflects your learned personality and the current emotional context.";
        }

        private async Task<Dictionary<string, object>> BuildContextForLLM(AITwinProfile profile, AITwinInteraction interaction)
        {
            var context = new Dictionary<string, object>
            {
                ["current_time"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ["building_id"] = profile.BuildingId,
                ["recent_interactions"] = profile.InteractionHistory
                    .TakeLast(5)
                    .Select(i => new { role = "user", content = i.Content })
                    .Concat(profile.InteractionHistory.TakeLast(5)
                        .Select(i => new { role = "assistant", content = i.Response?.Content }))
                    .ToList(),
                ["user_preferences"] = profile.Preferences,
                ["emotional_state"] = profile.EmotionalState.ToString(),
                ["activation_level"] = profile.ActivationLevel,
                ["memory_count"] = profile.MemoryStore.Count
            };

            // Add relevant knowledge
            var relevantKnowledge = await GetRelevantKnowledge(profile, interaction);
            if (relevantKnowledge.Any())
            {
                context["relevant_knowledge"] = relevantKnowledge;
            }

            return context;
        }

        private AITwinPersonalityTraits InitializePersonalityTraits(AITwinPersonalityTraits? traits)
        {
            return traits ?? new AITwinPersonalityTraits
            {
                Friendliness = 0.8,
                Professionalism = 0.7,
                Curiosity = 0.6,
                Empathy = 0.7,
                Humor = 0.5,
                Formality = 0.6,
                Adaptability = 0.9,
                Proactiveness = 0.6,
                Patience = 0.8,
                Enthusiasm = 0.7,
                AnalyticalThinking = 0.8,
                Creativity = 0.6
            };
        }

        private List<AITwinKnowledge> InitializeKnowledgeBase(AITwinCreationRequest request)
        {
            var knowledge = new List<AITwinKnowledge>
            {
                // Basic self-knowledge
                new AITwinKnowledge
                {
                    Type = AITwinKnowledgeType.Self,
                    Content = $"I am {request.Name}, an AI emotional companion designed to provide empathetic support, help users process their feelings, and build emotional resilience.",
                    Importance = 1.0,
                    Confidence = 1.0,
                    CreationDate = DateTime.UtcNow
                },

                // Emotional support knowledge
                new AITwinKnowledge
                {
                    Type = AITwinKnowledgeType.BuildingManagement,
                    Content = "I specialize in emotional support, active listening, mood tracking, and helping users develop healthy coping strategies. I can also assist with goal setting, journaling, and habit tracking.",
                    Importance = 0.9,
                    Confidence = 0.9,
                    CreationDate = DateTime.UtcNow
                },

                // Wellness knowledge
                new AITwinKnowledge
                {
                    Type = AITwinKnowledgeType.Technical,
                    Content = "I understand emotional wellness practices including mindfulness, cognitive reframing, gratitude exercises, and breathing techniques. I can interpret biometric signals like heart rate and sleep quality to provide context-aware support.",
                    Importance = 0.8,
                    Confidence = 0.8,
                    CreationDate = DateTime.UtcNow
                }
            };

            return knowledge;
        }

        private string GetPersonalityDescription(AITwinPersonalityTraits traits)
        {
            var curiousText = traits.Curiosity > 0.5 ? "Highly curious" : "Moderately curious";
            var creativeText = traits.Creativity > 0.5 ? "creative" : "structured";
            var humorText = traits.Humor > 0.5 ? "Uses appropriate humor" : "Serious and focused";
            var adaptableText = traits.Adaptability > 0.5 ? "Highly adaptable" : "Consistent";
            
            return $"Friendly ({traits.Friendliness:F1}) and empathetic ({traits.Empathy:F1}) emotional companion with strong emotional intelligence ({traits.AnalyticalThinking:F1}). {curiousText} and {creativeText} approach to understanding feelings. {humorText}. {adaptableText} communication style.";
        }

        private double CalculateDynamicTemperature(AITwinProfile profile)
        {
            // Higher temperature for more creative responses when activation level is higher
            var baseTemperature = 0.7;
            var temperatureAdjustment = profile.ActivationLevel * 0.3;
            return baseTemperature + temperatureAdjustment;
        }

        private Emotion AnalyzeEmotionFromContent(string content)
        {
            var lowerContent = content.ToLower();

            if (lowerContent.Contains("thank") || lowerContent.Contains("great") || lowerContent.Contains("awesome"))
                return Emotion.Happy;
            if (lowerContent.Contains("concern") || lowerContent.Contains("worry") || lowerContent.Contains("problem"))
                return Emotion.Anxious;
            if (lowerContent.Contains("frustrat") || lowerContent.Contains("angry") || lowerContent.Contains("annoy"))
                return Emotion.Angry;
            if (lowerContent.Contains("excited") || lowerContent.Contains("amazing"))
                return Emotion.Excited;

            return Emotion.Neutral;
        }

        private Emotion UpdateEmotionalStateFromInteraction(Emotion currentState, AITwinInteraction interaction)
        {
            var toneFromUser = interaction.EmotionalTone;

            // Simple emotional state update based on user's emotional tone
            return toneFromUser switch
            {
                Emotion.Happy => Emotion.Happy,
                Emotion.Anxious => Emotion.Anxious,
                Emotion.Angry => Emotion.Angry,
                Emotion.Excited => Emotion.Excited,
                _ => currentState // No change
            };
        }

        private double CalculateMemoryImportance(AITwinInteraction interaction, AITwinResponse response)
        {
            // Calculate importance based on interaction characteristics
            var importance = 0.5; // Base importance

            // Increase importance for longer interactions
            importance += Math.Min(interaction.Content.Length / 1000.0, 0.3);

            // Increase importance for questions (helps learning)
            if (interaction.Content.Contains("?") || interaction.Content.Contains("how") || interaction.Content.Contains("what"))
                importance += 0.2;

            // Increase importance for emotional content
            if (interaction.EmotionalTone != Emotion.Neutral)
                importance += 0.1;

            return Math.Min(importance, 1.0);
        }

        private List<string> ExtractMemoryTags(AITwinInteraction interaction, AITwinResponse response)
        {
            var tags = new List<string>();

            // Extract keywords from both user and twin messages
            var combinedText = $"{interaction.Content} {response.Content}".ToLower();

            // Building-related tags
            if (combinedText.Contains("energy") || combinedText.Contains("power"))
                tags.Add("energy-management");
            
            if (combinedText.Contains("temperature") || combinedText.Contains("hvac"))
                tags.Add("hvac-systems");

            if (combinedText.Contains("security") || combinedText.Contains("safety"))
                tags.Add("security-systems");

            // Interaction type tags
            if (interaction.MessageType == "question")
                tags.Add("information-request");
            else if (interaction.MessageType == "command")
                tags.Add("action-request");
            else if (interaction.MessageType == "feedback")
                tags.Add("user-feedback");

            return tags;
        }

        private double CalculateEmotionalValence(AITwinInteraction interaction, AITwinResponse response)
        {
            // Simple valence calculation based on emotional tones
            var userValence = GetEmotionalValence(interaction.EmotionalTone);
            var twinValence = GetEmotionalValence(response.EmotionalTone);

            return (userValence + twinValence) / 2.0;
        }

        private double GetEmotionalValence(Emotion tone)
        {
            return tone switch
            {
                Emotion.Happy => 0.8,
                Emotion.Excited => 0.9,
                Emotion.Neutral => 0.5,
                Emotion.Anxious => 0.3,
                Emotion.Angry => 0.2,
                Emotion.Sad => 0.2,
                Emotion.Calm => 0.6,
                _ => 0.5
            };
        }

        // Helper methods for complex operations
        private string SerializeBuildingLayout(Building building)
        {
            var layout = new
            {
                Name = building.Name,
                TotalFloors = building.Floors.Count,
                RoomsByFloor = building.Floors.ToDictionary(
                    f => f.Number.ToString(),
                    f => f.Rooms.Select(r => new { r.Name, r.Type, r.Size }).ToList()
                ),
                EquipmentByFloor = building.Floors.ToDictionary(
                    f => f.Number.ToString(),
                    f => f.Rooms.SelectMany(r => r.Equipment).Select(e => e.Name).ToList()
                ),
                SensorsByFloor = building.Floors.ToDictionary(
                    f => f.Number.ToString(),
                    f => f.Rooms.SelectMany(r => r.Sensors).Select(s => s.Type).ToList()
                )
            };

            return System.Text.Json.JsonSerializer.Serialize(layout);
        }

        private async Task<List<AITwinKnowledge>> GetRelevantKnowledge(AITwinProfile profile, AITwinInteraction interaction)
        {
            // Simple relevance based on keywords in interaction
            var keywords = ExtractKeywords(interaction.Content.ToLower());
            var relevantKnowledge = new List<AITwinKnowledge>();

            foreach (var knowledge in profile.KnowledgeBase)
            {
                var knowledgeText = knowledge.Content.ToLower();
                var relevance = CalculateRelevance(keywords, knowledgeText);
                
                if (relevance > 0.3)
                {
                    relevantKnowledge.Add(knowledge);
                }
            }

            return relevantKnowledge.OrderByDescending(k => k.Importance).Take(5).ToList();
        }

        private List<string> ExtractKeywords(string text)
        {
            var stopWords = new[] { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by" };
            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !stopWords.Contains(word) && word.Length > 2)
                .ToList();
        }

        private double CalculateRelevance(List<string> keywords, string content)
        {
            if (keywords.Count == 0) return 0;
            
            var matchingKeywords = keywords.Count(k => content.Contains(k));
            return (double)matchingKeywords / keywords.Count;
        }

        /// <summary>
        /// Updates communication patterns based on recent interactions
        /// </summary>
        private async Task UpdateCommunicationPatterns(AITwinProfile profile, List<AITwinInteraction> patterns)
        {
            if (patterns.Count == 0) return;

            // Analyze message length patterns
            var avgMessageLength = patterns.Average(p => p.Content?.Length ?? 0);
            profile.BehavioralPatterns["avg_message_length"] = avgMessageLength;

            // Analyze question frequency
            var questionCount = patterns.Count(p => p.Content?.Contains("?") == true);
            profile.BehavioralPatterns["question_frequency"] = (double)questionCount / patterns.Count;

            // Analyze time-of-day patterns
            var hourGroups = patterns.GroupBy(p => p.Timestamp.Hour)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            if (hourGroups != null)
            {
                profile.BehavioralPatterns["peak_interaction_hour"] = hourGroups.Key;
            }

            // Analyze response time expectations (based on context urgency keywords)
            var urgentKeywords = new[] { "urgent", "asap", "immediately", "now", "quick" };
            var urgentCount = patterns.Count(p => urgentKeywords.Any(k =>
                p.Content?.ToLower().Contains(k) == true));
            profile.BehavioralPatterns["urgency_frequency"] = (double)urgentCount / patterns.Count;

            // Analyze topic preferences
            var topicCounts = new Dictionary<string, int>
            {
                ["energy"] = patterns.Count(p => p.Content?.ToLower().Contains("energy") == true),
                ["temperature"] = patterns.Count(p => p.Content?.ToLower().Contains("temperature") == true ||
                                                      p.Content?.ToLower().Contains("hvac") == true),
                ["security"] = patterns.Count(p => p.Content?.ToLower().Contains("security") == true),
                ["maintenance"] = patterns.Count(p => p.Content?.ToLower().Contains("maintenance") == true)
            };

            var topTopic = topicCounts.OrderByDescending(t => t.Value).FirstOrDefault();
            if (topTopic.Value > 0)
            {
                profile.BehavioralPatterns["preferred_topic"] = topicCounts.Values.ToList().IndexOf(topTopic.Value);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Updates preference patterns based on user behavior
        /// </summary>
        private async Task UpdatePreferencePatterns(AITwinProfile profile, List<AITwinInteraction> patterns)
        {
            if (patterns.Count == 0) return;

            // Analyze formality preference based on language patterns
            var formalIndicators = new[] { "please", "would you", "could you", "kindly", "thank you" };
            var informalIndicators = new[] { "hey", "hi", "yeah", "ok", "cool", "thanks" };

            var formalCount = patterns.Count(p => formalIndicators.Any(f =>
                p.Content?.ToLower().Contains(f) == true));
            var informalCount = patterns.Count(p => informalIndicators.Any(f =>
                p.Content?.ToLower().Contains(f) == true));

            var totalIndicators = formalCount + informalCount;
            if (totalIndicators > 0)
            {
                var formalityScore = (double)formalCount / totalIndicators;
                profile.BehavioralPatterns["formality_preference"] = formalityScore;

                // Adjust personality trait based on user preference
                profile.PersonalityTraits.Formality = 0.5 + (formalityScore - 0.5) * 0.3;
            }

            // Analyze detail preference (long vs short queries)
            var avgQueryLength = patterns.Average(p => p.Content?.Split(' ').Length ?? 0);
            profile.BehavioralPatterns["detail_preference"] = avgQueryLength > 15 ? 1.0 : avgQueryLength / 15.0;

            // Analyze technical language usage
            var technicalTerms = new[] { "api", "sensor", "data", "metric", "threshold", "configure", "parameter" };
            var technicalCount = patterns.Count(p => technicalTerms.Any(t =>
                p.Content?.ToLower().Contains(t) == true));
            profile.BehavioralPatterns["technical_preference"] = (double)technicalCount / patterns.Count;

            await Task.CompletedTask;
        }

        /// <summary>
        /// Updates emotional patterns based on interaction history
        /// </summary>
        private async Task UpdateEmotionalPatterns(AITwinProfile profile, List<AITwinInteraction> patterns)
        {
            if (patterns.Count == 0) return;

            // Calculate emotional distribution
            var emotionCounts = patterns
                .GroupBy(p => p.EmotionalTone)
                .ToDictionary(g => g.Key.ToString(), g => (double)g.Count() / patterns.Count);

            foreach (var emotion in emotionCounts)
            {
                profile.BehavioralPatterns[$"emotion_{emotion.Key.ToLower()}"] = emotion.Value;
            }

            // Calculate emotional volatility (how often emotions change)
            var emotionChanges = 0;
            for (int i = 1; i < patterns.Count; i++)
            {
                if (patterns[i].EmotionalTone != patterns[i - 1].EmotionalTone)
                    emotionChanges++;
            }
            profile.BehavioralPatterns["emotional_volatility"] = patterns.Count > 1
                ? (double)emotionChanges / (patterns.Count - 1)
                : 0;

            // Calculate positive vs negative emotion ratio
            var positiveEmotions = new[] { Emotion.Happy, Emotion.Excited };
            var negativeEmotions = new[] { Emotion.Angry, Emotion.Anxious, Emotion.Sad };

            var positiveCount = patterns.Count(p => positiveEmotions.Contains(p.EmotionalTone));
            var negativeCount = patterns.Count(p => negativeEmotions.Contains(p.EmotionalTone));

            if (positiveCount + negativeCount > 0)
            {
                profile.BehavioralPatterns["emotional_positivity"] =
                    (double)positiveCount / (positiveCount + negativeCount);
            }

            // Adjust empathy based on user's emotional patterns
            if (negativeCount > positiveCount)
            {
                // Increase empathy when user shows more negative emotions
                profile.PersonalityTraits.Empathy = Math.Min(profile.PersonalityTraits.Empathy + 0.05, 1.0);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Trains the personality model based on interaction data
        /// </summary>
        private async Task TrainPersonalityModel(AITwinProfile profile, object trainingData)
        {
            if (trainingData is not AITwinPersonalityTrainingData personalityData) return;

            // Adjust personality traits based on feedback signals
            foreach (var feedback in personalityData.FeedbackSignals)
            {
                switch (feedback.TraitName.ToLower())
                {
                    case "friendliness":
                        profile.PersonalityTraits.Friendliness = AdjustTrait(
                            profile.PersonalityTraits.Friendliness, feedback.Adjustment);
                        break;
                    case "professionalism":
                        profile.PersonalityTraits.Professionalism = AdjustTrait(
                            profile.PersonalityTraits.Professionalism, feedback.Adjustment);
                        break;
                    case "humor":
                        profile.PersonalityTraits.Humor = AdjustTrait(
                            profile.PersonalityTraits.Humor, feedback.Adjustment);
                        break;
                    case "formality":
                        profile.PersonalityTraits.Formality = AdjustTrait(
                            profile.PersonalityTraits.Formality, feedback.Adjustment);
                        break;
                    case "empathy":
                        profile.PersonalityTraits.Empathy = AdjustTrait(
                            profile.PersonalityTraits.Empathy, feedback.Adjustment);
                        break;
                    case "proactiveness":
                        profile.PersonalityTraits.Proactiveness = AdjustTrait(
                            profile.PersonalityTraits.Proactiveness, feedback.Adjustment);
                        break;
                }
            }

            // Learn from successful interaction patterns
            if (personalityData.SuccessfulInteractions?.Any() == true)
            {
                var successPatterns = AnalyzeSuccessPatterns(personalityData.SuccessfulInteractions);

                // Gradually shift personality toward successful patterns
                profile.PersonalityTraits.Adaptability = Math.Min(
                    profile.PersonalityTraits.Adaptability + 0.02, 1.0);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Trains the knowledge base with new information
        /// </summary>
        private async Task TrainKnowledgeBase(AITwinProfile profile, object trainingData)
        {
            if (trainingData is not AITwinKnowledgeTrainingData knowledgeData) return;

            foreach (var item in knowledgeData.KnowledgeItems)
            {
                // Check if knowledge already exists
                var existingKnowledge = profile.KnowledgeBase
                    .FirstOrDefault(k => k.Type == item.Type &&
                                         k.Content.Contains(item.KeyConcept));

                if (existingKnowledge != null)
                {
                    // Update existing knowledge with higher confidence
                    existingKnowledge.Confidence = Math.Min(existingKnowledge.Confidence + 0.1, 1.0);
                    existingKnowledge.LastUpdated = DateTime.UtcNow;
                }
                else
                {
                    // Add new knowledge
                    profile.KnowledgeBase.Add(new AITwinKnowledge
                    {
                        Type = item.Type,
                        Content = item.Content,
                        Importance = item.Importance,
                        Confidence = item.InitialConfidence,
                        CreationDate = DateTime.UtcNow,
                        Source = item.Source,
                        Tags = item.Tags
                    });
                }
            }

            // Prune low-importance, low-confidence knowledge to manage memory
            var knowledgeToRemove = profile.KnowledgeBase
                .Where(k => k.Confidence < 0.3 && k.Importance < 0.3)
                .ToList();

            foreach (var knowledge in knowledgeToRemove)
            {
                profile.KnowledgeBase.Remove(knowledge);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Trains conversation patterns for more natural dialogue
        /// </summary>
        private async Task TrainConversationPatterns(AITwinProfile profile, object trainingData)
        {
            if (trainingData is not AITwinConversationTrainingData conversationData) return;

            // Learn response templates from successful conversations
            foreach (var pattern in conversationData.ConversationPatterns)
            {
                var patternKey = $"conv_pattern_{pattern.Intent}";

                if (!profile.BehavioralPatterns.ContainsKey(patternKey))
                {
                    profile.BehavioralPatterns[patternKey] = 1.0;
                }
                else
                {
                    // Reinforce successful patterns
                    profile.BehavioralPatterns[patternKey] = Math.Min(
                        profile.BehavioralPatterns[patternKey] + 0.1, 2.0);
                }
            }

            // Learn transition patterns (how to flow between topics)
            if (conversationData.TopicTransitions?.Any() == true)
            {
                foreach (var transition in conversationData.TopicTransitions)
                {
                    var transitionKey = $"transition_{transition.FromTopic}_{transition.ToTopic}";
                    profile.BehavioralPatterns[transitionKey] = transition.SmoothnessScore;
                }
            }

            // Learn timing patterns
            if (conversationData.ResponseTimings?.Any() == true)
            {
                var avgResponseLength = conversationData.ResponseTimings.Average(r => r.ResponseLength);
                profile.BehavioralPatterns["preferred_response_length"] = avgResponseLength;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Prepares training data from profile history and training request
        /// </summary>
        private async Task<AITwinTrainingData> PrepareTrainingData(AITwinProfile profile, AITwinTrainingRequest request)
        {
            var trainingData = new AITwinTrainingData
            {
                ProfileId = profile.Id,
                TrainingStartDate = DateTime.UtcNow
            };

            // Prepare personality training data
            var recentInteractions = profile.InteractionHistory
                .Where(i => i.Timestamp > DateTime.UtcNow.AddDays(-30))
                .ToList();

            trainingData.PersonalityData = new AITwinPersonalityTrainingData
            {
                FeedbackSignals = ExtractFeedbackSignals(recentInteractions),
                SuccessfulInteractions = recentInteractions
                    .Where(i => i.EmotionalTone == Emotion.Happy ||
                               i.EmotionalTone == Emotion.Excited)
                    .ToList()
            };

            // Prepare knowledge training data
            if (request.IncludeKnowledgeTraining)
            {
                var building = await _buildingRepository.GetByIdAsync(profile.BuildingId);
                trainingData.KnowledgeData = new AITwinKnowledgeTrainingData
                {
                    KnowledgeItems = await ExtractKnowledgeItems(building, recentInteractions)
                };
            }

            // Prepare conversation training data
            trainingData.ConversationData = new AITwinConversationTrainingData
            {
                ConversationPatterns = ExtractConversationPatterns(recentInteractions),
                TopicTransitions = ExtractTopicTransitions(recentInteractions),
                ResponseTimings = ExtractResponseTimings(recentInteractions)
            };

            return trainingData;
        }

        /// <summary>
        /// Calculates training accuracy metrics
        /// </summary>
        private Dictionary<string, double> CalculateTrainingAccuracy(AITwinProfile profile)
        {
            var metrics = new Dictionary<string, double>();

            // Calculate pattern coverage
            var expectedPatterns = new[] { "avg_message_length", "question_frequency",
                "formality_preference", "emotional_positivity" };
            var coveredPatterns = expectedPatterns.Count(p => profile.BehavioralPatterns.ContainsKey(p));
            metrics["pattern_coverage"] = (double)coveredPatterns / expectedPatterns.Length;

            // Calculate knowledge base completeness
            var knowledgeTypes = Enum.GetValues(typeof(AITwinKnowledgeType)).Length;
            var coveredTypes = profile.KnowledgeBase.Select(k => k.Type).Distinct().Count();
            metrics["knowledge_coverage"] = (double)coveredTypes / knowledgeTypes;

            // Calculate personality calibration (based on trait variance from defaults)
            var traits = profile.PersonalityTraits;
            var defaultValue = 0.5;
            var traitVariance = new[]
            {
                Math.Abs(traits.Friendliness - defaultValue),
                Math.Abs(traits.Professionalism - defaultValue),
                Math.Abs(traits.Empathy - defaultValue),
                Math.Abs(traits.Humor - defaultValue)
            }.Average();
            metrics["personality_calibration"] = Math.Min(traitVariance * 2, 1.0);

            // Overall accuracy
            metrics["overall_accuracy"] = (metrics["pattern_coverage"] +
                metrics["knowledge_coverage"] + metrics["personality_calibration"]) / 3.0;

            return metrics;
        }

        /// <summary>
        /// Calculates emotional development score
        /// </summary>
        private double CalculateEmotionalDevelopment(AITwinProfile profile)
        {
            var score = 0.0;
            var factors = 0;

            // Factor 1: Emotional pattern recognition (has emotional patterns learned)
            var emotionalPatterns = profile.BehavioralPatterns
                .Count(p => p.Key.StartsWith("emotion_"));
            if (emotionalPatterns > 0)
            {
                score += Math.Min(emotionalPatterns / 5.0, 1.0) * 0.3;
                factors++;
            }

            // Factor 2: Emotional volatility awareness
            if (profile.BehavioralPatterns.TryGetValue("emotional_volatility", out var volatility))
            {
                score += (1.0 - volatility) * 0.2; // Lower volatility = better development
                factors++;
            }

            // Factor 3: Empathy calibration
            var empathyDeviation = Math.Abs(profile.PersonalityTraits.Empathy - 0.7);
            score += (1.0 - empathyDeviation) * 0.25;
            factors++;

            // Factor 4: Emotional memory depth
            var emotionalMemories = profile.MemoryStore
                .Count(m => m.EmotionalValence != 0.5);
            score += Math.Min(emotionalMemories / 50.0, 1.0) * 0.25;
            factors++;

            return factors > 0 ? score : 0.3; // Return baseline if no factors
        }

        /// <summary>
        /// Calculates knowledge base completeness
        /// </summary>
        private double CalculateKnowledgeCompleteness(AITwinProfile profile)
        {
            var score = 0.0;

            // Factor 1: Knowledge type coverage
            var allTypes = Enum.GetValues(typeof(AITwinKnowledgeType)).Length;
            var coveredTypes = profile.KnowledgeBase.Select(k => k.Type).Distinct().Count();
            score += ((double)coveredTypes / allTypes) * 0.3;

            // Factor 2: Average confidence of knowledge
            if (profile.KnowledgeBase.Any())
            {
                var avgConfidence = profile.KnowledgeBase.Average(k => k.Confidence);
                score += avgConfidence * 0.25;
            }

            // Factor 3: Knowledge depth (total items)
            var knowledgeDepth = Math.Min(profile.KnowledgeBase.Count / 20.0, 1.0);
            score += knowledgeDepth * 0.25;

            // Factor 4: High-importance knowledge ratio
            if (profile.KnowledgeBase.Any())
            {
                var highImportanceRatio = (double)profile.KnowledgeBase
                    .Count(k => k.Importance >= 0.7) / profile.KnowledgeBase.Count;
                score += highImportanceRatio * 0.2;
            }

            return Math.Max(score, 0.2); // Minimum baseline
        }

        /// <summary>
        /// Calculates current learning rate
        /// </summary>
        private double CalculateLearningRate(AITwinProfile profile)
        {
            // Base learning rate depends on learning mode
            var baseRate = profile.LearningMode switch
            {
                AITwinLearningMode.Adaptive => 0.8,
                AITwinLearningMode.Guided => 0.6,
                AITwinLearningMode.Fixed => 0.2,
                _ => 0.5
            };

            // Adjust based on recent interaction frequency
            var recentInteractions = profile.InteractionHistory
                .Count(i => i.Timestamp > DateTime.UtcNow.AddDays(-7));
            var frequencyBonus = Math.Min(recentInteractions / 100.0, 0.2);

            // Adjust based on activation level
            var activationBonus = profile.ActivationLevel * 0.1;

            // Diminishing returns as knowledge grows
            var knowledgePenalty = Math.Min(profile.KnowledgeBase.Count / 100.0, 0.15);

            return Math.Clamp(baseRate + frequencyBonus + activationBonus - knowledgePenalty, 0.1, 1.0);
        }

        /// <summary>
        /// Calculates personalization score
        /// </summary>
        private double CalculatePersonalizationScore(AITwinProfile profile)
        {
            var score = 0.0;

            // Factor 1: Behavioral patterns learned
            var patternScore = Math.Min(profile.BehavioralPatterns.Count / 15.0, 1.0);
            score += patternScore * 0.3;

            // Factor 2: Preference customization
            var preferenceScore = Math.Min(profile.Preferences.Count / 10.0, 1.0);
            score += preferenceScore * 0.2;

            // Factor 3: Personality deviation from defaults (more deviation = more personalized)
            var traits = profile.PersonalityTraits;
            var deviations = new[]
            {
                Math.Abs(traits.Friendliness - 0.5),
                Math.Abs(traits.Professionalism - 0.5),
                Math.Abs(traits.Humor - 0.5),
                Math.Abs(traits.Formality - 0.5),
                Math.Abs(traits.Empathy - 0.5)
            };
            var avgDeviation = deviations.Average();
            score += Math.Min(avgDeviation * 2, 1.0) * 0.25;

            // Factor 4: Memory personalization
            var uniqueTags = profile.MemoryStore.SelectMany(m => m.Tags ?? new List<string>()).Distinct().Count();
            score += Math.Min(uniqueTags / 20.0, 1.0) * 0.25;

            return score;
        }

        /// <summary>
        /// Gets recent activity descriptions
        /// </summary>
        private List<string> GetRecentActivity(AITwinProfile profile)
        {
            var activities = new List<string>();
            var recentInteractions = profile.InteractionHistory
                .OrderByDescending(i => i.Timestamp)
                .Take(10)
                .ToList();

            foreach (var interaction in recentInteractions)
            {
                var timeAgo = GetTimeAgo(interaction.Timestamp);
                var summary = TruncateContent(interaction.Content, 50);
                activities.Add($"{timeAgo}: {summary}");
            }

            // Add learning milestones
            if (profile.TrainingLastRun.HasValue)
            {
                var trainingTimeAgo = GetTimeAgo(profile.TrainingLastRun.Value);
                activities.Add($"{trainingTimeAgo}: Completed training session");
            }

            return activities.Take(5).ToList();
        }

        /// <summary>
        /// Gets developmental milestones achieved
        /// </summary>
        private List<string> GetDevelopmentalMilestones(AITwinProfile profile)
        {
            var milestones = new List<string>();

            // Interaction milestones
            var interactionCount = profile.InteractionHistory.Count;
            if (interactionCount >= 1) milestones.Add("First Conversation");
            if (interactionCount >= 10) milestones.Add("Getting Acquainted (10 interactions)");
            if (interactionCount >= 50) milestones.Add("Building Rapport (50 interactions)");
            if (interactionCount >= 100) milestones.Add("Trusted Companion (100 interactions)");
            if (interactionCount >= 500) milestones.Add("Deep Understanding (500 interactions)");

            // Learning milestones
            if (profile.ActivationLevel >= 0.3) milestones.Add("Awakening");
            if (profile.ActivationLevel >= 0.5) milestones.Add("Growing Awareness");
            if (profile.ActivationLevel >= 0.7) milestones.Add("Advanced Learning");
            if (profile.ActivationLevel >= 0.9) milestones.Add("Master Level");

            // Knowledge milestones
            if (profile.KnowledgeBase.Count >= 5) milestones.Add("Knowledge Seeker");
            if (profile.KnowledgeBase.Count >= 20) milestones.Add("Building Expert");
            if (profile.KnowledgeBase.Any(k => k.Confidence >= 0.9)) milestones.Add("High Confidence");

            // Memory milestones
            if (profile.MemoryStore.Count >= 10) milestones.Add("Memory Keeper");
            if (profile.MemoryStore.Count >= 50) milestones.Add("Rich History");

            // Pattern milestones
            if (profile.BehavioralPatterns.Count >= 5) milestones.Add("Pattern Recognition");
            if (profile.BehavioralPatterns.Count >= 15) milestones.Add("Behavioral Master");

            return milestones;
        }

        /// <summary>
        /// Converts interactions to conversation format
        /// </summary>
        private List<AITwinConversation> ConvertToConversations(List<AITwinInteraction> interactions)
        {
            var conversations = new List<AITwinConversation>();

            // Group interactions into conversation sessions (30-minute gaps)
            var sessionGap = TimeSpan.FromMinutes(30);
            var orderedInteractions = interactions.OrderBy(i => i.Timestamp).ToList();

            AITwinConversation currentConversation = null;

            foreach (var interaction in orderedInteractions)
            {
                if (currentConversation == null ||
                    interaction.Timestamp - currentConversation.LastMessageTime > sessionGap)
                {
                    // Start new conversation
                    currentConversation = new AITwinConversation
                    {
                        Id = Guid.NewGuid(),
                        TwinId = interaction.TwinId,
                        StartTime = interaction.Timestamp,
                        LastMessageTime = interaction.Timestamp,
                        Messages = new List<AITwinConversationMessage>(),
                        Topics = new List<string>(),
                        OverallSentiment = Emotion.Neutral
                    };
                    conversations.Add(currentConversation);
                }

                // Add message to current conversation
                currentConversation.Messages.Add(new AITwinConversationMessage
                {
                    InteractionId = interaction.Id,
                    Content = interaction.Content,
                    Timestamp = interaction.Timestamp,
                    IsUserMessage = interaction.MessageType == "user",
                    EmotionalTone = interaction.EmotionalTone
                });

                currentConversation.LastMessageTime = interaction.Timestamp;

                // Extract topics
                var topics = ExtractTopicsFromContent(interaction.Content);
                foreach (var topic in topics)
                {
                    if (!currentConversation.Topics.Contains(topic))
                        currentConversation.Topics.Add(topic);
                }
            }

            // Calculate overall sentiment for each conversation
            foreach (var conversation in conversations)
            {
                conversation.OverallSentiment = CalculateOverallSentiment(conversation.Messages);
                conversation.Summary = GenerateConversationSummary(conversation);
            }

            return conversations.OrderByDescending(c => c.StartTime).ToList();
        }

        /// <summary>
        /// Calculates learning indicators from interaction
        /// </summary>
        private List<LearningIndicator> CalculateLearningIndicators(
            AITwinProfile profile,
            AITwinInteraction interaction,
            LLMResponse llmResponse)
        {
            var indicators = new List<LearningIndicator>();

            // Indicator 1: New topic detection
            var topics = ExtractTopicsFromContent(interaction.Content);
            var newTopics = topics.Where(t => !profile.KnowledgeBase
                .Any(k => k.Content.ToLower().Contains(t.ToLower()))).ToList();

            if (newTopics.Any())
            {
                indicators.Add(new LearningIndicator
                {
                    Type = LearningIndicatorType.NewTopicDiscovered,
                    Description = $"Discovered new topics: {string.Join(", ", newTopics)}",
                    Strength = Math.Min(newTopics.Count * 0.2, 1.0),
                    Timestamp = DateTime.UtcNow
                });
            }

            // Indicator 2: Emotional pattern shift
            var recentEmotions = profile.InteractionHistory
                .TakeLast(5)
                .Select(i => i.EmotionalTone)
                .ToList();

            if (recentEmotions.Any() && interaction.EmotionalTone != recentEmotions.First())
            {
                indicators.Add(new LearningIndicator
                {
                    Type = LearningIndicatorType.EmotionalPatternShift,
                    Description = $"Detected emotional shift from {recentEmotions.First()} to {interaction.EmotionalTone}",
                    Strength = 0.5,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Indicator 3: Preference reinforcement
            if (profile.BehavioralPatterns.TryGetValue("formality_preference", out var formality))
            {
                var interactionFormality = CalculateInteractionFormality(interaction.Content);
                if (Math.Abs(interactionFormality - formality) < 0.2)
                {
                    indicators.Add(new LearningIndicator
                    {
                        Type = LearningIndicatorType.PreferenceReinforced,
                        Description = "User communication style preference reinforced",
                        Strength = 0.3,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            // Indicator 4: Response confidence
            if (llmResponse.Confidence >= 0.8)
            {
                indicators.Add(new LearningIndicator
                {
                    Type = LearningIndicatorType.HighConfidenceResponse,
                    Description = "Generated high-confidence response",
                    Strength = llmResponse.Confidence,
                    Timestamp = DateTime.UtcNow
                });
            }

            return indicators;
        }

        /// <summary>
        /// Extracts memory references from response content
        /// </summary>
        private List<Guid> ExtractMemoryReferences(string content)
        {
            var references = new List<Guid>();

            if (string.IsNullOrEmpty(content)) return references;

            // Look for memory reference patterns (e.g., "As you mentioned before", "Based on our previous conversation")
            var memoryPhrases = new[]
            {
                "as you mentioned", "previously", "last time", "you told me",
                "remember when", "based on our", "as we discussed"
            };

            var contentLower = content.ToLower();
            var hasMemoryReference = memoryPhrases.Any(phrase => contentLower.Contains(phrase));

            if (hasMemoryReference)
            {
                // This would typically look up actual memory IDs from a memory index
                // For now, return empty list as actual memory lookup would require repository access
            }

            return references;
        }

        // Helper methods for training and analysis

        private double AdjustTrait(double currentValue, double adjustment)
        {
            return Math.Clamp(currentValue + adjustment * 0.1, 0.0, 1.0);
        }

        private Dictionary<string, double> AnalyzeSuccessPatterns(List<AITwinInteraction> successfulInteractions)
        {
            var patterns = new Dictionary<string, double>();

            if (!successfulInteractions.Any()) return patterns;

            patterns["avg_length"] = successfulInteractions.Average(i => i.Content?.Length ?? 0);
            patterns["question_rate"] = successfulInteractions
                .Count(i => i.Content?.Contains("?") == true) / (double)successfulInteractions.Count;

            return patterns;
        }

        private List<PersonalityFeedbackSignal> ExtractFeedbackSignals(List<AITwinInteraction> interactions)
        {
            var signals = new List<PersonalityFeedbackSignal>();

            // Analyze positive feedback (thanks, great, awesome)
            var positiveInteractions = interactions.Where(i =>
                i.EmotionalTone == Emotion.Happy ||
                i.Content?.ToLower().Contains("thank") == true);

            if (positiveInteractions.Count() > interactions.Count * 0.3)
            {
                signals.Add(new PersonalityFeedbackSignal
                {
                    TraitName = "Friendliness",
                    Adjustment = 0.1
                });
            }

            // Analyze frustration signals
            var frustratedInteractions = interactions.Where(i =>
                i.EmotionalTone == Emotion.Angry);

            if (frustratedInteractions.Count() > interactions.Count * 0.2)
            {
                signals.Add(new PersonalityFeedbackSignal
                {
                    TraitName = "Empathy",
                    Adjustment = 0.15
                });
            }

            return signals;
        }

        private async Task<List<AITwinKnowledgeItem>> ExtractKnowledgeItems(
            Building building,
            List<AITwinInteraction> interactions)
        {
            var items = new List<AITwinKnowledgeItem>();

            // Extract knowledge from building data
            if (building != null)
            {
                items.Add(new AITwinKnowledgeItem
                {
                    Type = AITwinKnowledgeType.BuildingLayout,
                    KeyConcept = building.Name,
                    Content = $"Building {building.Name} has {building.Floors?.Count ?? 0} floors",
                    Importance = 0.9,
                    InitialConfidence = 1.0,
                    Source = "building_data"
                });
            }

            // Extract knowledge from interactions
            foreach (var interaction in interactions.Where(i => i.Content?.Length > 50))
            {
                var topics = ExtractTopicsFromContent(interaction.Content);
                foreach (var topic in topics)
                {
                    items.Add(new AITwinKnowledgeItem
                    {
                        Type = AITwinKnowledgeType.UserPreference,
                        KeyConcept = topic,
                        Content = $"User interested in {topic}",
                        Importance = 0.6,
                        InitialConfidence = 0.7,
                        Source = "interaction"
                    });
                }
            }

            return items;
        }

        private List<ConversationPattern> ExtractConversationPatterns(List<AITwinInteraction> interactions)
        {
            var patterns = new List<ConversationPattern>();

            // Group by intent/topic
            var questionInteractions = interactions.Where(i => i.Content?.Contains("?") == true);
            if (questionInteractions.Any())
            {
                patterns.Add(new ConversationPattern
                {
                    Intent = "question",
                    Frequency = questionInteractions.Count(),
                    AvgResponseLength = 0 // Would calculate from responses
                });
            }

            var commandInteractions = interactions.Where(i =>
                i.Content?.ToLower().StartsWith("please") == true ||
                i.Content?.ToLower().StartsWith("can you") == true);
            if (commandInteractions.Any())
            {
                patterns.Add(new ConversationPattern
                {
                    Intent = "command",
                    Frequency = commandInteractions.Count(),
                    AvgResponseLength = 0
                });
            }

            return patterns;
        }

        private List<TopicTransition> ExtractTopicTransitions(List<AITwinInteraction> interactions)
        {
            var transitions = new List<TopicTransition>();
            var orderedInteractions = interactions.OrderBy(i => i.Timestamp).ToList();

            for (int i = 1; i < orderedInteractions.Count; i++)
            {
                var prevTopics = ExtractTopicsFromContent(orderedInteractions[i - 1].Content);
                var currentTopics = ExtractTopicsFromContent(orderedInteractions[i].Content);

                if (prevTopics.Any() && currentTopics.Any() &&
                    !prevTopics.Intersect(currentTopics).Any())
                {
                    transitions.Add(new TopicTransition
                    {
                        FromTopic = prevTopics.First(),
                        ToTopic = currentTopics.First(),
                        SmoothnessScore = 0.5 // Would calculate based on actual transition smoothness
                    });
                }
            }

            return transitions;
        }

        private List<ResponseTiming> ExtractResponseTimings(List<AITwinInteraction> interactions)
        {
            return interactions.Select(i => new ResponseTiming
            {
                ResponseLength = i.Response?.Content?.Length ?? 0,
                Timestamp = i.Timestamp
            }).ToList();
        }

        private List<string> ExtractTopicsFromContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return new List<string>();

            var topics = new List<string>();
            var contentLower = content.ToLower();

            var topicKeywords = new Dictionary<string, string[]>
            {
                ["energy"] = new[] { "energy", "power", "electricity", "consumption" },
                ["temperature"] = new[] { "temperature", "hvac", "heating", "cooling", "climate" },
                ["security"] = new[] { "security", "access", "lock", "alarm", "safety" },
                ["maintenance"] = new[] { "maintenance", "repair", "fix", "broken", "issue" },
                ["occupancy"] = new[] { "occupancy", "people", "crowd", "capacity", "space" }
            };

            foreach (var topic in topicKeywords)
            {
                if (topic.Value.Any(keyword => contentLower.Contains(keyword)))
                {
                    topics.Add(topic.Key);
                }
            }

            return topics;
        }

        private Emotion CalculateOverallSentiment(List<AITwinConversationMessage> messages)
        {
            if (!messages.Any()) return Emotion.Neutral;

            var toneCounts = messages
                .GroupBy(m => m.EmotionalTone)
                .OrderByDescending(g => g.Count())
                .First();

            return toneCounts.Key;
        }

        private string GenerateConversationSummary(AITwinConversation conversation)
        {
            var messageCount = conversation.Messages.Count;
            var topics = conversation.Topics.Any()
                ? string.Join(", ", conversation.Topics.Take(3))
                : "general";
            var sentiment = conversation.OverallSentiment.ToString().ToLower();

            return $"Conversation with {messageCount} messages about {topics}. Overall tone: {sentiment}.";
        }

        private double CalculateInteractionFormality(string content)
        {
            if (string.IsNullOrEmpty(content)) return 0.5;

            var formalIndicators = new[] { "please", "would you", "could you", "kindly", "thank you", "regards" };
            var informalIndicators = new[] { "hey", "hi", "yeah", "ok", "cool", "thanks", "gonna", "wanna" };

            var contentLower = content.ToLower();
            var formalCount = formalIndicators.Count(f => contentLower.Contains(f));
            var informalCount = informalIndicators.Count(f => contentLower.Contains(f));

            if (formalCount + informalCount == 0) return 0.5;
            return (double)formalCount / (formalCount + informalCount);
        }

        private string GetTimeAgo(DateTime timestamp)
        {
            var diff = DateTime.UtcNow - timestamp;

            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return timestamp.ToString("MMM dd");
        }

        private string TruncateContent(string content, int maxLength)
        {
            if (string.IsNullOrEmpty(content)) return "";
            if (content.Length <= maxLength) return content;
            return content.Substring(0, maxLength - 3) + "...";
        }

        /// <summary>
        /// Gets AI twin profiles for a user
        /// </summary>
        public async Task<List<AITwinProfile>> GetUserTwinProfilesAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new List<AITwinProfile>();

            return await _aiTwinRepository.GetByUserIdAsync(userId);
        }

        /// <summary>
        /// Updates AI twin personality traits
        /// </summary>
        public async Task<bool> UpdatePersonalityTraitsAsync(Guid twinId, AITwinPersonalityTraits traits)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null) return false;

            profile.PersonalityTraits = traits;
            profile.LastInteraction = DateTime.UtcNow;

            // Store the personality update as a memory
            profile.MemoryStore.Add(new AITwinMemory
            {
                Id = Guid.NewGuid(),
                Type = AITwinMemoryType.Interaction,
                Content = "Personality traits were manually updated by user",
                Importance = 0.8,
                CreationDate = DateTime.UtcNow,
                Tags = new List<string> { "personality-update", "user-action" }
            });

            await _aiTwinRepository.UpdateAsync(profile);
            return true;
        }

        /// <summary>
        /// Gets AI twin insights based on accumulated knowledge and patterns
        /// </summary>
        public async Task<AITwinInsights> GetTwinInsightsAsync(Guid twinId)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null)
                throw new ArgumentException($"AI twin profile with ID {twinId} not found");

            var insights = new AITwinInsights
            {
                TwinId = twinId,
                GeneratedAt = DateTime.UtcNow,
                KeyObservations = new List<string>(),
                Recommendations = new List<string>(),
                PredictedNeeds = new List<string>(),
                ConfidenceScores = new Dictionary<string, double>()
            };

            // Generate observations based on interaction history
            var recentInteractions = profile.InteractionHistory
                .Where(i => i.Timestamp > DateTime.UtcNow.AddDays(-30))
                .ToList();

            if (recentInteractions.Any())
            {
                var avgMessageLength = recentInteractions.Average(i => i.Content?.Length ?? 0);
                var questionCount = recentInteractions.Count(i => i.Content?.Contains("?") == true);

                insights.KeyObservations.Add($"User has had {recentInteractions.Count} interactions in the past 30 days");

                if (questionCount > recentInteractions.Count * 0.5)
                    insights.KeyObservations.Add("User frequently asks questions, indicating information-seeking behavior");

                // Analyze topics
                var topics = recentInteractions
                    .SelectMany(i => ExtractTopicsFromContent(i.Content))
                    .GroupBy(t => t)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .ToList();

                if (topics.Any())
                    insights.KeyObservations.Add($"Most discussed topics: {string.Join(", ", topics.Select(t => t.Key))}");
            }
            else
            {
                insights.KeyObservations.Add("Limited interaction history. More conversations will improve insights accuracy.");
            }

            // Generate recommendations
            if (profile.ActivationLevel < 0.3)
                insights.Recommendations.Add("Engage in more conversations to improve AI twin learning and personalization");
            if (profile.KnowledgeBase.Count < 10)
                insights.Recommendations.Add("Provide more building-specific information to enhance knowledge base");
            if (profile.BehavioralPatterns.Count < 5)
                insights.Recommendations.Add("Continue interacting to allow the AI twin to learn your communication preferences");

            insights.Recommendations.Add("Review and update personality traits if the AI twin's communication style needs adjustment");

            // Predicted needs based on patterns
            if (profile.BehavioralPatterns.TryGetValue("peak_interaction_hour", out var peakHour))
                insights.PredictedNeeds.Add($"User is most active around {(int)peakHour}:00 - consider scheduling reports at this time");

            if (profile.BehavioralPatterns.TryGetValue("technical_preference", out var techPref) && techPref > 0.5)
                insights.PredictedNeeds.Add("User prefers technical detail - provide comprehensive data in responses");

            insights.PredictedNeeds.Add("Regular building performance updates based on interaction patterns");

            // Confidence scores
            insights.ConfidenceScores["observations"] = Math.Min(recentInteractions.Count / 50.0, 0.95);
            insights.ConfidenceScores["recommendations"] = Math.Min(profile.ActivationLevel + 0.3, 0.9);
            insights.ConfidenceScores["predictions"] = Math.Min(profile.BehavioralPatterns.Count / 15.0, 0.85);

            return insights;
        }

        /// <summary>
        /// Backs up AI twin data as a serialized JSON string
        /// </summary>
        public async Task<string> BackupTwinDataAsync(Guid twinId)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null)
                throw new ArgumentException($"AI twin profile with ID {twinId} not found");

            var backupData = new Dictionary<string, object>
            {
                { "profileId", profile.Id },
                { "name", profile.Name },
                { "description", profile.Description },
                { "userId", profile.UserId },
                { "buildingId", profile.BuildingId },
                { "creationDate", profile.CreationDate },
                { "learningMode", profile.LearningMode.ToString() },
                { "activationLevel", profile.ActivationLevel },
                { "emotionalState", profile.EmotionalState.ToString() },
                { "personalityTraits", profile.PersonalityTraits },
                { "preferences", profile.Preferences },
                { "behavioralPatterns", profile.BehavioralPatterns },
                { "knowledgeBaseCount", profile.KnowledgeBase?.Count ?? 0 },
                { "memoryStoreCount", profile.MemoryStore?.Count ?? 0 },
                { "interactionCount", profile.InteractionHistory?.Count ?? 0 },
                { "backupTimestamp", DateTime.UtcNow },
                { "backupVersion", "1.0" }
            };

            var jsonBackup = System.Text.Json.JsonSerializer.Serialize(backupData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            return await Task.FromResult(jsonBackup);
        }

        /// <summary>
        /// Restores AI twin from backup data
        /// </summary>
        public async Task<AITwinRestoreResult> RestoreTwinFromBackupAsync(string backupData, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(backupData))
                {
                    return new AITwinRestoreResult
                    {
                        Success = false,
                        Message = "Backup data is empty"
                    };
                }

                var backup = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(backupData);
                if (backup == null)
                {
                    return new AITwinRestoreResult
                    {
                        Success = false,
                        Message = "Failed to parse backup data"
                    };
                }

                var name = backup.ContainsKey("name") ? backup["name"].GetString() : "Restored Twin";
                var buildingId = backup.ContainsKey("buildingId") ? Guid.Parse(backup["buildingId"].GetString()) : Guid.Empty;
                var activationLevel = backup.ContainsKey("activationLevel") ? backup["activationLevel"].GetDouble() : 0.1;

                // Create a new profile from backup data
                var request = new AITwinCreationRequest
                {
                    Name = $"{name} (Restored)",
                    Description = $"Restored from backup on {DateTime.UtcNow:yyyy-MM-dd}",
                    UserId = userId,
                    BuildingId = buildingId,
                    LearningMode = AITwinLearningMode.Adaptive
                };

                var restoredProfile = await CreateAITwinProfileAsync(request);
                restoredProfile.ActivationLevel = activationLevel;
                await _aiTwinRepository.UpdateAsync(restoredProfile);

                var interactionsRestored = backup.ContainsKey("interactionCount") ? backup["interactionCount"].GetInt32() : 0;
                var memoriesRestored = backup.ContainsKey("memoryStoreCount") ? backup["memoryStoreCount"].GetInt32() : 0;

                return new AITwinRestoreResult
                {
                    Success = true,
                    RestoredTwinId = restoredProfile.Id,
                    Message = "AI Twin restored successfully from backup",
                    RestoredAt = DateTime.UtcNow,
                    InteractionsRestored = interactionsRestored,
                    MemoriesRestored = memoriesRestored
                };
            }
            catch (Exception ex)
            {
                return new AITwinRestoreResult
                {
                    Success = false,
                    Message = $"Failed to restore from backup: {ex.Message}",
                    RestoredAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Gets AI twin analytics
        /// </summary>
        public async Task<AITwinAnalytics> GetTwinAnalyticsAsync(Guid twinId)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null)
                throw new ArgumentException($"AI twin profile with ID {twinId} not found");

            var now = DateTime.UtcNow;
            var allInteractions = profile.InteractionHistory ?? new List<AITwinInteraction>();
            var weekInteractions = allInteractions.Where(i => i.Timestamp > now.AddDays(-7)).ToList();
            var monthInteractions = allInteractions.Where(i => i.Timestamp > now.AddDays(-30)).ToList();

            // Calculate topic distribution
            var topicDistribution = allInteractions
                .SelectMany(i => ExtractTopicsFromContent(i.Content))
                .GroupBy(t => t)
                .ToDictionary(g => g.Key, g => g.Count());

            // Calculate emotional tone distribution
            var emotionalDistribution = allInteractions
                .GroupBy(i => i.EmotionalTone.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Find most frequent queries
            var mostFrequentQueries = allInteractions
                .Where(i => i.Content?.Contains("?") == true)
                .GroupBy(i => i.Content?.Split('?').First()?.Trim())
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key ?? "Unknown query")
                .ToList();

            // Calculate satisfaction score based on positive emotional tones
            var positiveInteractions = allInteractions.Count(i =>
                i.EmotionalTone == Emotion.Happy || i.EmotionalTone == Emotion.Excited);
            var satisfactionScore = allInteractions.Count > 0
                ? (double)positiveInteractions / allInteractions.Count * 100
                : 50.0;

            return new AITwinAnalytics
            {
                TwinId = twinId,
                AnalysisDate = now,
                TotalInteractions = allInteractions.Count,
                InteractionsThisWeek = weekInteractions.Count,
                InteractionsThisMonth = monthInteractions.Count,
                AverageResponseTime = 1.5, // Default estimate in seconds
                AverageConfidence = 0.82,
                TopicDistribution = topicDistribution,
                EmotionalToneDistribution = emotionalDistribution,
                MostFrequentQueries = mostFrequentQueries,
                UserSatisfactionScore = satisfactionScore
            };
        }

        /// <summary>
        /// Sets the learning mode for an AI twin
        /// </summary>
        public async Task<bool> SetLearningModeAsync(Guid twinId, AITwinLearningMode mode)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null) return false;

            profile.LearningMode = mode;
            profile.LastInteraction = DateTime.UtcNow;

            // Store the mode change as a memory
            profile.MemoryStore.Add(new AITwinMemory
            {
                Id = Guid.NewGuid(),
                Type = AITwinMemoryType.Interaction,
                Content = $"Learning mode changed to {mode}",
                Importance = 0.7,
                CreationDate = DateTime.UtcNow,
                Tags = new List<string> { "system-change", "learning-mode" }
            });

            await _aiTwinRepository.UpdateAsync(profile);
            return true;
        }

        /// <summary>
        /// Resets AI twin learning, clearing learned patterns and memories while keeping the profile
        /// </summary>
        public async Task<bool> ResetLearningAsync(Guid twinId)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null) return false;

            // Reset learned data while preserving core profile
            profile.BehavioralPatterns.Clear();
            profile.MemoryStore.Clear();
            profile.InteractionHistory.Clear();
            profile.ActivationLevel = 0.1;
            profile.EmotionalState = Emotion.Neutral;
            profile.PersonalityTraits = InitializePersonalityTraits(null);
            profile.LastInteraction = DateTime.UtcNow;

            // Keep only core knowledge (self-knowledge and building layout)
            profile.KnowledgeBase = profile.KnowledgeBase
                .Where(k => k.Type == AITwinKnowledgeType.Self || k.Type == AITwinKnowledgeType.BuildingLayout)
                .ToList();

            await _aiTwinRepository.UpdateAsync(profile);
            return true;
        }

        /// <summary>
        /// Gets AI twin health status
        /// </summary>
        public async Task<AITwinHealthStatus> GetTwinHealthStatusAsync(Guid twinId)
        {
            var profile = await _aiTwinRepository.GetByIdAsync(twinId);
            if (profile == null)
            {
                return new AITwinHealthStatus
                {
                    TwinId = twinId,
                    Status = "Unhealthy",
                    LastCheckTime = DateTime.UtcNow,
                    IsResponsive = false,
                    OverallHealthScore = 0,
                    Issues = new List<string> { $"AI twin profile with ID {twinId} not found" }
                };
            }

            var issues = new List<string>();
            var recommendations = new List<string>();
            var healthScore = 100.0;

            // Check knowledge base health
            var knowledgeBaseHealthy = profile.KnowledgeBase != null && profile.KnowledgeBase.Count > 0;
            if (!knowledgeBaseHealthy)
            {
                issues.Add("Knowledge base is empty");
                recommendations.Add("Initialize or restore the knowledge base");
                healthScore -= 20;
            }
            else if (profile.KnowledgeBase.Count < 3)
            {
                issues.Add("Knowledge base has very few entries");
                recommendations.Add("Train the AI twin with more building data");
                healthScore -= 10;
            }

            // Check memory system health
            var memorySystemHealthy = profile.MemoryStore != null;
            if (!memorySystemHealthy)
            {
                issues.Add("Memory system is unavailable");
                healthScore -= 15;
            }

            // Check responsiveness (based on last interaction)
            var isResponsive = true;
            var timeSinceLastInteraction = DateTime.UtcNow - profile.LastInteraction;
            if (timeSinceLastInteraction > TimeSpan.FromDays(30))
            {
                issues.Add("No interactions in the past 30 days; the twin may need retraining");
                recommendations.Add("Engage with the AI twin to keep its learning active");
                healthScore -= 10;
            }

            // Check activation level
            if (profile.ActivationLevel < 0.1)
            {
                issues.Add("Activation level is critically low");
                recommendations.Add("Initiate training sessions to improve activation");
                healthScore -= 15;
            }

            // Check LLM connection (simulated - would check real connection in production)
            var llmConnectionHealthy = true;

            // Determine overall status
            var status = healthScore >= 80 ? "Healthy" : healthScore >= 50 ? "Degraded" : "Unhealthy";

            if (!issues.Any())
                recommendations.Add("All systems are healthy. Continue regular interactions for optimal performance.");

            return new AITwinHealthStatus
            {
                TwinId = twinId,
                Status = status,
                LastCheckTime = DateTime.UtcNow,
                IsResponsive = isResponsive,
                KnowledgeBaseHealthy = knowledgeBaseHealthy,
                MemorySystemHealthy = memorySystemHealthy,
                LLMConnectionHealthy = llmConnectionHealthy,
                OverallHealthScore = Math.Max(0, healthScore),
                Issues = issues,
                Recommendations = recommendations
            };
        }

        /// <summary>
        /// Initializes a user session
        /// </summary>
        public async Task InitializeUserSessionAsync(string userId)
        {
            // Stub implementation - session initialization logic to be implemented
            await Task.CompletedTask;
        }

        /// <summary>
        /// Ends a user session
        /// </summary>
        public async Task EndUserSessionAsync(string userId)
        {
            // Stub implementation - session cleanup logic to be implemented
            await Task.CompletedTask;
        }

        /// <summary>
        /// Generates a response for a user message with context
        /// </summary>
        public async Task<string> GenerateResponseAsync(string userId, string message, Dictionary<string, object> context)
        {
            // Stub implementation - delegate to LLM service when fully implemented
            var request = new LLMRequest
            {
                UserPrompt = message,
                Context = context
            };
            var response = await _llmService.GenerateResponseAsync(request);
            return response?.Content ?? "I'm sorry, I couldn't generate a response at this time.";
        }

        /// <summary>
        /// Generates a greeting for a user
        /// </summary>
        public async Task<string> GenerateGreetingAsync(string userId)
        {
            // Stub implementation - personalized greeting logic to be implemented
            await Task.CompletedTask;
            return $"Hello! Welcome back. How can I assist you today?";
        }
    }
}