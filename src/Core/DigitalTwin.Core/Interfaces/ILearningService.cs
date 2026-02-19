using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface ILearningService
    {
        Task<List<LearningPath>> GetPathsAsync(LearningCategory? category);
        Task<(LearningPath? Path, List<LearningModule> Modules)> GetPathByIdAsync(Guid pathId);
        Task<UserLearningProgress> StartPathAsync(Guid userId, Guid pathId);
        Task<(LearningModule? Module, UserLearningProgress? Progress)> GetCurrentModuleAsync(Guid userId, Guid pathId);
        Task<UserLearningProgress> CompleteModuleAsync(Guid userId, Guid pathId, string? reflectionNotes);
        Task<List<UserLearningProgress>> GetProgressAsync(Guid userId);
        Task<LearningPath?> GetSuggestedPathAsync(Guid userId);
        Task SeedLearningContentAsync();
    }
}
