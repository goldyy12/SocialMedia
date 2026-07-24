// Services/CommentService.cs
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class CommentService : ICommentService
    {
        private readonly AppDbContext _context;

        public CommentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CommentResponseDto>?> GetCommentsForPostAsync(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return null;

            return await _context.Comments
                .AsNoTracking()
                .Where(c => c.PostId == postId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UserId = c.UserId,
                    Username = c.User.Username,
                    ProfilePic = c.User.ProfilePic
                })
                .ToListAsync();
        }

        public async Task<CommentResponseDto?> AddCommentAsync(int postId, int userId, CommentDto dto)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return null;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var comment = new Comment
            {
                Content = dto.Content,
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            if (post.UserId != userId)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = post.UserId,
                    Type = "comment",
                    Message = $"{user.Username} commented on your post.",
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return new CommentResponseDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId,
                Username = user.Username,
                ProfilePic = user.ProfilePic
            };
        }

        public async Task<bool?> DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return null;
            if (comment.UserId != userId) return false;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CommentResponseDto?> EditCommentAsync(int commentId, int userId, CommentDto dto)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null) return null;
            if (comment.UserId != userId) return null; // handled separately below via a forbidden check if you want to distinguish

            comment.Content = dto.Content;
            await _context.SaveChangesAsync();

            return new CommentResponseDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId,
                Username = comment.User.Username,
                ProfilePic = comment.User.ProfilePic
            };
        }
    }
}