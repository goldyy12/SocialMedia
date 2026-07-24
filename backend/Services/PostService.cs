
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<PostResponseDto>> GetFeedAsync(int currentUserId)
        {
            return await _context.Posts
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Where(p =>
                    _context.Follows.Any(f =>
                        f.FollowerId == currentUserId &&
                        f.FollowingId == p.UserId &&
                        f.Status == "accepted")
                    || p.UserId == currentUserId)
                .Select(p => new PostResponseDto
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
                })
                .ToListAsync();
        }

        public async Task<List<PostResponseDto>> GetPostsByUserAsync(int userId)
        {
            return await _context.Posts
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Content = p.Content,
                    ImageUrl = p.ImageUrl,
                    CreatedAt = p.CreatedAt,
                    UserId = p.UserId,
                    Username = p.User.Username,
                    ProfilePic = p.User.ProfilePic
                })
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

        public async Task<bool?> UpdatePostAsync(int postId, int userId, PostDto dto)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return null;
            if (post.UserId != userId) return false;

            post.Content = dto.Content;
            post.ImageUrl = dto.ImageUrl;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}