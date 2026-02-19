using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Enums;

namespace DigitalTwin.Core.Plugins
{
    public class MoodTrackingPlugin : ICompanionPlugin
    {
        private readonly ILogger<MoodTrackingPlugin> _logger;

        public string Name => "MoodTrackingPlugin";
        public int Priority => 10;

        // AD-1 compliant negative emotions from unified taxonomy
        private static readonly Emotion[] NegativeEmotions = new[]
        {
            Emotion.Sad, Emotion.Angry, Emotion.Anxious
        };

        public MoodTrackingPlugin(ILogger<MoodTrackingPlugin> logger)
        {
            _logger = logger;
        }

        public Task<PluginResult> ProcessAsync(PluginContext context)
        {
            // Map recent memory emotions to unified taxonomy via EmotionMapper
            var recentEmotions = context.Memories
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .Select(m => EmotionMapper.FromEmotionType(m.PrimaryEmotion))
                .ToList();

            var moodContext = new Dictionary<string, object>();

            if (recentEmotions.Any())
            {
                var dominantMood = recentEmotions
                    .GroupBy(e => e)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                moodContext["mood_trend"] = EmotionMapper.ToExternalString(dominantMood);
                moodContext["recent_emotions"] = recentEmotions.Select(EmotionMapper.ToExternalString).ToList();

                // Detect declining trend using unified negative set
                var negativeCount = recentEmotions.Count(e => NegativeEmotions.Contains(e));

                if (negativeCount >= 3)
                {
                    moodContext["declining_mood"] = true;
                    moodContext["mood_context"] = "The user has shown a pattern of negative emotions recently. Be extra supportive and gentle.";
                }
            }

            return Task.FromResult(new PluginResult
            {
                AdditionalContext = moodContext.Count > 0 ? moodContext : null,
                ShouldContinue = true
            });
        }
    }
}
