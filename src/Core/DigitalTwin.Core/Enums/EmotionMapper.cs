using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Enums
{
    /// <summary>
    /// Maps between legacy emotion enums and the canonical Emotion type.
    /// </summary>
    public static class EmotionMapper
    {
        public static Emotion FromEmotionType(EmotionType value) => value switch
        {
            EmotionType.Happy => Emotion.Happy,
            EmotionType.Sad => Emotion.Sad,
            EmotionType.Angry => Emotion.Angry,
            EmotionType.Fearful or EmotionType.Fear => Emotion.Anxious,
            EmotionType.Surprised or EmotionType.Surprise => Emotion.Surprised,
            EmotionType.Disgusted or EmotionType.Disgust => Emotion.Angry,
            EmotionType.Neutral => Emotion.Neutral,
            EmotionType.Excited => Emotion.Excited,
            EmotionType.Anxious => Emotion.Anxious,
            EmotionType.Calm or EmotionType.Content => Emotion.Calm,
            EmotionType.Frustrated => Emotion.Angry,
            EmotionType.Curious => Emotion.Excited,
            _ => Emotion.Neutral
        };

        public static Emotion FromEmotionalTone(EmotionalTone value) => value switch
        {
            EmotionalTone.Neutral => Emotion.Neutral,
            EmotionalTone.Happy => Emotion.Happy,
            EmotionalTone.Excited => Emotion.Excited,
            EmotionalTone.Concerned => Emotion.Anxious,
            EmotionalTone.Frustrated => Emotion.Angry,
            EmotionalTone.Curious => Emotion.Excited,
            EmotionalTone.Sad => Emotion.Sad,
            EmotionalTone.Calm => Emotion.Calm,
            _ => Emotion.Neutral
        };

        public static Emotion FromEmotionalState(EmotionalState value) => value switch
        {
            EmotionalState.Neutral => Emotion.Neutral,
            EmotionalState.Happy => Emotion.Happy,
            EmotionalState.Excited => Emotion.Excited,
            EmotionalState.Concerned => Emotion.Anxious,
            EmotionalState.Frustrated => Emotion.Angry,
            EmotionalState.Curious => Emotion.Excited,
            EmotionalState.Calm => Emotion.Calm,
            EmotionalState.Alert => Emotion.Surprised,
            _ => Emotion.Neutral
        };

        public static Emotion FromString(string emotion) => (emotion ?? "").ToLower() switch
        {
            "happy" => Emotion.Happy,
            "excited" => Emotion.Excited,
            "sad" or "depressed" => Emotion.Sad,
            "angry" or "frustrated" => Emotion.Angry,
            "anxious" or "worried" or "fear" or "scared" => Emotion.Anxious,
            "surprised" or "surprise" => Emotion.Surprised,
            "calm" or "relaxed" or "content" => Emotion.Calm,
            "curious" => Emotion.Excited,
            "disgust" or "disgusted" => Emotion.Angry,
            _ => Emotion.Neutral
        };

        public static string ToExternalString(Emotion value) => value switch
        {
            Emotion.Neutral => "neutral",
            Emotion.Happy => "happy",
            Emotion.Sad => "sad",
            Emotion.Angry => "angry",
            Emotion.Anxious => "anxious",
            Emotion.Surprised => "surprised",
            Emotion.Calm => "calm",
            Emotion.Excited => "excited",
            _ => "neutral"
        };

        /// <summary>
        /// Convert canonical Emotion back to legacy EmotionalTone for entity storage.
        /// </summary>
        public static EmotionalTone ToEmotionalTone(Emotion value) => value switch
        {
            Emotion.Neutral => EmotionalTone.Neutral,
            Emotion.Happy => EmotionalTone.Happy,
            Emotion.Sad => EmotionalTone.Sad,
            Emotion.Angry => EmotionalTone.Frustrated,
            Emotion.Anxious => EmotionalTone.Concerned,
            Emotion.Surprised => EmotionalTone.Excited,
            Emotion.Calm => EmotionalTone.Calm,
            Emotion.Excited => EmotionalTone.Excited,
            _ => EmotionalTone.Neutral
        };
    }
}
