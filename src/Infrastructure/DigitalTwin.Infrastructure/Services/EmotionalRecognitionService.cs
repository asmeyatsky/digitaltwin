using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Infrastructure.Services
{
    /// <summary>
    /// Emotional recognition service that integrates with DeepFace microservice
    /// for facial expression analysis and text-based emotion detection
    /// </summary>
    public class EmotionalRecognitionService : IEmotionalRecognitionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmotionalRecognitionService> _logger;
        private readonly string _deepFaceBaseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public EmotionalRecognitionService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<EmotionalRecognitionService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _deepFaceBaseUrl = configuration["DeepFace:BaseUrl"] ?? "http://deepface:8001";

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Analyzes emotional tone from text using keyword-based analysis
        /// </summary>
        public async Task<EmotionalAnalysis> AnalyzeEmotionalToneAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return CreateNeutralAnalysis();
            }

            try
            {
                var lowerText = text.ToLower();
                var emotionScores = new Dictionary<EmotionalTone, double>();

                // Keyword-based emotion detection
                var emotionKeywords = new Dictionary<EmotionalTone, string[]>
                {
                    [EmotionalTone.Happy] = new[] { "happy", "great", "wonderful", "excellent", "fantastic", "amazing", "love", "joy", "pleased", "delighted", "glad", "excited", "thrilled" },
                    [EmotionalTone.Excited] = new[] { "excited", "wow", "awesome", "incredible", "can't wait", "amazing", "thrilling", "enthusiastic" },
                    [EmotionalTone.Concerned] = new[] { "worried", "concerned", "anxious", "nervous", "uneasy", "troubled", "afraid", "scared" },
                    [EmotionalTone.Frustrated] = new[] { "frustrated", "angry", "annoyed", "irritated", "upset", "mad", "furious", "hate" },
                    [EmotionalTone.Curious] = new[] { "curious", "wondering", "interested", "intrigued", "question", "what", "how", "why" },
                    [EmotionalTone.Neutral] = new[] { "okay", "fine", "alright", "normal", "usual" }
                };

                // Calculate scores for each emotion
                foreach (var emotion in emotionKeywords)
                {
                    var matchCount = 0;
                    foreach (var keyword in emotion.Value)
                    {
                        if (lowerText.Contains(keyword))
                            matchCount++;
                    }
                    emotionScores[emotion.Key] = matchCount > 0 ? Math.Min(matchCount * 0.2, 1.0) : 0;
                }

                // Find primary emotion
                var primaryEmotion = EmotionalTone.Neutral;
                var maxScore = 0.0;
                foreach (var score in emotionScores)
                {
                    if (score.Value > maxScore)
                    {
                        maxScore = score.Value;
                        primaryEmotion = score.Key;
                    }
                }

                // If no strong emotion detected, default to neutral
                if (maxScore < 0.1)
                {
                    primaryEmotion = EmotionalTone.Neutral;
                    emotionScores[EmotionalTone.Neutral] = 0.8;
                }

                // Calculate sentiment
                var (sentiment, sentimentScore) = CalculateSentiment(emotionScores);

                return new EmotionalAnalysis
                {
                    PrimaryEmotion = primaryEmotion,
                    EmotionScores = emotionScores,
                    Intensity = maxScore,
                    Confidence = Math.Min(maxScore + 0.3, 1.0),
                    Sentiment = sentiment,
                    SentimentScore = sentimentScore
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing emotional tone from text");
                return CreateNeutralAnalysis();
            }
        }

        /// <summary>
        /// Detects facial expression from image using DeepFace microservice
        /// </summary>
        public async Task<FacialExpression> DetectFacialExpressionAsync(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return new FacialExpression
                {
                    FaceDetected = false,
                    Confidence = 0
                };
            }

            try
            {
                using var content = new MultipartFormDataContent();
                var imageContent = new ByteArrayContent(imageData);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "file", "image.jpg");

                var response = await _httpClient.PostAsync(
                    $"{_deepFaceBaseUrl}/analyze/facial-expression",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<DeepFaceFacialExpressionResponse>(json, _jsonOptions);

                    if (result != null)
                    {
                        return new FacialExpression
                        {
                            FaceDetected = result.FaceDetected,
                            Expression = result.DominantEmotion ?? "neutral",
                            Confidence = result.Confidence,
                            ExpressionScores = result.Emotions ?? new Dictionary<string, double>()
                        };
                    }
                }
                else
                {
                    _logger.LogWarning("DeepFace service returned status {StatusCode}", response.StatusCode);
                }

                return new FacialExpression
                {
                    FaceDetected = false,
                    Confidence = 0,
                    Expression = "unknown"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to connect to DeepFace service");
                return new FacialExpression
                {
                    FaceDetected = false,
                    Confidence = 0,
                    Expression = "service_unavailable"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting facial expression");
                return new FacialExpression
                {
                    FaceDetected = false,
                    Confidence = 0,
                    Expression = "error"
                };
            }
        }

        /// <summary>
        /// Analyzes speech emotion from audio using DeepFace microservice
        /// Note: DeepFace primarily handles images, so this extracts frames from video
        /// or uses a placeholder for pure audio analysis
        /// </summary>
        public async Task<SpeechEmotion> AnalyzeSpeechEmotionAsync(byte[] audioData)
        {
            // DeepFace is primarily for facial analysis
            // For speech emotion, we could integrate with a dedicated speech emotion service
            // For now, return a placeholder response
            _logger.LogWarning("Speech emotion analysis not fully implemented - requires dedicated speech emotion service");

            return new SpeechEmotion
            {
                Emotion = EmotionalTone.Neutral,
                Confidence = 0.5,
                ArousalLevel = 0.5,
                ValenceLevel = 0.5,
                EmotionProbabilities = new Dictionary<string, double>
                {
                    ["neutral"] = 0.8,
                    ["happy"] = 0.1,
                    ["sad"] = 0.05,
                    ["angry"] = 0.05
                }
            };
        }

        /// <summary>
        /// Performs comprehensive emotional analysis on an image
        /// </summary>
        public async Task<EmotionalAnalysis> AnalyzeImageEmotionAsync(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return CreateNeutralAnalysis();
            }

            try
            {
                using var content = new MultipartFormDataContent();
                var imageContent = new ByteArrayContent(imageData);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "file", "image.jpg");

                var response = await _httpClient.PostAsync(
                    $"{_deepFaceBaseUrl}/analyze/emotion",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<DeepFaceEmotionalAnalysisResponse>(json, _jsonOptions);

                    if (result != null)
                    {
                        // Map DeepFace emotions to our EmotionalTone enum
                        var emotionScores = MapToEmotionalTones(result.EmotionScores);

                        return new EmotionalAnalysis
                        {
                            PrimaryEmotion = MapStringToEmotionalTone(result.PrimaryEmotion),
                            EmotionScores = emotionScores,
                            Intensity = result.Intensity,
                            Confidence = result.Confidence,
                            Sentiment = result.Sentiment,
                            SentimentScore = result.SentimentScore
                        };
                    }
                }

                return CreateNeutralAnalysis();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing image emotion");
                return CreateNeutralAnalysis();
            }
        }

        /// <summary>
        /// Health check for the DeepFace service
        /// </summary>
        public async Task<bool> IsDeepFaceServiceHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_deepFaceBaseUrl}/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private EmotionalAnalysis CreateNeutralAnalysis()
        {
            return new EmotionalAnalysis
            {
                PrimaryEmotion = EmotionalTone.Neutral,
                EmotionScores = new Dictionary<EmotionalTone, double>
                {
                    [EmotionalTone.Neutral] = 1.0
                },
                Intensity = 0.5,
                Confidence = 0.8,
                Sentiment = "Neutral",
                SentimentScore = 0.5
            };
        }

        private (string sentiment, double score) CalculateSentiment(Dictionary<EmotionalTone, double> emotions)
        {
            var positiveEmotions = new[] { EmotionalTone.Happy, EmotionalTone.Excited };
            var negativeEmotions = new[] { EmotionalTone.Frustrated, EmotionalTone.Concerned };

            var positiveScore = 0.0;
            var negativeScore = 0.0;

            foreach (var emotion in emotions)
            {
                if (Array.Exists(positiveEmotions, e => e == emotion.Key))
                    positiveScore += emotion.Value;
                else if (Array.Exists(negativeEmotions, e => e == emotion.Key))
                    negativeScore += emotion.Value;
            }

            var total = positiveScore + negativeScore + emotions.GetValueOrDefault(EmotionalTone.Neutral, 0);
            if (total == 0)
                return ("Neutral", 0.5);

            var sentimentScore = (positiveScore - negativeScore + total) / (2 * total);

            if (positiveScore > negativeScore && positiveScore > 0.3)
                return ("Positive", sentimentScore);
            else if (negativeScore > positiveScore && negativeScore > 0.3)
                return ("Negative", sentimentScore);
            else
                return ("Neutral", sentimentScore);
        }

        private Dictionary<EmotionalTone, double> MapToEmotionalTones(Dictionary<string, double> scores)
        {
            var result = new Dictionary<EmotionalTone, double>();

            var mapping = new Dictionary<string, EmotionalTone>
            {
                ["happy"] = EmotionalTone.Happy,
                ["surprise"] = EmotionalTone.Excited,
                ["angry"] = EmotionalTone.Frustrated,
                ["disgust"] = EmotionalTone.Frustrated,
                ["fear"] = EmotionalTone.Concerned,
                ["sad"] = EmotionalTone.Concerned,
                ["neutral"] = EmotionalTone.Neutral
            };

            foreach (var score in scores)
            {
                if (mapping.TryGetValue(score.Key.ToLower(), out var tone))
                {
                    if (result.ContainsKey(tone))
                        result[tone] = Math.Max(result[tone], score.Value);
                    else
                        result[tone] = score.Value;
                }
            }

            return result;
        }

        private EmotionalTone MapStringToEmotionalTone(string emotion)
        {
            return emotion?.ToLower() switch
            {
                "happy" => EmotionalTone.Happy,
                "surprise" => EmotionalTone.Excited,
                "angry" => EmotionalTone.Frustrated,
                "disgust" => EmotionalTone.Frustrated,
                "fear" => EmotionalTone.Concerned,
                "sad" => EmotionalTone.Concerned,
                "neutral" => EmotionalTone.Neutral,
                _ => EmotionalTone.Neutral
            };
        }
    }

    // Response models for DeepFace API
    internal class DeepFaceFacialExpressionResponse
    {
        [JsonPropertyName("face_detected")]
        public bool FaceDetected { get; set; }

        [JsonPropertyName("dominant_emotion")]
        public string DominantEmotion { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("emotions")]
        public Dictionary<string, double> Emotions { get; set; }

        [JsonPropertyName("age")]
        public int? Age { get; set; }

        [JsonPropertyName("gender")]
        public string Gender { get; set; }
    }

    internal class DeepFaceEmotionalAnalysisResponse
    {
        [JsonPropertyName("primary_emotion")]
        public string PrimaryEmotion { get; set; }

        [JsonPropertyName("emotion_scores")]
        public Dictionary<string, double> EmotionScores { get; set; }

        [JsonPropertyName("intensity")]
        public double Intensity { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("sentiment")]
        public string Sentiment { get; set; }

        [JsonPropertyName("sentiment_score")]
        public double SentimentScore { get; set; }

        [JsonPropertyName("arousal_level")]
        public double ArousalLevel { get; set; }

        [JsonPropertyName("valence_level")]
        public double ValenceLevel { get; set; }
    }
}
