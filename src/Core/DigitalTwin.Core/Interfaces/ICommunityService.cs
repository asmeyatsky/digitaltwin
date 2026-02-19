using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.Core.Interfaces
{
    public interface ICommunityService
    {
        Task<CommunityGroup> CreateGroupAsync(Guid userId, string name, string description, GroupCategory category);
        Task<(List<CommunityGroup> Groups, int TotalCount)> GetGroupsAsync(GroupCategory? category, string? search, int page, int pageSize);
        Task<CommunityGroup?> GetGroupByIdAsync(Guid groupId);
        Task<CommunityMembership> JoinGroupAsync(Guid userId, Guid groupId);
        Task LeaveGroupAsync(Guid userId, Guid groupId);
        Task<CommunityPost> CreatePostAsync(Guid userId, Guid groupId, string title, string content, bool isAnonymous);
        Task<(List<CommunityPost> Posts, int TotalCount)> GetPostsAsync(Guid groupId, int page, int pageSize);
        Task<CommunityPost?> GetPostByIdAsync(Guid postId);
        Task<CommunityReply> ReplyToPostAsync(Guid userId, Guid postId, string content, bool isAnonymous);
        Task<(List<CommunityReply> Replies, int TotalCount)> GetRepliesAsync(Guid postId, int page, int pageSize);
        Task LikePostAsync(Guid userId, Guid postId);
        Task LikeReplyAsync(Guid userId, Guid replyId);
        Task<List<CommunityGroup>> GetUserGroupsAsync(Guid userId);
        Task<List<CommunityGroup>> GetSuggestedGroupsAsync(Guid userId);
    }
}
