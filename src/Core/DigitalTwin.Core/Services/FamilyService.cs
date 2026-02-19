using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class FamilyService : IFamilyService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<FamilyService> _logger;

        public FamilyService(DigitalTwinDbContext context, ILogger<FamilyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Family> CreateFamilyAsync(Guid userId, string name)
        {
            // Check if user already belongs to a family
            var existingMembership = await _context.FamilyMembers
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (existingMembership != null)
                throw new InvalidOperationException("User already belongs to a family.");

            var family = new Family
            {
                Name = name,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Families.Add(family);

            var member = new FamilyMember
            {
                FamilyId = family.Id,
                UserId = userId,
                Role = FamilyRole.Owner,
                JoinedAt = DateTime.UtcNow
            };

            _context.FamilyMembers.Add(member);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Family '{FamilyName}' created by user {UserId}", name, userId);
            return family;
        }

        public async Task<FamilyInvite> InviteMemberAsync(Guid userId, Guid familyId, string email, FamilyRole role)
        {
            // Verify the requesting user is the owner of the family
            var membership = await _context.FamilyMembers
                .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId);

            if (membership == null || membership.Role != FamilyRole.Owner)
                throw new UnauthorizedAccessException("Only the family owner can invite members.");

            var inviteCode = GenerateInviteCode();

            var invite = new FamilyInvite
            {
                FamilyId = familyId,
                Email = email,
                Role = role,
                InviteCode = inviteCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsAccepted = false
            };

            _context.FamilyInvites.Add(invite);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invite created for {Email} to family {FamilyId} with code {InviteCode}",
                email, familyId, inviteCode);

            return invite;
        }

        public async Task<FamilyMember> AcceptInviteAsync(Guid userId, string inviteCode)
        {
            var invite = await _context.FamilyInvites
                .FirstOrDefaultAsync(i => i.InviteCode == inviteCode && !i.IsAccepted);

            if (invite == null)
                throw new InvalidOperationException("Invalid or already used invite code.");

            if (invite.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Invite code has expired.");

            // Check if user already belongs to a family
            var existingMembership = await _context.FamilyMembers
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (existingMembership != null)
                throw new InvalidOperationException("User already belongs to a family.");

            var member = new FamilyMember
            {
                FamilyId = invite.FamilyId,
                UserId = userId,
                Role = invite.Role,
                JoinedAt = DateTime.UtcNow
            };

            invite.IsAccepted = true;

            _context.FamilyMembers.Add(member);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} joined family {FamilyId} via invite {InviteCode}",
                userId, invite.FamilyId, inviteCode);

            return member;
        }

        public async Task<Family?> GetFamilyAsync(Guid userId)
        {
            var membership = await _context.FamilyMembers
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership == null)
                return null;

            return await _context.Families.FindAsync(membership.FamilyId);
        }

        public async Task<List<FamilyMember>> GetFamilyMembersAsync(Guid familyId)
        {
            return await _context.FamilyMembers
                .Where(m => m.FamilyId == familyId)
                .OrderBy(m => m.JoinedAt)
                .ToListAsync();
        }

        public async Task RemoveMemberAsync(Guid userId, Guid familyId, Guid memberUserId)
        {
            // Verify the requesting user is the owner
            var ownerMembership = await _context.FamilyMembers
                .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId);

            if (ownerMembership == null || ownerMembership.Role != FamilyRole.Owner)
                throw new UnauthorizedAccessException("Only the family owner can remove members.");

            if (userId == memberUserId)
                throw new InvalidOperationException("Owner cannot remove themselves from the family.");

            var memberToRemove = await _context.FamilyMembers
                .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == memberUserId);

            if (memberToRemove == null)
                throw new InvalidOperationException("Member not found in this family.");

            _context.FamilyMembers.Remove(memberToRemove);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {MemberUserId} removed from family {FamilyId} by {UserId}",
                memberUserId, familyId, userId);
        }

        public async Task<FamilyInsights> GetSharedInsightsAsync(Guid familyId)
        {
            var members = await _context.FamilyMembers
                .Where(m => m.FamilyId == familyId)
                .ToListAsync();

            var memberUserIds = members.Select(m => m.UserId.ToString()).ToList();

            var periodStart = DateTime.UtcNow.AddDays(-30);
            var periodEnd = DateTime.UtcNow;

            // Query EmotionalMemory for all family members in the last 30 days
            var emotionalMemories = await _context.EmotionalMemories
                .Where(em => memberUserIds.Contains(em.UserId) && em.Timestamp >= periodStart)
                .ToListAsync();

            // Aggregate emotion counts (don't expose individual data)
            var emotionDistribution = emotionalMemories
                .GroupBy(em => em.PrimaryEmotion.ToString().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Count());

            // Determine overall mood from the most frequent emotion
            var overallMood = emotionDistribution.Any()
                ? emotionDistribution.OrderByDescending(kv => kv.Value).First().Key
                : "neutral";

            return new FamilyInsights
            {
                FamilyId = familyId,
                MemberCount = members.Count,
                EmotionDistribution = emotionDistribution,
                OverallMood = overallMood,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd
            };
        }

        private static string GenerateInviteCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var bytes = RandomNumberGenerator.GetBytes(8);
            var code = new char[8];
            for (int i = 0; i < 8; i++)
            {
                code[i] = chars[bytes[i] % chars.Length];
            }
            return new string(code);
        }
    }
}
