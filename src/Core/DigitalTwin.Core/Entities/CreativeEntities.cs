using System;
using DigitalTwin.Core.Enums;

namespace DigitalTwin.Core.Entities
{
    public enum CreativeWorkType
    {
        Story,
        Poem,
        Reflection,
        Gratitude,
        Letter,
        FreeWrite
    }

    public class CreativeWork
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public CreativeWorkType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Emotion Mood { get; set; }
        public bool IsShared { get; set; }
        public Guid? SharedToGroupId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CollaborativeStory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class StoryChapter
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StoryId { get; set; }
        public Guid AuthorUserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int ChapterOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
