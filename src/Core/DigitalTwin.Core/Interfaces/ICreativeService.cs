using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Enums;

namespace DigitalTwin.Core.Interfaces
{
    public interface ICreativeService
    {
        Task<CreativeWork> CreateWorkAsync(Guid userId, CreativeWorkType type, string title, string content, Emotion mood);
        Task<(List<CreativeWork> Works, int TotalCount)> GetWorksAsync(Guid userId, CreativeWorkType? type, int page, int pageSize);
        Task<CreativeWork?> GetWorkByIdAsync(Guid userId, Guid workId);
        Task<CreativeWork> UpdateWorkAsync(Guid userId, Guid workId, string title, string content, Emotion mood);
        Task DeleteWorkAsync(Guid userId, Guid workId);
        Task<CreativeWork> ShareWorkAsync(Guid userId, Guid workId, Guid? groupId);
        Task<(List<CreativeWork> Works, int TotalCount)> GetSharedWorksAsync(Guid? groupId, int page, int pageSize);
        Task<string> GeneratePromptAsync(Guid userId, CreativeWorkType type);
        Task<CollaborativeStory> StartCollaborativeStoryAsync(Guid userId, Guid roomId, string title);
        Task<StoryChapter> AddChapterAsync(Guid userId, Guid storyId, string content);
        Task<(CollaborativeStory Story, List<StoryChapter> Chapters)> GetCollaborativeStoryAsync(Guid storyId);
    }
}
