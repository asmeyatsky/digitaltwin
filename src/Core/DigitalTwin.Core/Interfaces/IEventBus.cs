using System;
using System.Threading.Tasks;

namespace DigitalTwin.Core.Interfaces
{
    public interface IEventBus
    {
        Task PublishAsync<T>(string eventName, T data);
        void Subscribe<T>(string eventName, Func<T, Task> handler);
    }
}
