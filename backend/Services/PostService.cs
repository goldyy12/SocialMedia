
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace backend.Services
{
    public class PostService : IPostService
    {
        private readonly AppDbContext _context;

        public PostService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PostResponseDto?> CreatePostAsync(int userId, PostDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var post = new Post
            {
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return new PostResponseDto
            {
                Id = post.Id,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                CreatedAt = post.CreatedAt,
                UserId = post.UserId,
                Username = user.Username,
                ProfilePic = user.ProfilePic
            };
        }
        private static Expression<Func<Post, PostResponseDto>> ToDto(int currentUserId) =>
    p => new PostResponseDto
    {
        Id = p.Id,
        Content = p.Content,
        ImageUrl = p.ImageUrl,
        CreatedAt = p.CreatedAt,
        UserId = p.UserId,
        Username = p.User.Username,
        ProfilePic = p.User.ProfilePic,
        LikesCount = p.Likes.Count,
        IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
        Comments = p.Comments
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentResponseDto
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                UserId = c.UserId,
                Username = c.User.Username,
                ProfilePic = c.User.ProfilePic
            }).ToList()
    };


        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 50;

        public async Task<List<PostResponseDto>> GetFeedAsync(int currentUserId, int? beforeId = null, int limit = DefaultPageSize)
        {
            var query = _context.Posts
                .AsNoTracking()
                .Where(p =>
                    _context.Follows.Any(f =>
                        f.FollowerId == currentUserId &&
                        f.FollowingId == p.UserId &&
                        f.Status == FollowStatus.Accepted)
                    || p.UserId == currentUserId);

            return await PageAsync(query, currentUserId, beforeId, limit);
        }

        public async Task<List<PostResponseDto>> GetPostsByUserAsync(int profileUserId, int currentUserId, int? beforeId = null, int limit = DefaultPageSize)
        {
            var query = _context.Posts
                .AsNoTracking()
                .Where(p => p.UserId == profileUserId);

            return await PageAsync(query, currentUserId, beforeId, limit);
        }

        // Keyset (cursor) pagination: newest first, ordered by (CreatedAt, Id).
        // `beforeId` is the id of the last post the client already has; we return
        // the posts that come after it. Unlike Skip/Take, new posts arriving at the
        // top don't shift pages and cause duplicates.
        private async Task<List<PostResponseDto>> PageAsync(IQueryable<Post> query, int currentUserId, int? beforeId, int limit)
        {
            limit = Math.Clamp(limit, 1, MaxPageSize);

            if (beforeId.HasValue)
            {
                var cursor = await _context.Posts
                    .AsNoTracking()
                    .Where(p => p.Id == beforeId.Value)
                    .Select(p => new { p.Id, p.CreatedAt })
                    .FirstOrDefaultAsync();

                if (cursor == null) return new List<PostResponseDto>();

                query = query.Where(p =>
                    p.CreatedAt < cursor.CreatedAt ||
                    (p.CreatedAt == cursor.CreatedAt && p.Id < cursor.Id));
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Take(limit)
                .Select(ToDto(currentUserId))
                .ToListAsync();
        }

        public async Task<bool?> DeletePostAsync(int postId, int userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return null;
            if (post.UserId != userId) return false;

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(UpdatePostResult Result, PostResponseDto? Post)> UpdatePostAsync(int postId, int userId, PostDto dto)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return (UpdatePostResult.NotFound, null);
            if (post.UserId != userId) return (UpdatePostResult.Forbidden, null);

            var user = await _context.Users.FindAsync(userId);

            post.Content = dto.Content;
            // The edit form only sends text — don't wipe an existing image.
            if (dto.ImageUrl != null) post.ImageUrl = dto.ImageUrl;
            await _context.SaveChangesAsync();

            return (UpdatePostResult.Success, new PostResponseDto
            {
                Id = post.Id,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                CreatedAt = post.CreatedAt,
                UserId = post.UserId,
                Username = user!.Username,
                ProfilePic = user.ProfilePic,
                LikesCount = await _context.Likes.CountAsync(l => l.PostId == post.Id),
                IsLikedByCurrentUser = await _context.Likes.AnyAsync(l => l.PostId == post.Id && l.UserId == userId)
            });
        }
    }
}