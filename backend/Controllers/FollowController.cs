using backend.Helpers;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FollowController : ControllerBase
    {
        private readonly IFollowService _followService;

        public FollowController(IFollowService followService)
        {
            _followService = followService;
        }

        [HttpPost("{targetUserId}")]
        public async Task<IActionResult> SendFollowRequest(int targetUserId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _followService.SendFollowRequestAsync(userId.Value, targetUserId);
            return result switch
            {
                SendFollowResult.CannotFollowSelf => BadRequest("You cannot follow yourself"),
                SendFollowResult.AlreadySent => BadRequest("Follow request already sent"),
                SendFollowResult.Success => Ok(new { message = "Follow request sent" }),
                _ => StatusCode(500)
            };
        }

        [HttpPatch("{followerId}/accept")]
        public async Task<IActionResult> AcceptFollowRequest(int followerId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _followService.AcceptFollowRequestAsync(userId.Value, followerId);
            return result switch
            {
                AcceptFollowResult.NotFound => NotFound("Follow request not found"),
                AcceptFollowResult.AlreadyAccepted => BadRequest("Already accepted"),
                AcceptFollowResult.Success => Ok(new { message = "Follow request accepted" }),
                _ => StatusCode(500)
            };
        }

        [HttpDelete("followers/{followerId}")]
        public async Task<IActionResult> RemoveFollower(int followerId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _followService.RemoveFollowerAsync(userId.Value, followerId);
            return result switch
            {
                RemoveFollowerResult.NotFound => NotFound("Follower not found"),
                RemoveFollowerResult.Success => Ok(new { message = "Follower removed" }),
                _ => StatusCode(500)
            };
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetFollowRequests()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            return Ok(await _followService.GetFollowRequestsAsync(userId.Value));
        }

        [HttpGet("followers")]
        public async Task<IActionResult> GetFollowers()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            return Ok(await _followService.GetFollowersAsync(userId.Value));
        }

        [HttpGet("following")]
        public async Task<IActionResult> GetFollowing()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            return Ok(await _followService.GetFollowingAsync(userId.Value));
        }

        [HttpGet("available-friends")]
        public async Task<IActionResult> GetAvailableFriends()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            return Ok(await _followService.GetAvailableFriendsAsync(userId.Value));
        }

        [HttpDelete("{targetUserId}")]
        public async Task<IActionResult> Unfollow(int targetUserId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _followService.UnfollowAsync(userId.Value, targetUserId);
            return result switch
            {
                UnfollowResult.NotFound => NotFound("Follow not found"),
                UnfollowResult.Success => Ok(new { message = "Unfollowed successfully" }),
                _ => StatusCode(500)
            };
        }
    }
}