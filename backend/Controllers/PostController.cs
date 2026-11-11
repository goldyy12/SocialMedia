using backend.DTOs;
using backend.Helpers;
using backend.Interfaces;
using backend.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] PostDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var result = await _postService.CreatePostAsync(userId.Value, dto);
            if (result == null) return Unauthorized("User not found");

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPosts([FromQuery] int? beforeId, [FromQuery] int limit = PostService.DefaultPageSize)
        {
            int? currentUserId = User.GetCurrentUserId();
            if (currentUserId == null) return Unauthorized("Invalid user ID in token");

            var posts = await _postService.GetFeedAsync(currentUserId.Value, beforeId, limit);
            return Ok(posts);
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetPostsByUser(int id, [FromQuery] int? beforeId, [FromQuery] int limit = PostService.DefaultPageSize)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var posts = await _postService.GetPostsByUserAsync(id, userId.Value, beforeId, limit);
            return Ok(posts);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var result = await _postService.DeletePostAsync(id, userId.Value);
            if (result == null) return NotFound("Post not found");
            if (result == false) return Forbid();

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] PostDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var (result, updatedPost) = await _postService.UpdatePostAsync(id, userId.Value, dto);

            return result switch
            {
                UpdatePostResult.NotFound => NotFound("Post not found"),
                UpdatePostResult.Forbidden => Forbid(),
                UpdatePostResult.Success => Ok(updatedPost),
                _ => StatusCode(500)
            };
        }

        private const long MaxUploadBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        [HttpPost("upload")]
        [RequestSizeLimit(MaxUploadBytes + 64 * 1024)] // small allowance for multipart overhead
        public async Task<IActionResult> UploadImage(IFormFile file, [FromServices] Cloudinary cloudinary)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            if (file.Length > MaxUploadBytes)
                return BadRequest("File is too large (max 5 MB)");

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImageTypes.Contains(file.ContentType) || !AllowedImageExtensions.Contains(extension))
                return BadRequest("Only JPEG, PNG, GIF and WebP images are allowed");

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "odinbook"
            };

            // Cloudinary decodes the file as an image and rejects anything that isn't one.
            var result = await cloudinary.UploadAsync(uploadParams);
            if (result.Error != null || result.SecureUrl == null)
                return BadRequest("Image upload failed");

            return Ok(new { url = result.SecureUrl.ToString() });
        }
    }
}