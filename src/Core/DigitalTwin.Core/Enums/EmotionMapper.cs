namespace DigitalTwin.Core.Enums
{
    /// <summary>
    /// Maps string emotion labels to/from the canonical Emotion enum.
    /// Legacy conversion methods (FromEmotionType, FromEmotionalTone, etc.)
    /// have been removed now that all code uses Emotion directly.
    /// </summary>
    public static class EmotionMapper
    {
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
    }
}
