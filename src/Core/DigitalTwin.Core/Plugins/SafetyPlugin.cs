using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DigitalTwin.Core.Plugins
{
    public class SafetyPlugin : ICompanionPlugin
    {
        private readonly ILogger<SafetyPlugin> _logger;

        public string Name => "SafetyPlugin";
        public int Priority => 0; // Always runs first

        private static readonly string[] CrisisKeywords = new[]
        {
            "suicide", "kill myself", "end my life", "want to die",
            "self-harm", "hurt myself", "cutting myself", "no reason to live",
            "overdose", "jump off", "hang myself"
        };

        private const string SafetyResources =
            "\n\n---\nIf you're in crisis, please reach out for help:\n" +
            "- National Suicide Prevention Lifeline: 988 (call or text)\n" +
            "- Crisis Text Line: Text HOME to 741741\n" +
            "- International Association for Suicide Prevention: https://www.iasp.info/resources/Crisis_Centres/\n" +
            "You are not alone, and help is available 24/7.";

        public SafetyPlugin(ILogger<SafetyPlugin> logger)
        {
            _logger = logger;
        }

        public Task<PluginResult> ProcessAsync(PluginContext context)
        {
            var messageLower = context.Message.ToLowerInvariant();
            var isCrisis = CrisisKeywords.Any(keyword => messageLower.Contains(keyword));

            if (isCrisis)
            {
                _logger.LogWarning("Crisis keywords detected for user {UserId}", context.UserId);
                return Task.FromResult(new PluginResult
                {
                    AdditionalContext = new Dictionary<string, object>
                    {
                        ["safety_alert"] = true,
                        ["safety_resources"] = SafetyResources,
                        ["crisis_detected"] = true,
                    },
                    ShouldContinue = true // Still process but add safety context
                });
            }

            return Task.FromResult(new PluginResult { ShouldContinue = true });
        }
    }
}
