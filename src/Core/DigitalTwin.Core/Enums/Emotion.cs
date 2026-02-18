namespace DigitalTwin.Core.Enums
{
    /// <summary>
    /// Canonical emotion taxonomy used across all services.
    /// Maps to/from legacy enums (EmotionType, EmotionalState, EmotionalTone)
    /// via EmotionMapper.
    /// </summary>
    public enum Emotion
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Anxious,
        Surprised,
        Calm,
        Excited
    }
}
