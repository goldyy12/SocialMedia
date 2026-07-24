
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class FollowService : IFollowService
    {
        private readonly AppDbContext _context;

        public FollowService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SendFollowResult> SendFollowRequestAsync(int userId, int targetUserId)
        {
            if (userId == targetUserId) return SendFollowResult.CannotFollowSelf;

            var existing = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowingId == targetUserId);
            if (existing != null) return SendFollowResult.AlreadySent;

            _context.Follows.Add(new Follow
            {
                FollowerId = userId,
                FollowingId = targetUserId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return SendFollowResult.Success;
        }

        public async Task<AcceptFollowResult> AcceptFollowRequestAsync(int userId, int followerId)
        {
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == userId);
            if (follow == null) return AcceptFollowResult.NotFound;
            if (follow.Status == "accepted") return AcceptFollowResult.AlreadyAccepted;

            var currentUser = await _context.Users.FindAsync(userId);

            _context.Notifications.Add(new Notification
            {
                UserId = followerId,
                Type = "follow_accepted",
                Message = $"{currentUser!.Username} accepted your follow request.",
                CreatedAt = DateTime.UtcNow
            });

            follow.Status = "accepted";
            await _context.SaveChangesAsync();
            return AcceptFollowResult.Success;
        }

        public async Task<RemoveFollowerResult> RemoveFollowerAsync(int userId, int followerId)
        {
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == userId);
            if (follow == null) return RemoveFollowerResult.NotFound;

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();
            return RemoveFollowerResult.Success;
        }

        public async Task<UnfollowResult> UnfollowAsync(int userId, int targetUserId)
        {
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowingId == targetUserId);
            if (follow == null) return UnfollowResult.NotFound;

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();
            return UnfollowResult.Success;
        }

        public async Task<List<FollowRequestDto>> GetFollowRequestsAsync(int userId)
        {
            return await _context.Follows
                .AsNoTracking()
                .Where(f => f.FollowingId == userId && f.Status == "pending")
                .Select(f => new FollowRequestDto
                {
                    Id = f.Id,
                    FollowerId = f.FollowerId,
                    Username = f.Follower.Username,
                    ProfilePic = f.Follower.ProfilePic,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<List<FollowerDto>> GetFollowersAsync(int userId)
        {
            return await _context.Follows
                .AsNoTracking()
                .Where(f => f.FollowingId == userId && f.Status == "accepted")
                .Select(f => new FollowerDto
                {
                    FollowerId = f.FollowerId,
                    Username = f.Follower.Username,
                    ProfilePic = f.Follower.ProfilePic
                })
                .ToListAsync();
        }

        public async Task<List<FollowingDto>> GetFollowingAsync(int userId)
        {
            return await _context.Follows
                .AsNoTracking()
                .Where(f => f.FollowerId == userId && f.Status == "accepted")
                .Select(f => new FollowingDto
                {
                    FollowingId = f.FollowingId,
                    Username = f.Following.Username,
                    ProfilePic = f.Following.ProfilePic
                })
                .ToListAsync();
        }

        public async Task<List<AvailableFriendDto>> GetAvailableFriendsAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id != userId &&
                            !_context.Follows.Any(f => f.FollowerId == userId && f.FollowingId == u.Id))
                .Select(u => new AvailableFriendDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    ProfilePic = u.ProfilePic,
                    Bio = u.Bio,
                    FollowsYou = _context.Follows.Any(f =>
                        f.FollowerId == u.Id &&
                        f.FollowingId == userId &&
                        f.Status == "accepted"),
                    IsPending = _context.Follows.Any(f =>
                        f.FollowerId == userId &&
                        f.FollowingId == u.Id &&
                        f.Status == "pending")
                })
                .ToListAsync();
        }
    }
}