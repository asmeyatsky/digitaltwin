using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class InMemoryEventBus : IEventBus
    {
        private readonly ILogger<InMemoryEventBus> _logger;
        private readonly ConcurrentDictionary<string, Delegate> _handlers = new();

        public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
        {
            _logger = logger;
            _logger.LogInformation("Using in-memory event bus (development mode)");
        }

        public Task PublishAsync<T>(string eventName, T data)
        {
            _logger.LogDebug("Publishing event {EventName}", eventName);

            if (_handlers.TryGetValue(eventName, out var handler) && handler is Func<T, Task> typedHandler)
            {
                return typedHandler(data);
            }

            return Task.CompletedTask;
        }

        public void Subscribe<T>(string eventName, Func<T, Task> handler)
        {
            _handlers[eventName] = handler;
            _logger.LogInformation("Subscribed to event {EventName} (in-memory)", eventName);
        }
    }
}
