
using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class LikeService : ILikeService
    {
        private readonly AppDbContext _context;

        public LikeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LikeResult> LikePostAsync(int postId, int userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return LikeResult.PostNotFound;

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);
            if (existingLike != null) return LikeResult.AlreadyLiked;

            var like = new Like
            {
                UserId = userId,
                PostId = postId,
                CreatedAt = DateTime.UtcNow
            };

            if (post.UserId != userId)
            {
                var liker = await _context.Users.FindAsync(userId);
                _context.Notifications.Add(new Notification
                {
                    UserId = post.UserId,
                    Type = "like",
                    Message = $"{liker!.Username} liked your post.",
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            return LikeResult.Success;
        }

        public async Task<UnlikeResult> UnlikePostAsync(int postId, int userId)
        {
            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);
            if (like == null) return UnlikeResult.LikeNotFound;

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            return UnlikeResult.Success;
        }
    }
}