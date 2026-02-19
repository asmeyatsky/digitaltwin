using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface ITherapyService
    {
        Task<(List<TherapistProfile> Therapists, int TotalCount)> GetTherapistsAsync(string? specialization, int page, int pageSize);
        Task<TherapistProfile?> GetTherapistByIdAsync(Guid therapistId);
        Task<TherapySession> BookSessionAsync(Guid userId, Guid therapistId, DateTime scheduledAt);
        Task CancelSessionAsync(Guid userId, Guid sessionId);
        Task<(List<TherapySession> Sessions, int TotalCount)> GetUserSessionsAsync(Guid userId, int page, int pageSize);
        Task<(ScreeningType Type, string[] Questions)> GetScreeningQuestionsAsync(ScreeningType type);
        Task<ClinicalScreening> SubmitScreeningAsync(Guid userId, ScreeningType type, List<int> responses);
        Task<List<ClinicalScreening>> GetScreeningHistoryAsync(Guid userId);
        Task<TherapistReferral> GenerateReferralAsync(Guid userId, string reason, ReferralUrgency urgency);
        Task<List<TherapistReferral>> GetUserReferralsAsync(Guid userId);
    }
}
