using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalTwin.Core.Interfaces
{
    public interface IPushNotificationService
    {
        Task RegisterDeviceAsync(Guid userId, string token, string platform);
        Task UnregisterDeviceAsync(Guid userId, string token);
        Task SendPushAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null);
        Task SendPushToMultipleAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null);
    }
}
