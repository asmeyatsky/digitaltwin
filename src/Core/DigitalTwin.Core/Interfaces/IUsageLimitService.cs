using System;
using System.Threading.Tasks;

namespace DigitalTwin.Core.Interfaces
{
    public interface IUsageLimitService
    {
        Task<(bool allowed, int remaining, int limit)> CheckLimitAsync(string userId, string resource);
        Task IncrementUsageAsync(string userId, string resource);
        Task<UsageSummary> GetUsageAsync(string userId);
    }

    public class UsageSummary
    {
        public string UserId { get; set; } = string.Empty;
        public string Tier { get; set; } = "free";
        public Dictionary<string, UsageDetail> Resources { get; set; } = new();
    }

    public class UsageDetail
    {
        public int Used { get; set; }
        public int Limit { get; set; }
        public int Remaining => Math.Max(0, Limit - Used);
        public DateTime ResetsAt { get; set; }
    }
}
