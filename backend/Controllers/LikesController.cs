using backend.Helpers;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LikeController : ControllerBase
    {
        private readonly ILikeService _likeService;

        public LikeController(ILikeService likeService)
        {
            _likeService = likeService;
        }

        [HttpPost("{postId}")]
        public async Task<IActionResult> LikePost(int postId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var result = await _likeService.LikePostAsync(postId, userId.Value);

            return result switch
            {
                LikeResult.PostNotFound => NotFound("Post not found"),
                LikeResult.AlreadyLiked => BadRequest("Post already liked"),
                LikeResult.Success => Ok(new { message = "Post liked successfully" }),
                _ => StatusCode(500)
            };
        }

        [HttpDelete("{postId}")]
        public async Task<IActionResult> UnlikePost(int postId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var result = await _likeService.UnlikePostAsync(postId, userId.Value);

            return result switch
            {
                UnlikeResult.LikeNotFound => NotFound("Like not found"),
                UnlikeResult.Success => Ok(new { message = "Post unliked successfully" }),
                _ => StatusCode(500)
            };
        }
    }
}