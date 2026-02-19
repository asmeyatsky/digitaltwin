using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class CommunityService : ICommunityService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<CommunityService> _logger;

        public CommunityService(DigitalTwinDbContext context, ILogger<CommunityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CommunityGroup> CreateGroupAsync(Guid userId, string name, string description, GroupCategory category)
        {
            var group = new CommunityGroup
            {
                Name = name,
                Description = description,
                Category = category,
                IsModerated = true,
                CreatedByUserId = userId,
                MemberCount = 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommunityGroups.Add(group);

            // Creator automatically becomes a Moderator member
            var membership = new CommunityMembership
            {
                GroupId = group.Id,
                UserId = userId,
                Role = CommunityRole.Moderator,
                JoinedAt = DateTime.UtcNow
            };

            _context.CommunityMemberships.Add(membership);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Community group '{GroupName}' created by user {UserId}", name, userId);
            return group;
        }

        public async Task<(List<CommunityGroup> Groups, int TotalCount)> GetGroupsAsync(GroupCategory? category, string? search, int page, int pageSize)
        {
            var query = _context.CommunityGroups.AsQueryable();

            if (category.HasValue)
                query = query.Where(g => g.Category == category.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(g => g.Name.Contains(search) || g.Description.Contains(search));

            var totalCount = await query.CountAsync();

            var groups = await query
                .OrderByDescending(g => g.MemberCount)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (groups, totalCount);
        }

        public async Task<CommunityGroup?> GetGroupByIdAsync(Guid groupId)
        {
            return await _context.CommunityGroups.FindAsync(groupId);
        }

        public async Task<CommunityMembership> JoinGroupAsync(Guid userId, Guid groupId)
        {
            var group = await _context.CommunityGroups.FindAsync(groupId);
            if (group == null)
                throw new InvalidOperationException("Group not found.");

            var existingMembership = await _context.CommunityMemberships
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (existingMembership != null)
                throw new InvalidOperationException("User is already a member of this group.");

            var membership = new CommunityMembership
            {
                GroupId = groupId,
                UserId = userId,
                Role = CommunityRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            _context.CommunityMemberships.Add(membership);
            group.MemberCount++;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} joined group {GroupId}", userId, groupId);
            return membership;
        }

        public async Task LeaveGroupAsync(Guid userId, Guid groupId)
        {
            var membership = await _context.CommunityMemberships
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (membership == null)
                throw new InvalidOperationException("User is not a member of this group.");

            var group = await _context.CommunityGroups.FindAsync(groupId);
            if (group != null && group.MemberCount > 0)
                group.MemberCount--;

            _context.CommunityMemberships.Remove(membership);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} left group {GroupId}", userId, groupId);
        }

        public async Task<CommunityPost> CreatePostAsync(Guid userId, Guid groupId, string title, string content, bool isAnonymous)
        {
            // Verify user is a member of the group
            var isMember = await _context.CommunityMemberships
                .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (!isMember)
                throw new InvalidOperationException("User must be a member of the group to create a post.");

            var post = new CommunityPost
            {
                GroupId = groupId,
                AuthorUserId = userId,
                Title = title,
                Content = content,
                IsAnonymous = isAnonymous,
                LikeCount = 0,
                ReplyCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommunityPosts.Add(post);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Post '{PostTitle}' created in group {GroupId} by user {UserId}", title, groupId, userId);
            return post;
        }

        public async Task<(List<CommunityPost> Posts, int TotalCount)> GetPostsAsync(Guid groupId, int page, int pageSize)
        {
            var query = _context.CommunityPosts.Where(p => p.GroupId == groupId);

            var totalCount = await query.CountAsync();

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (posts, totalCount);
        }

        public async Task<CommunityPost?> GetPostByIdAsync(Guid postId)
        {
            return await _context.CommunityPosts.FindAsync(postId);
        }

        public async Task<CommunityReply> ReplyToPostAsync(Guid userId, Guid postId, string content, bool isAnonymous)
        {
            var post = await _context.CommunityPosts.FindAsync(postId);
            if (post == null)
                throw new InvalidOperationException("Post not found.");

            // Verify user is a member of the group
            var isMember = await _context.CommunityMemberships
                .AnyAsync(m => m.GroupId == post.GroupId && m.UserId == userId);

            if (!isMember)
                throw new InvalidOperationException("User must be a member of the group to reply.");

            var reply = new CommunityReply
            {
                PostId = postId,
                AuthorUserId = userId,
                Content = content,
                IsAnonymous = isAnonymous,
                LikeCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommunityReplies.Add(reply);
            post.ReplyCount++;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reply added to post {PostId} by user {UserId}", postId, userId);
            return reply;
        }

        public async Task<(List<CommunityReply> Replies, int TotalCount)> GetRepliesAsync(Guid postId, int page, int pageSize)
        {
            var query = _context.CommunityReplies.Where(r => r.PostId == postId);

            var totalCount = await query.CountAsync();

            var replies = await query
                .OrderBy(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (replies, totalCount);
        }

        public async Task LikePostAsync(Guid userId, Guid postId)
        {
            var post = await _context.CommunityPosts.FindAsync(postId);
            if (post == null)
                throw new InvalidOperationException("Post not found.");

            post.LikeCount++;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Post {PostId} liked by user {UserId}", postId, userId);
        }

        public async Task LikeReplyAsync(Guid userId, Guid replyId)
        {
            var reply = await _context.CommunityReplies.FindAsync(replyId);
            if (reply == null)
                throw new InvalidOperationException("Reply not found.");

            reply.LikeCount++;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reply {ReplyId} liked by user {UserId}", replyId, userId);
        }

        public async Task<List<CommunityGroup>> GetUserGroupsAsync(Guid userId)
        {
            var groupIds = await _context.CommunityMemberships
                .Where(m => m.UserId == userId)
                .Select(m => m.GroupId)
                .ToListAsync();

            return await _context.CommunityGroups
                .Where(g => groupIds.Contains(g.Id))
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<CommunityGroup>> GetSuggestedGroupsAsync(Guid userId)
        {
            var joinedGroupIds = await _context.CommunityMemberships
                .Where(m => m.UserId == userId)
                .Select(m => m.GroupId)
                .ToListAsync();

            return await _context.CommunityGroups
                .Where(g => !joinedGroupIds.Contains(g.Id))
                .OrderByDescending(g => g.MemberCount)
                .Take(10)
                .ToListAsync();
        }
    }
}
