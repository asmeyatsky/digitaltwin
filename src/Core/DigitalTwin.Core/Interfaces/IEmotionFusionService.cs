using System;
using System.Threading.Tasks;

namespace DigitalTwin.Core.Interfaces
{
    public interface IEmotionFusionService
    {
        Task<FusedEmotion> FuseEmotionsAsync(EmotionSignal[] signals);
    }

    public enum EmotionSource
    {
        Text,
        Voice,
        Face
    }

    public class EmotionSignal
    {
        public EmotionSource Source { get; set; }
        public string Emotion { get; set; } = "neutral";
        public double Confidence { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class FusedEmotion
    {
        public string PrimaryEmotion { get; set; } = "neutral";
        public double Confidence { get; set; }
        public string? SecondaryEmotion { get; set; }
        public EmotionSignal[] Signals { get; set; } = Array.Empty<EmotionSignal>();
        public double Valence { get; set; } // -1 (negative) to 1 (positive)
        public double Arousal { get; set; } // 0 (calm) to 1 (excited)
    }
}
