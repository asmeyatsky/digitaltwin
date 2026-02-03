using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Enums;

namespace DigitalTwin.Core.Services
{
    /// <summary>
    /// Emotional State Service
    /// Manages emotional memory, relationship context, and conversation continuity
    /// </summary>
    public class EmotionalStateService : IEmotionalStateService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<EmotionalStateService> _logger;
        private readonly Dictionary<Guid, List<EmotionalMemory>> _userMemories;
        
        public EmotionalStateService(
            DigitalTwinDbContext context,
            ILogger<EmotionalStateService> logger)
        {
            _context = context;
            _logger = logger;
            _userMemories = new Dictionary<Guid, List<EmotionalMemory>>();
        }
        
        public async Task StoreEmotionalMemoryAsync(EmotionalMemory memory)
        {
            try
            {
                _logger.LogInformation("Storing emotional memory for user {UserId} with emotion {Emotion} and intensity {Intensity}", 
                    memory.UserId, memory.Emotion, memory.Intensity);
                
                // Add to user's memory list
                if (!_userMemories.ContainsKey(memory.UserId))
                {
                    _userMemories[memory.UserId] = new List<EmotionalMemory>();
                }
                
                _userMemories[memory.UserId].Add(memory);
                
                // Store in database
                _context.EmotionalMemories.Add(memory);
                await _context.SaveChangesAsync();
                
                // Limit memory list to 50 most recent
                if (_userMemories[memory.UserId].Count > 50)
                {
                    _userMemories[memory.UserId] = _userMemories[memory.UserId]
                        .OrderByDescending(m => m.CreatedAt)
                        .Take(50)
                        .ToList();
                }
                
                _logger.LogInformation("Successfully stored emotional memory: {MemoryId}", memory.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing emotional memory: {Error}", ex.Message);
                return false;
            }
        }
        
        public async Task<List<EmotionalMemory>> GetUserMemoriesAsync(Guid userId, int limit = 50)
        {
            try
            {
                // Get memories from cache first
                if (_userMemories.ContainsKey(userId))
                {
                    return _userMemories[userId].Take(limit);
                }
                
                // Fallback to database
                var memories = await _context.EmotionalMemories
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
                
                // Cache the results
                _userMemories[userId] = memories;
                
                _logger.LogInformation("Retrieved {Count} memories for user {UserId}", memories.Count, userId);
                return memories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user memories: {Error}", ex.Message);
                return new List<EmotionalMemory>();
            }
        }
        
        public async Task<List<EmotionalMemory>> GetRelevantMemoriesAsync(Guid userId, string context, int limit = 10)
        {
            try
            {
                var allMemories = await GetUserMemoriesAsync(userId);
                
                // Find memories relevant to current context
                var relevantMemories = allMemories
                    .Where(m => m.Description.Contains(context, StringComparison.OrdinalIgnoreCase) ||
                               m.EmotionTags.Any(tag => tag.Contains(context, StringComparison.OrdinalIgnoreCase)) ||
                               m.AssociatedEmotions.Any(e => e.ToString().Contains(context, StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(m => m.ImportanceScore)
                    .Take(limit)
                    .ToList();
                
                _logger.LogInformation("Found {Count} relevant memories for user {UserId} in context {Context}", 
                    relevantMemories.Count, userId, context);
                
                return relevantMemories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving relevant memories: {Error}", ex.Message);
                return new List<EmotionalMemory>();
            }
        }
        
        public async Task<EmotionalTrend> AnalyzeEmotionalTrendsAsync(Guid userId, TimeSpan period)
        {
            try
            {
                var memories = await GetUserMemoriesAsync(userId, 1000);
                var cutoffDate = DateTime.UtcNow - period;
                
                var recentMemories = memories
                    .Where(m => m.CreatedAt >= cutoffDate)
                    .ToList();
                
                if (!recentMemories.Any())
                {
                    return new EmotionalTrend
                    {
                        DominantEmotion = EmotionType.Neutral,
                        EmotionalStability = 1.0,
                        TrendDirection = TrendDirection.Stable,
                        Confidence = 0.5
                    };
                }
                
                // Analyze emotional patterns
                var emotionCounts = new Dictionary<EmotionType, int>();
                foreach (var memory in recentMemories)
                {
                    emotionCounts[memory.PrimaryEmotion] = emotionCounts.GetValueOrDefault(memory.PrimaryEmotion, 0) + 1;
                }
                
                // Determine dominant emotion
                var dominantEmotion = emotionCounts.OrderByDescending(kv => kv.Value).First().Key;
                
                // Calculate emotional stability (consistency)
                var consistencyScore = CalculateEmotionalStability(recentMemories);
                
                // Calculate trend direction
                var trendDirection = CalculateTrendDirection(recentMemories, dominantEmotion);
                
                _logger.LogInformation("Analyzed emotional trends for user {UserId}: Dominant={DominantEmotion}, Stability={Stability:F2}", 
                    userId, dominantEmotion, consistencyScore);
                
                return new EmotionalTrend
                {
                    UserId = userId,
                    DominantEmotion = dominantEmotion,
                    EmotionalStability = consistencyScore,
                    TrendDirection = trendDirection,
                    Period = period,
                    AnalysisDate = DateTime.UtcNow,
                    Confidence = CalculateTrendConfidence(recentMemories, consistencyScore)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing emotional trends: {Error}", ex.Message);
                return new EmotionalTrend();
            }
        }
        
        public async Task<bool> UpdateMemoryImportanceAsync(Guid memoryId, Guid userId, int importance)
        {
            try
            {
                var memory = await _context.EmotionalMemories.FindAsync(memoryId);
                if (memory != null && memory.UserId == userId)
                {
                    memory.ImportanceScore = Math.Min(10, Math.Max(1, importance));
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Updated memory importance: {MemoryId} to {Importance}", memoryId, importance);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating memory importance: {Error}", ex.Message);
                return false;
            }
        }
        
        private double CalculateEmotionalStability(List<EmotionalMemory> memories)
        {
            if (memories.Count < 5) return 0.5;
            
            // Group by emotion and calculate consistency
            var emotionGroups = new Dictionary<EmotionType, List<EmotionalMemory>>();
            foreach (var memory in memories)
            {
                if (!emotionGroups.ContainsKey(memory.PrimaryEmotion))
                {
                    emotionGroups[memory.PrimaryEmotion] = new List<EmotionalMemory>();
                }
                emotionGroups[memory.PrimaryEmotion].Add(memory);
            }
            
            // Calculate overall consistency score
            var consistencyScores = new List<double>();
            foreach (var group in emotionGroups.Values)
            {
                if (group.Count >= 3)
                {
                    // Calculate consistency for this emotion
                    var score = CalculateEmotionConsistency(group);
                    consistencyScores.Add(score);
                }
            }
            
            return consistencyScores.Any() ? consistencyScores.Average() : 0.5;
        }
        
        private double CalculateEmotionConsistency(List<EmotionalMemory> emotionMemories)
        {
            if (emotionMemories.Count < 3) return 0.3;
            
            // Check consistency of emotional expressions
            var intensities = emotionMemories.Select(m => m.Intensity).ToList();
            var avgIntensity = intensities.Average();
            var variance = intensities.Any(i => Math.Abs(i - avgIntensity) > 0.3) ? 0.7 : 1.0;
            
            // Check temporal consistency
            var timeGaps = new List<double>();
            for (int i = 1; i < emotionMemories.Count; i++)
            {
                var gap = (emotionMemories[i].CreatedAt - emotionMemories[i-1].CreatedAt).TotalMinutes;
                timeGaps.Add(gap);
            }
            
            var avgTimeGap = timeGaps.Average();
            var timeConsistency = avgTimeGap < 60 ? 1.0 : 0.5; // Consistent within 1 hour
            
            // Combine factors
            return (variance * 0.3 + timeConsistency * 0.7) / 2.0;
        }
        
        private TrendDirection CalculateTrendDirection(List<EmotionalMemory> memories, EmotionType currentDominant)
        {
            if (memories.Count < 10) return TrendDirection.InsufficientData;
            
            var recentMemories = memories.Take(5).ToList();
            var olderMemories = memories.Skip(5).Take(10).ToList();
            
            var recentDominant = GetMostFrequentEmotion(recentMemories);
            var olderDominant = GetMostFrequentEmotion(olderMemories);
            
            if (recentDominant == olderDominant && recentDominant == currentDominant)
            {
                return TrendDirection.Stable;
            }
            else if (IsImprovementTrend(recentDominant, olderDominant))
            {
                return TrendDirection.Improving;
            }
            else if (IsDeclineTrend(recentDominant, olderDominant))
            {
                return TrendDirection.Declining;
            }
            
            return TrendDirection.Fluctuating;
        }
        
        private EmotionType GetMostFrequentEmotion(List<EmotionalMemory> memories)
        {
            var emotionCounts = memories
                .GroupBy(m => m.PrimaryEmotion)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(kv => kv.Value)
                .First().Key;
        }
        
        private bool IsImprovementTrend(EmotionType recent, EmotionType older)
        {
            var positiveEmotions = new[] { EmotionType.Happy, EmotionType.Excited, EmotionType.Content };
            var negativeEmotions = new[] { EmotionType.Sad, EmotionType.Angry, EmotionType.Fear, EmotionType.Disgust };
            
            var isRecentPositive = positiveEmotions.Contains(recent);
            var isOldPositive = positiveEmotions.Contains(older);
            
            return isRecentPositive && !isOldPositive;
        }
        
        private bool IsDeclineTrend(EmotionType recent, EmotionType older)
        {
            var positiveEmotions = new[] { EmotionType.Happy, EmotionType.Excited, EmotionType.Content };
            var negativeEmotions = new[] { EmotionType.Sad, EmotionType.Angry, EmotionType.Fear, EmotionType.Disgust };
            
            var isRecentNegative = negativeEmotions.Contains(recent);
            var isOldNegative = negativeEmotions.Contains(older);
            
            return !isRecentNegative && isOldNegative;
        }
        
        private double CalculateTrendConfidence(List<EmotionalMemory> memories, double consistencyScore)
        {
            var dataQuality = memories.Count >= 10 ? 1.0 : 0.7;
            var timeSpan = memories.Max(m => m.CreatedAt) - memories.Min(m => m.CreatedAt);
            var recencyFactor = timeSpan.TotalDays > 7 ? 0.8 : 0.5;
            
            return (consistencyScore * 0.6 + dataQuality * 0.4) * recencyFactor;
        }
    }
    
    public interface IEmotionalStateService
    {
        Task<bool> StoreEmotionalMemoryAsync(EmotionalMemory memory);
        Task<List<EmotionalMemory>> GetUserMemoriesAsync(Guid userId, int limit = 50);
        Task<List<EmotionalMemory>> GetRelevantMemoriesAsync(Guid userId, string context, int limit = 10);
        Task<EmotionalTrend> AnalyzeEmotionalTrendsAsync(Guid userId, TimeSpan period);
        Task<bool> UpdateMemoryImportanceAsync(Guid memoryId, Guid userId, int importance);
    }
}