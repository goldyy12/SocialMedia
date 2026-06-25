using backend.Data;
using backend.DTOs;
using backend.Helpers;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetCommentsForPost(int postId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound("Post not found");

            var comments = await _context.Comments
                .AsNoTracking()
                .Where(c => c.PostId == postId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    c.CreatedAt,
                    c.UserId,
                    Username = c.User.Username,
                    ProfilePic = c.User.ProfilePic
                })
                .ToListAsync();

            return Ok(comments);
        }

        [HttpPost("{postId}")]
        public async Task<IActionResult> AddCommentToPost(int postId, [FromBody] CommentDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound("Post not found");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return Unauthorized("User not found");

            var comment = new Comment
            {
                Content = dto.Content,
                PostId = postId,
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };
            if (post.UserId != userId.Value)
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

            return Ok(new
            {
                comment.Id,
                comment.Content,
                comment.CreatedAt,
                comment.UserId,
                Username = user.Username,
                ProfilePic = user.ProfilePic
            });
        }

        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound("Comment not found");
            if (comment.UserId != userId.Value) return Forbid();

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPut("{commentId}")]
        public async Task<IActionResult> EditComment(int commentId, [FromBody] CommentDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound("Comment not found");
            if (comment.UserId != userId.Value) return Forbid();
            comment.Content = dto.Content;
            await _context.SaveChangesAsync();
            return Ok(new
            {
                comment.Id,
                comment.Content,
                comment.CreatedAt,
                comment.UserId
            });
        }     }
}