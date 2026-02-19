using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DigitalTwin.Core.Plugins
{
    public interface IPluginManager
    {
        Task<PluginContext> ExecutePipelineAsync(PluginContext context);
    }

    public class PluginManager : IPluginManager
    {
        private readonly IEnumerable<ICompanionPlugin> _plugins;
        private readonly ILogger<PluginManager> _logger;

        public PluginManager(IEnumerable<ICompanionPlugin> plugins, ILogger<PluginManager> logger)
        {
            _plugins = plugins.OrderBy(p => p.Priority);
            _logger = logger;
        }

        public async Task<PluginContext> ExecutePipelineAsync(PluginContext context)
        {
            foreach (var plugin in _plugins)
            {
                _logger.LogDebug("Executing plugin: {PluginName} (priority {Priority})", plugin.Name, plugin.Priority);

                var result = await plugin.ProcessAsync(context);

                if (result.ModifiedMessage != null)
                    context.Message = result.ModifiedMessage;

                if (result.AdditionalContext != null)
                {
                    foreach (var (key, value) in result.AdditionalContext)
                        context.AdditionalContext[key] = value;
                }

                if (!result.ShouldContinue)
                {
                    _logger.LogInformation("Plugin {PluginName} halted pipeline", plugin.Name);
                    break;
                }
            }

            return context;
        }
    }
}
