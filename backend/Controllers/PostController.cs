using backend.Data;
using backend.DTOs;
using backend.Helpers;
using backend.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PostController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] PostDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return Unauthorized("User not found");

            var post = new Post
            {
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return Ok(new PostResponseDto
            {
                Id = post.Id,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                CreatedAt = post.CreatedAt,
                UserId = post.UserId,
                Username = user.Username,
                ProfilePic = user.ProfilePic
            });

        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPosts()
        {
            int currentUserId = User.GetCurrentUserId() ?? 0;
            var posts = await _context.Posts
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Where(p => (_context.Follows.Any(f => f.FollowerId == currentUserId && f.FollowingId == p.UserId && f.Status == "accepted") || p.UserId == currentUserId))
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

            return Ok(posts);
        }
        [HttpGet("user/{id}")]
        [Authorize]

        public async Task <IActionResult> GetPostById(int id)
        {

            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.UserId == id)
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
                }).ToListAsync();

            return Ok(posts);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound("Post not found");
            if (post.UserId != userId.Value) return Forbid();
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] PostDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound("Post not found");
            if (post.UserId != userId.Value) return Forbid();

            post.Content = dto.Content;
            post.ImageUrl = dto.ImageUrl;

            await _context.SaveChangesAsync();

            
            return Ok("Post Updated Successfully");
        }
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromServices] Cloudinary cloudinary)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "odinbook"
            };

            var result = await cloudinary.UploadAsync(uploadParams);
            return Ok(new { url = result.SecureUrl.ToString() });
        }
    }
    }