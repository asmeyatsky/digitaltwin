using System;

namespace DigitalTwin.Core.Entities
{
    public enum FamilyRole
    {
        Owner,
        Adult,
        Child
    }

    public class Family
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class FamilyMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FamilyId { get; set; }
        public Guid UserId { get; set; }
        public FamilyRole Role { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public class FamilyInvite
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FamilyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public FamilyRole Role { get; set; }
        public string InviteCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsAccepted { get; set; }
    }
}
