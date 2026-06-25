using backend.Data;
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
    public class FollowController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FollowController(AppDbContext context)
        {
            _context = context;
        }

        // Send a follow request
        [HttpPost("{targetUserId}")]
        public async Task<IActionResult> SendFollowRequest(int targetUserId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (userId.Value == targetUserId)
                return BadRequest("You cannot follow yourself");

            var existing = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == userId.Value && f.FollowingId == targetUserId);
            if (existing != null) return BadRequest("Follow request already sent");

            var follow = new Follow
            {
                FollowerId = userId.Value,
                FollowingId = targetUserId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Follows.Add(follow);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Follow request sent" });
        }

        // Accept a follow request
        [HttpPatch("{followerId}/accept")]
        public async Task<IActionResult> AcceptFollowRequest(int followerId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == userId.Value);
            if (follow == null) return NotFound("Follow request not found");
            if (follow.Status == "accepted") return BadRequest("Already accepted");

            var currentUser = await _context.Users.FindAsync(userId.Value);

            _context.Notifications.Add(new Notification
            {
                UserId = followerId,
                Type = "follow_accepted",
                Message = $"{currentUser.Username} accepted your follow request.",
                CreatedAt = DateTime.UtcNow
            });

            follow.Status = "accepted";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Follow request accepted" });
        }


       
        [HttpDelete("followers/{followerId}")]
        public async Task<IActionResult> RemoveFollower(int followerId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == userId.Value);
            if (follow == null) return NotFound("Follower not found");

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Follower removed" });
        }


        [HttpGet("requests")]
        public async Task<IActionResult> GetFollowRequests()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var requests = await _context.Follows
                .AsNoTracking()
                .Where(f => f.FollowingId == userId.Value && f.Status == "pending")
                .Select(f => new
                {
                    f.Id,
                    f.FollowerId,
                    Username = f.Follower.Username,
                    ProfilePic = f.Follower.ProfilePic,
                    f.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }


        [HttpGet("followers")]
        public async Task<IActionResult> GetFollowers()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var followers = await _context.Follows
                .AsNoTracking()
                .Where(f => f.FollowingId == userId.Value && f.Status == "accepted")
                .Select(f => new
                {
                    f.FollowerId,
                    Username = f.Follower.Username,
                    ProfilePic = f.Follower.ProfilePic
                })
                .ToListAsync();

            return Ok(followers);
        }


        [HttpGet("following")]
        public async Task<IActionResult> GetFollowing()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var following = await _context.Follows
                .AsNoTracking()
                .Where(f => f.FollowerId == userId.Value && f.Status == "accepted")
                .Select(f => new
                {
                    f.FollowingId,
                    Username = f.Following.Username,
                    ProfilePic = f.Following.ProfilePic
                })
                .ToListAsync();

            return Ok(following);
        }
        [HttpGet("available-friends")]
        public async Task<IActionResult> GetAvailableFriends()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();
            
            Console.WriteLine($"Current userId: {userId}");
            var friends = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id != userId.Value &&
                            !_context.Follows.Any(f => f.FollowerId == userId.Value && f.FollowingId == u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.ProfilePic,
                    u.Bio,
                    FollowsYou = _context.Follows.Any(f =>
                    f.FollowerId == u.Id &&
                f.FollowingId == userId.Value &&
                f.Status == "accepted"),
                isPending = _context.Follows.Any(f => f.FollowerId == userId.Value && f.FollowingId == u.Id && f.Status == "pending")
                })
                .ToListAsync();
            return Ok(friends);
        }
        [HttpDelete("{targetUserId}")]
        public async Task<IActionResult> Unfollow(int targetUserId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == userId.Value && f.FollowingId == targetUserId);
            if (follow == null) return NotFound("Follow not found");

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Unfollowed successfully" });
        }
    }

}