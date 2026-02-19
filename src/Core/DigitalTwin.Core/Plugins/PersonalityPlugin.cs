using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Enums;

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

            // AD-1 compliant: map emotion string to unified Emotion enum for tone adjustment
            if (!string.IsNullOrEmpty(context.Emotion))
            {
                var emotion = EmotionMapper.FromString(context.Emotion);
                var toneAdjustment = emotion switch
                {
                    Emotion.Sad or Emotion.Anxious => "Use a warm, gentle, and reassuring tone.",
                    Emotion.Angry => "Use a calm, patient, and validating tone.",
                    Emotion.Happy or Emotion.Excited => "Match their energy with enthusiasm while being genuine.",
                    Emotion.Surprised => "Acknowledge the surprise and be attentive to what caused it.",
                    Emotion.Calm => "Maintain a relaxed, grounded conversational tone.",
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
