using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface IFamilyService
    {
        Task<Family> CreateFamilyAsync(Guid userId, string name);
        Task<FamilyInvite> InviteMemberAsync(Guid userId, Guid familyId, string email, FamilyRole role);
        Task<FamilyMember> AcceptInviteAsync(Guid userId, string inviteCode);
        Task<Family?> GetFamilyAsync(Guid userId);
        Task<List<FamilyMember>> GetFamilyMembersAsync(Guid familyId);
        Task RemoveMemberAsync(Guid userId, Guid familyId, Guid memberUserId);
        Task<FamilyInsights> GetSharedInsightsAsync(Guid familyId);
    }

    public class FamilyInsights
    {
        public Guid FamilyId { get; set; }
        public int MemberCount { get; set; }
        public Dictionary<string, int> EmotionDistribution { get; set; } = new();
        public string OverallMood { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
