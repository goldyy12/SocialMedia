using backend.Data;
using backend.Helpers;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.DTOs;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LikeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LikeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{postId}")]
        public async Task<IActionResult> LikePost(int postId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound("Post not found");

            
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId.Value && l.PostId == postId);
            if (existingLike != null) return BadRequest("Post already liked");

            var like = new Like
            {
                UserId = userId.Value,
                PostId = postId,
                CreatedAt = DateTime.UtcNow
            };
            if (post.UserId != userId.Value)
            {
                var liker = await _context.Users.FindAsync(userId.Value);
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

            return Ok(new { message = "Post liked successfully" });
        }

        [HttpDelete("{postId}")]
        public async Task<IActionResult> UnlikePost(int postId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId.Value && l.PostId == postId);
            if (like == null) return NotFound("Like not found");

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Post unliked successfully" });
        }
    }
}