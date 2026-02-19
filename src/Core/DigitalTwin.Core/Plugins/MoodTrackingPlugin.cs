using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DigitalTwin.Core.Plugins
{
    public class MoodTrackingPlugin : ICompanionPlugin
    {
        private readonly ILogger<MoodTrackingPlugin> _logger;

        public string Name => "MoodTrackingPlugin";
        public int Priority => 10;

        public MoodTrackingPlugin(ILogger<MoodTrackingPlugin> logger)
        {
            _logger = logger;
        }

        public Task<PluginResult> ProcessAsync(PluginContext context)
        {
            // Analyze emotion trend from recent memories
            var recentEmotions = context.Memories
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .Select(m => m.PrimaryEmotion.ToString().ToLowerInvariant())
                .ToList();

            var moodContext = new Dictionary<string, object>();

            if (recentEmotions.Any())
            {
                var dominantMood = recentEmotions
                    .GroupBy(e => e)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                moodContext["mood_trend"] = dominantMood;
                moodContext["recent_emotions"] = recentEmotions;

                // Detect declining trend
                var negativeEmotions = new[] { "sad", "angry", "fear", "fearful", "anxious", "frustrated", "disgust", "disgusted" };
                var negativeCount = recentEmotions.Count(e => negativeEmotions.Contains(e));

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
