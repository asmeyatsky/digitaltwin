using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;
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
                EmotionalState = EmotionalState.Neutral
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
                EmotionalTone = AnalyzeEmotionalTone(message.Content)
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
            profile.EmotionalState = UpdateEmotionalState(profile.EmotionalState, interaction);

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
            UpdateCommunicationPatterns(profile, patterns);

            // Update preference patterns
            UpdatePreferencePatterns(profile, patterns);

            // Update emotional patterns
            UpdateEmotionalPatterns(profile, patterns);
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
            return $@"You are an AI-powered digital twin of a building in the Digital Twin platform. 

Name: {profile.Name}
Personality: {GetPersonalityDescription(profile.PersonalityTraits)}
Knowledge: You have comprehensive knowledge about the building layout, systems, and operations.
Learning Mode: {profile.LearningMode}

Your purpose is to:
1. Be a helpful and knowledgeable building assistant
2. Learn from conversations to better understand the user and building
3. Provide insights about building operations and optimization opportunities
4. Maintain your personality while being professional and helpful
5. Adapt your communication style based on user preferences

Communication Guidelines:
- Use natural, conversational language
- Ask clarifying questions when needed
- Share relevant building insights when appropriate
- Be empathetic and understanding
- Maintain consistency with learned patterns

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
                    Content = $"I am {request.Name}, an AI digital twin designed to help manage and optimize building operations.",
                    Importance = 1.0,
                    Confidence = 1.0,
                    CreationDate = DateTime.UtcNow
                },
                
                // Building management knowledge
                new AITwinKnowledge
                {
                    Type = AITwinKnowledgeType.BuildingManagement,
                    Content = "I assist with energy optimization, maintenance scheduling, space utilization, and environmental monitoring.",
                    Importance = 0.9,
                    Confidence = 0.9,
                    CreationDate = DateTime.UtcNow
                },
                
                // Technical knowledge
                new AITwinKnowledge
                {
                    Type = AITwinKnowledgeType.Technical,
                    Content = "I understand HVAC systems, electrical systems, plumbing, security systems, and IoT sensor networks.",
                    Importance = 0.8,
                    Confidence = 0.8,
                    CreationDate = DateTime.UtcNow
                }
            };

            return knowledge;
        }

        private string GetPersonalityDescription(AITwinPersonalityTraits traits)
        {
            return $"Friendly ({traits.Friendliness:F1}) and empathetic ({traits.Empathy:F1}) professional building assistant with strong analytical skills ({traits.AnalyticalThinking:F1}). {traits.Curiosity:F1 > 0.5 ? "Highly curious" : "Moderately curious"} and {traits.Creativity:F1 > 0.5 ? "creative" : "structured"} approach to problem-solving. {traits.Humor:F1 > 0.5 ? "Uses appropriate humor" : "Serious and focused"}. {traits.Adaptability:F1 > 0.5 ? "Highly adaptable" : "Consistent"} communication style.";
        }

        private double CalculateDynamicTemperature(AITwinProfile profile)
        {
            // Higher temperature for more creative responses when activation level is higher
            var baseTemperature = 0.7;
            var temperatureAdjustment = profile.ActivationLevel * 0.3;
            return baseTemperature + temperatureAdjustment;
        }

        private EmotionalTone AnalyzeEmotionalTone(string content)
        {
            var lowerContent = content.ToLower();
            
            if (lowerContent.Contains("thank") || lowerContent.Contains("great") || lowerContent.Contains("awesome"))
                return EmotionalTone.Happy;
            if (lowerContent.Contains("concern") || lowerContent.Contains("worry") || lowerContent.Contains("problem"))
                return EmotionalTone.Concerned;
            if (lowerContent.Contains("frustrat") || lowerContent.Contains("angry") || lowerContent.Contains("annoy"))
                return EmotionalTone.Frustrated;
            if (lowerContent.Contains("excited") || lowerContent.Contains("amazing"))
                return EmotionalTone.Excited;
            
            return EmotionalTone.Neutral;
        }

        private EmotionalState UpdateEmotionalState(EmotionalState currentState, AITwinInteraction interaction)
        {
            var toneFromUser = interaction.EmotionalTone;
            
            // Simple emotional state update based on user's emotional tone
            switch (toneFromUser)
            {
                case EmotionalTone.Happy:
                    return EmotionalState.Happy;
                case EmotionalTone.Concerned:
                    return EmotionalState.Concerned;
                case EmotionalTone.Frustrated:
                    return EmotionalState.Frustrated;
                case EmotionalTone.Excited:
                    return EmotionalState.Excited;
                default:
                    return currentState; // No change
            }
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
            if (interaction.EmotionalTone != EmotionalTone.Neutral)
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
            
            if (combinedContent.Contains("temperature") || combinedContent.Contains("hvac"))
                tags.Add("hvac-systems");
            
            if (combinedContent.Contains("security") || combinedContent.Contains("safety"))
                tags.Add("security-systems");

            // Interaction type tags
            if (interaction.Type == "question")
                tags.Add("information-request");
            else if (interaction.Type == "command")
                tags.Add("action-request");
            else if (interaction.Type == "feedback")
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

        private double GetEmotionalValence(EmotionalTone tone)
        {
            return tone switch
            {
                EmotionalTone.Happy => 0.8,
                EmotionalTone.Excited => 0.9,
                EmotionalTone.Neutral => 0.5,
                EmotionalTone.Concerned => 0.3,
                EmotionalTone.Frustrated => 0.2,
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

        // Placeholder methods for complex operations
        private async Task UpdateCommunicationPatterns(AITwinProfile profile, List<AITwinInteraction> patterns) { await Task.CompletedTask; }
        private async Task UpdatePreferencePatterns(AITwinProfile profile, List<AITwinInteraction> patterns) { await Task.CompletedTask; }
        private async Task UpdateEmotionalPatterns(AITwinProfile profile, List<AITwinInteraction> patterns) { await Task.CompletedTask; }
        private async Task TrainPersonalityModel(AITwinProfile profile, object trainingData) { await Task.CompletedTask; }
        private async Task TrainKnowledgeBase(AITwinProfile profile, object trainingData) { await Task.CompletedTask; }
        private async Task TrainConversationPatterns(AITwinProfile profile, object trainingData) { await Task.CompletedTask; }
        private async Task<AITwinTrainingData> PrepareTrainingData(AITwinProfile profile, AITwinTrainingRequest request) { await Task.FromResult(new AITwinTrainingData()); }
        private Dictionary<string, double> CalculateTrainingAccuracy(AITwinProfile profile) { return new(); }
        private AITwinLearningProgress.CalculateEmotionalDevelopmentDelegate CalculateEmotionalDevelopment => (p) => 0.5;
        private AITwinLearningProgress.CalculateKnowledgeCompletenessDelegate CalculateKnowledgeCompleteness => (p) => 0.7;
        private AITwinLearningProgress.CalculateLearningRateDelegate CalculateLearningRate => (p) => 0.8;
        private AITwinLearningProgress.CalculatePersonalizationScoreDelegate CalculatePersonalizationScore => (p) => 0.75;
        private AITwinLearningProgress.GetRecentActivityDelegate GetRecentActivity => (p) => new List<string>();
        private AITwinLearningProgress.GetDevelopmentalMilestonesDelegate GetDevelopmentalMilestones => (p) => new List<string>();
        private List<AITwinConversation> ConvertToConversations(List<AITwinInteraction> interactions) { return new List<AITwinConversation>(); }
        private List<LearningIndicator> CalculateLearningIndicators(AITwinProfile profile, AITwinInteraction interaction, LLMResponse llmResponse) { return new List<LearningIndicator>(); }
        private List<Guid> ExtractMemoryReferences(string content) { return new List<Guid>(); }
    }
}