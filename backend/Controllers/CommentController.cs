using backend.DTOs;
using backend.Helpers;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetCommentsForPost(int postId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var comments = await _commentService.GetCommentsForPostAsync(postId);
            if (comments == null) return NotFound("Post not found");

            return Ok(comments);
        }

        [HttpPost("{postId}")]
        public async Task<IActionResult> AddCommentToPost(int postId, [FromBody] CommentDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var result = await _commentService.AddCommentAsync(postId, userId.Value, dto);
            if (result == null) return NotFound("Post or user not found");

            return Ok(result);
        }

        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var result = await _commentService.DeleteCommentAsync(commentId, userId.Value);
            if (result == null) return NotFound("Comment not found");
            if (result == false) return Forbid();

            return NoContent();
        }

        [HttpPut("{commentId}")]
        public async Task<IActionResult> EditComment(int commentId, [FromBody] CommentDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid user ID in token");

            var result = await _commentService.EditCommentAsync(commentId, userId.Value, dto);
            if (result == null) return NotFound("Comment not found");

            return Ok(result);
        }
    }
}