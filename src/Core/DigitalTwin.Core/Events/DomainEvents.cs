using System;

namespace DigitalTwin.Core.Events
{
    public record ConversationStarted(string UserId, Guid SessionId, DateTime Timestamp);
    public record MessageProcessed(string UserId, string Emotion, double Confidence, DateTime Timestamp);
    public record EmotionDetected(string UserId, string Emotion, string Source, double Confidence, DateTime Timestamp);
    public record MoodDeclined(string UserId, string CurrentEmotion, string PreviousEmotion, DateTime Timestamp);
    public record CheckInTriggered(string UserId, string Type, string? EmotionContext, DateTime Timestamp);
    public record UsageLimitExceeded(string UserId, string Resource, int Limit, DateTime Timestamp);
}
