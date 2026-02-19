namespace DigitalTwin.Core.Enums
{
    /// <summary>
    /// Canonical emotion taxonomy used across all services.
    /// String conversion via EmotionMapper.FromString / ToExternalString.
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
