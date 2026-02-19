using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class TherapyService : ITherapyService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<TherapyService> _logger;

        public TherapyService(DigitalTwinDbContext context, ILogger<TherapyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(List<TherapistProfile> Therapists, int TotalCount)> GetTherapistsAsync(string? specialization, int page, int pageSize)
        {
            var query = _context.TherapistProfiles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(specialization))
                query = query.Where(t => t.Specializations.Contains(specialization));

            var totalCount = await query.CountAsync();

            var therapists = await query
                .OrderByDescending(t => t.IsVerified)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (therapists, totalCount);
        }

        public async Task<TherapistProfile?> GetTherapistByIdAsync(Guid therapistId)
        {
            return await _context.TherapistProfiles.FindAsync(therapistId);
        }

        public async Task<TherapySession> BookSessionAsync(Guid userId, Guid therapistId, DateTime scheduledAt)
        {
            var therapist = await _context.TherapistProfiles.FindAsync(therapistId);
            if (therapist == null)
                throw new InvalidOperationException("Therapist not found.");

            var session = new TherapySession
            {
                TherapistId = therapistId,
                ClientUserId = userId,
                ScheduledAt = scheduledAt,
                DurationMinutes = 50,
                Status = SessionStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            _context.TherapySessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session booked for user {UserId} with therapist {TherapistId} at {ScheduledAt}",
                userId, therapistId, scheduledAt);
            return session;
        }

        public async Task CancelSessionAsync(Guid userId, Guid sessionId)
        {
            var session = await _context.TherapySessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.ClientUserId == userId);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            if (session.Status != SessionStatus.Scheduled)
                throw new InvalidOperationException("Only scheduled sessions can be cancelled.");

            session.Status = SessionStatus.Cancelled;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session {SessionId} cancelled by user {UserId}", sessionId, userId);
        }

        public async Task<(List<TherapySession> Sessions, int TotalCount)> GetUserSessionsAsync(Guid userId, int page, int pageSize)
        {
            var query = _context.TherapySessions.Where(s => s.ClientUserId == userId);

            var totalCount = await query.CountAsync();

            var sessions = await query
                .OrderByDescending(s => s.ScheduledAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (sessions, totalCount);
        }

        public Task<(ScreeningType Type, string[] Questions)> GetScreeningQuestionsAsync(ScreeningType type)
        {
            var questions = type switch
            {
                ScreeningType.PHQ9 => new[]
                {
                    "Little interest or pleasure in doing things",
                    "Feeling down, depressed, or hopeless",
                    "Trouble falling or staying asleep, or sleeping too much",
                    "Feeling tired or having little energy",
                    "Poor appetite or overeating",
                    "Feeling bad about yourself or that you are a failure",
                    "Trouble concentrating on things, such as reading or watching TV",
                    "Moving or speaking slowly, or being fidgety and restless",
                    "Thoughts that you would be better off dead, or of hurting yourself"
                },
                ScreeningType.GAD7 => new[]
                {
                    "Feeling nervous, anxious, or on edge",
                    "Not being able to stop or control worrying",
                    "Worrying too much about different things",
                    "Trouble relaxing",
                    "Being so restless that it is hard to sit still",
                    "Becoming easily annoyed or irritable",
                    "Feeling afraid, as if something awful might happen"
                },
                ScreeningType.PSS10 => new[]
                {
                    "Been upset because of something that happened unexpectedly",
                    "Felt that you were unable to control the important things in your life",
                    "Felt nervous and stressed",
                    "Felt confident about your ability to handle your personal problems",
                    "Felt that things were going your way",
                    "Found that you could not cope with all the things that you had to do",
                    "Been able to control irritations in your life",
                    "Felt that you were on top of things",
                    "Been angered because of things that were outside of your control",
                    "Felt difficulties were piling up so high that you could not overcome them"
                },
                ScreeningType.WHO5 => new[]
                {
                    "I have felt cheerful and in good spirits",
                    "I have felt calm and relaxed",
                    "I have felt active and vigorous",
                    "I woke up feeling fresh and rested",
                    "My daily life has been filled with things that interest me"
                },
                _ => throw new ArgumentException($"Unknown screening type: {type}")
            };

            return Task.FromResult((type, questions));
        }

        public async Task<ClinicalScreening> SubmitScreeningAsync(Guid userId, ScreeningType type, List<int> responses)
        {
            var (_, questions) = await GetScreeningQuestionsAsync(type);

            if (responses.Count != questions.Length)
                throw new InvalidOperationException(
                    $"Expected {questions.Length} responses for {type}, but received {responses.Count}.");

            var score = responses.Sum();

            var severity = type switch
            {
                ScreeningType.PHQ9 => score switch
                {
                    <= 4 => "Minimal",
                    <= 9 => "Mild",
                    <= 14 => "Moderate",
                    <= 19 => "Moderately Severe",
                    _ => "Severe"
                },
                ScreeningType.GAD7 => score switch
                {
                    <= 4 => "Minimal",
                    <= 9 => "Mild",
                    <= 14 => "Moderate",
                    _ => "Severe"
                },
                ScreeningType.PSS10 => score switch
                {
                    <= 13 => "Low Stress",
                    <= 26 => "Moderate Stress",
                    _ => "High Stress"
                },
                ScreeningType.WHO5 => score switch
                {
                    >= 13 => "Good Wellbeing",
                    >= 9 => "Moderate Wellbeing",
                    _ => "Low Wellbeing"
                },
                _ => "Unknown"
            };

            var screening = new ClinicalScreening
            {
                UserId = userId,
                Type = type,
                Responses = JsonSerializer.Serialize(responses),
                Score = score,
                Severity = severity,
                CompletedAt = DateTime.UtcNow
            };

            _context.ClinicalScreenings.Add(screening);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Screening {Type} submitted by user {UserId}: score={Score}, severity={Severity}",
                type, userId, score, severity);
            return screening;
        }

        public async Task<List<ClinicalScreening>> GetScreeningHistoryAsync(Guid userId)
        {
            return await _context.ClinicalScreenings
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CompletedAt)
                .ToListAsync();
        }

        public async Task<TherapistReferral> GenerateReferralAsync(Guid userId, string reason, ReferralUrgency urgency)
        {
            var referral = new TherapistReferral
            {
                UserId = userId,
                Reason = reason,
                Urgency = urgency,
                IsAcknowledged = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.TherapistReferrals.Add(referral);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Referral generated for user {UserId}: urgency={Urgency}, reason={Reason}",
                userId, urgency, reason);
            return referral;
        }

        public async Task<List<TherapistReferral>> GetUserReferralsAsync(Guid userId)
        {
            return await _context.TherapistReferrals
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
