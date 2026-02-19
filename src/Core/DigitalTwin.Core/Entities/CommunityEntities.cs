using System;

namespace DigitalTwin.Core.Entities
{
    public enum GroupCategory
    {
        Support,
        Interest,
        Wellness,
        Mindfulness,
        Relationships
    }

    public enum CommunityRole
    {
        Member,
        Moderator
    }

    public class CommunityGroup
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GroupCategory Category { get; set; }
        public bool IsModerated { get; set; }
        public Guid CreatedByUserId { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CommunityPost
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupId { get; set; }
        public Guid AuthorUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
        public int LikeCount { get; set; }
        public int ReplyCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CommunityReply
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PostId { get; set; }
        public Guid AuthorUserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CommunityMembership
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public CommunityRole Role { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
