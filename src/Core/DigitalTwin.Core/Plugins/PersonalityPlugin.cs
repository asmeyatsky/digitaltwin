using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DigitalTwin.Core.Plugins
{
    public class PersonalityPlugin : ICompanionPlugin
    {
        private readonly ILogger<PersonalityPlugin> _logger;

        public string Name => "PersonalityPlugin";
        public int Priority => 20;

        public PersonalityPlugin(ILogger<PersonalityPlugin> logger)
        {
            _logger = logger;
        }

        public Task<PluginResult> ProcessAsync(PluginContext context)
        {
            var personalityContext = new Dictionary<string, object>();

            // Check if user has personality settings in their additional context
            if (context.AdditionalContext.TryGetValue("user_personality_traits", out var traits))
            {
                personalityContext["personality_instruction"] =
                    $"Adjust your tone and communication style based on the user's personality preferences: {traits}";
            }

            // Default personality adjustments based on emotion
            if (!string.IsNullOrEmpty(context.Emotion))
            {
                var emotion = context.Emotion.ToLowerInvariant();
                var toneAdjustment = emotion switch
                {
                    "sad" or "fearful" or "fear" or "anxious" => "Use a warm, gentle, and reassuring tone.",
                    "angry" or "frustrated" => "Use a calm, patient, and validating tone.",
                    "happy" or "excited" => "Match their energy with enthusiasm while being genuine.",
                    "curious" => "Be informative and encouraging of their curiosity.",
                    _ => (string?)null
                };

                if (toneAdjustment != null)
                    personalityContext["tone_adjustment"] = toneAdjustment;
            }

            return Task.FromResult(new PluginResult
            {
                AdditionalContext = personalityContext.Count > 0 ? personalityContext : null,
                ShouldContinue = true
            });
        }
    }
}
