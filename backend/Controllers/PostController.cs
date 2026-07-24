using backend.DTOs;
using backend.Helpers;
using backend.Interfaces;
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
        public async Task<IActionResult> GetPosts()
        {
            int currentUserId = User.GetCurrentUserId() ?? 0;
            var posts = await _postService.GetFeedAsync(currentUserId);
            return Ok(posts);
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetPostById(int id)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var posts = await _postService.GetPostsByUserAsync(id);
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

            var result = await _postService.UpdatePostAsync(id, userId.Value, dto);
            if (result == null) return NotFound("Post not found");
            if (result == false) return Forbid();

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