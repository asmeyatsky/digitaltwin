using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Enums;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.Telemetry;

namespace DigitalTwin.Core.Services
{
    public class EmotionFusionService : IEmotionFusionService
    {
        private readonly ILogger<EmotionFusionService> _logger;

        // Source weights: Face > Voice > Text
        private static readonly Dictionary<EmotionSource, double> SourceWeights = new()
        {
            [EmotionSource.Face] = 0.4,
            [EmotionSource.Voice] = 0.3,
            [EmotionSource.Text] = 0.3,
        };

        // Unified Emotion → (valence, arousal) mapping — AD-1 compliant
        private static readonly Dictionary<Emotion, (double valence, double arousal)> EmotionDimensions = new()
        {
            [Emotion.Happy] = (0.8, 0.6),
            [Emotion.Excited] = (0.7, 0.9),
            [Emotion.Calm] = (0.4, 0.1),
            [Emotion.Neutral] = (0.0, 0.3),
            [Emotion.Sad] = (-0.7, 0.2),
            [Emotion.Angry] = (-0.6, 0.8),
            [Emotion.Anxious] = (-0.4, 0.7),
            [Emotion.Surprised] = (0.1, 0.8),
        };

        private const double RecencyDecayHalfLifeSeconds = 30.0;
        private const double ConfidenceThreshold = 0.3;

        public EmotionFusionService(ILogger<EmotionFusionService> logger)
        {
            _logger = logger;
        }

        public Task<FusedEmotion> FuseEmotionsAsync(EmotionSignal[] signals)
        {
            if (signals == null || signals.Length == 0)
            {
                return Task.FromResult(new FusedEmotion
                {
                    PrimaryEmotion = Emotion.Neutral,
                    Confidence = 0.5,
                    Signals = Array.Empty<EmotionSignal>()
                });
            }

            MetricsRegistry.FusionOperationsTotal.Inc();

            using var activity = DiagnosticConfig.Source.StartActivity("FuseEmotions");
            activity?.SetTag("signalCount", signals.Length);

            var now = DateTime.UtcNow;

            // Calculate weighted scores for each emotion
            var emotionScores = new Dictionary<Emotion, double>();
            var totalWeight = 0.0;

            foreach (var signal in signals)
            {
                var sourceWeight = SourceWeights.GetValueOrDefault(signal.Source, 0.2);
                var recencyWeight = ComputeRecencyWeight(signal.Timestamp, now);
                var effectiveWeight = sourceWeight * recencyWeight * signal.Confidence;

                emotionScores[signal.Emotion] = emotionScores.GetValueOrDefault(signal.Emotion, 0.0) + effectiveWeight;
                totalWeight += effectiveWeight;
            }

            if (totalWeight == 0)
            {
                return Task.FromResult(new FusedEmotion
                {
                    PrimaryEmotion = Emotion.Neutral,
                    Confidence = 0.5,
                    Signals = signals
                });
            }

            // Normalize scores
            foreach (var key in emotionScores.Keys.ToList())
            {
                emotionScores[key] /= totalWeight;
            }

            var sorted = emotionScores.OrderByDescending(kv => kv.Value).ToList();
            var primaryEmotion = sorted[0].Key;
            var primaryConfidence = sorted[0].Value;

            // If max confidence is below threshold, return Neutral
            if (primaryConfidence < ConfidenceThreshold)
            {
                primaryEmotion = Emotion.Neutral;
                primaryConfidence = 1.0 - sorted.Sum(kv => kv.Value);
            }

            Emotion? secondaryEmotion = sorted.Count > 1 ? sorted[1].Key : null;

            // Compute valence and arousal from weighted emotion dimensions
            var valence = 0.0;
            var arousal = 0.0;
            foreach (var (emotion, score) in emotionScores)
            {
                if (EmotionDimensions.TryGetValue(emotion, out var dims))
                {
                    valence += dims.valence * score;
                    arousal += dims.arousal * score;
                }
            }

            var result = new FusedEmotion
            {
                PrimaryEmotion = primaryEmotion,
                Confidence = Math.Round(primaryConfidence, 3),
                SecondaryEmotion = secondaryEmotion,
                Signals = signals,
                Valence = Math.Round(Math.Clamp(valence, -1.0, 1.0), 3),
                Arousal = Math.Round(Math.Clamp(arousal, 0.0, 1.0), 3)
            };

            _logger.LogInformation("Fused {Count} signals → {Primary} ({Confidence:F2}), valence={Valence}, arousal={Arousal}",
                signals.Length, result.PrimaryEmotion, result.Confidence, result.Valence, result.Arousal);

            activity?.SetTag("primaryEmotion", result.PrimaryEmotion.ToString());

            return Task.FromResult(result);
        }

        private static double ComputeRecencyWeight(DateTime signalTime, DateTime now)
        {
            var ageSeconds = (now - signalTime).TotalSeconds;
            if (ageSeconds <= 0) return 1.0;
            return Math.Pow(0.5, ageSeconds / RecencyDecayHalfLifeSeconds);
        }
    }
}
