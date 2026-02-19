using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Plugins
{
    public interface ICompanionPlugin
    {
        string Name { get; }
        int Priority { get; } // lower = earlier
        Task<PluginResult> ProcessAsync(PluginContext context);
    }

    public class PluginContext
    {
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Emotion { get; set; }
        public List<EmotionalMemory> Memories { get; set; } = new();
        public string? SessionId { get; set; }
        public Dictionary<string, object> AdditionalContext { get; set; } = new();
    }

    public class PluginResult
    {
        public string? ModifiedMessage { get; set; }
        public Dictionary<string, object>? AdditionalContext { get; set; }
        public bool ShouldContinue { get; set; } = true;
    }
}
