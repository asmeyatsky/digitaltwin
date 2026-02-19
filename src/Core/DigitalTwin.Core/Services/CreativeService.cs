using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Enums;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class CreativeService : ICreativeService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<CreativeService> _logger;
        private static readonly Random _random = new();

        private static readonly Dictionary<CreativeWorkType, string[]> _prompts = new()
        {
            [CreativeWorkType.Story] = new[]
            {
                "Write about a time when a stranger's kindness changed your day",
                "Imagine you found a door in your home you had never noticed before",
                "Tell the story of a meal that changed the way you see the world",
                "Write about a moment where silence spoke louder than words",
                "Describe a journey where getting lost turned out to be the best part",
                "Write about an unexpected friendship that formed in an unlikely place",
                "Tell the story of an ordinary object that holds extraordinary meaning to you"
            },
            [CreativeWorkType.Poem] = new[]
            {
                "Describe the color of your current emotion without naming it",
                "Write a poem about the space between breaths",
                "Capture the feeling of a rainy morning in verse",
                "Write about something you carry with you that no one can see",
                "Describe the sound of comfort in a poem",
                "Write a poem addressed to your future self one year from now",
                "Capture the exact moment when night becomes morning"
            },
            [CreativeWorkType.Reflection] = new[]
            {
                "What would you tell your younger self about today?",
                "Reflect on a belief you held strongly that has changed over time",
                "What does courage look like in your everyday life?",
                "Think about a challenge that taught you something unexpected about yourself",
                "Reflect on a moment of stillness that gave you clarity",
                "What does home mean to you beyond a physical place?",
                "How has your definition of success evolved over the years?"
            },
            [CreativeWorkType.Gratitude] = new[]
            {
                "Name three small things that brought you comfort this week",
                "Write about someone who believed in you before you believed in yourself",
                "Describe a simple pleasure you often take for granted",
                "What is something your body allows you to do that you are grateful for?",
                "Write about a lesson learned from a difficult experience you are now thankful for",
                "Describe a sound, smell, or taste that instantly makes you feel at peace",
                "Who made you smile recently, and what did they do?"
            },
            [CreativeWorkType.Letter] = new[]
            {
                "Write a letter to someone you haven't spoken to in a while",
                "Write a letter to your past self during a difficult time",
                "Write a letter of forgiveness — to someone else or to yourself",
                "Write a thank-you letter to someone who will never read it",
                "Write a letter to a place that shaped who you are",
                "Write a letter to an emotion you struggle with, as if it were a person",
                "Write a letter to someone you admire explaining what they mean to you"
            },
            [CreativeWorkType.FreeWrite] = new[]
            {
                "Start with 'I remember...' and write for 5 minutes without stopping",
                "Begin with 'Right now I feel...' and let the words flow",
                "Write about whatever comes to mind when you close your eyes for ten seconds",
                "Start with 'If I could change one thing...' and keep writing",
                "Write continuously starting with 'The thing I never say is...'",
                "Begin with 'Today the world looks like...' and see where it takes you",
                "Start with 'What I need most right now is...' and write without editing"
            }
        };

        public CreativeService(DigitalTwinDbContext context, ILogger<CreativeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CreativeWork> CreateWorkAsync(Guid userId, CreativeWorkType type, string title, string content, Emotion mood)
        {
            var work = new CreativeWork
            {
                UserId = userId,
                Type = type,
                Title = title,
                Content = content,
                Mood = mood,
                IsShared = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CreativeWorks.Add(work);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Creative work '{Title}' ({Type}) created by user {UserId}", title, type, userId);
            return work;
        }

        public async Task<(List<CreativeWork> Works, int TotalCount)> GetWorksAsync(Guid userId, CreativeWorkType? type, int page, int pageSize)
        {
            var query = _context.CreativeWorks.Where(w => w.UserId == userId);

            if (type.HasValue)
                query = query.Where(w => w.Type == type.Value);

            var totalCount = await query.CountAsync();

            var works = await query
                .OrderByDescending(w => w.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (works, totalCount);
        }

        public async Task<CreativeWork?> GetWorkByIdAsync(Guid userId, Guid workId)
        {
            return await _context.CreativeWorks
                .FirstOrDefaultAsync(w => w.Id == workId && w.UserId == userId);
        }

        public async Task<CreativeWork> UpdateWorkAsync(Guid userId, Guid workId, string title, string content, Emotion mood)
        {
            var work = await _context.CreativeWorks
                .FirstOrDefaultAsync(w => w.Id == workId && w.UserId == userId);

            if (work == null)
                throw new InvalidOperationException("Creative work not found.");

            work.Title = title;
            work.Content = content;
            work.Mood = mood;
            work.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Creative work {WorkId} updated by user {UserId}", workId, userId);
            return work;
        }

        public async Task DeleteWorkAsync(Guid userId, Guid workId)
        {
            var work = await _context.CreativeWorks
                .FirstOrDefaultAsync(w => w.Id == workId && w.UserId == userId);

            if (work == null)
                throw new InvalidOperationException("Creative work not found.");

            _context.CreativeWorks.Remove(work);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Creative work {WorkId} deleted by user {UserId}", workId, userId);
        }

        public async Task<CreativeWork> ShareWorkAsync(Guid userId, Guid workId, Guid? groupId)
        {
            var work = await _context.CreativeWorks
                .FirstOrDefaultAsync(w => w.Id == workId && w.UserId == userId);

            if (work == null)
                throw new InvalidOperationException("Creative work not found.");

            work.IsShared = true;
            work.SharedToGroupId = groupId;
            work.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Creative work {WorkId} shared by user {UserId} to group {GroupId}", workId, userId, groupId?.ToString() ?? "public");
            return work;
        }

        public async Task<(List<CreativeWork> Works, int TotalCount)> GetSharedWorksAsync(Guid? groupId, int page, int pageSize)
        {
            var query = _context.CreativeWorks.Where(w => w.IsShared);

            if (groupId.HasValue)
                query = query.Where(w => w.SharedToGroupId == groupId.Value);

            var totalCount = await query.CountAsync();

            var works = await query
                .OrderByDescending(w => w.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (works, totalCount);
        }

        public Task<string> GeneratePromptAsync(Guid userId, CreativeWorkType type)
        {
            if (_prompts.TryGetValue(type, out var prompts) && prompts.Length > 0)
            {
                var prompt = prompts[_random.Next(prompts.Length)];
                _logger.LogInformation("Generated creative prompt for user {UserId}, type {Type}", userId, type);
                return Task.FromResult(prompt);
            }

            return Task.FromResult("Write about whatever is on your mind right now.");
        }

        public async Task<CollaborativeStory> StartCollaborativeStoryAsync(Guid userId, Guid roomId, string title)
        {
            var story = new CollaborativeStory
            {
                RoomId = roomId,
                Title = title,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.CollaborativeStories.Add(story);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Collaborative story '{Title}' started by user {UserId} in room {RoomId}", title, userId, roomId);
            return story;
        }

        public async Task<StoryChapter> AddChapterAsync(Guid userId, Guid storyId, string content)
        {
            var story = await _context.CollaborativeStories.FindAsync(storyId);
            if (story == null)
                throw new InvalidOperationException("Collaborative story not found.");

            var maxOrder = await _context.StoryChapters
                .Where(c => c.StoryId == storyId)
                .MaxAsync(c => (int?)c.ChapterOrder) ?? 0;

            var chapter = new StoryChapter
            {
                StoryId = storyId,
                AuthorUserId = userId,
                Content = content,
                ChapterOrder = maxOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.StoryChapters.Add(chapter);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Chapter {Order} added to story {StoryId} by user {UserId}", chapter.ChapterOrder, storyId, userId);
            return chapter;
        }

        public async Task<(CollaborativeStory Story, List<StoryChapter> Chapters)> GetCollaborativeStoryAsync(Guid storyId)
        {
            var story = await _context.CollaborativeStories.FindAsync(storyId);
            if (story == null)
                throw new InvalidOperationException("Collaborative story not found.");

            var chapters = await _context.StoryChapters
                .Where(c => c.StoryId == storyId)
                .OrderBy(c => c.ChapterOrder)
                .ToListAsync();

            return (story, chapters);
        }
    }
}
