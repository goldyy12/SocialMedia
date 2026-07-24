
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserSummaryDto>> GetAllUsersAsync(int currentUserId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id != currentUserId)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    ProfilePic = u.ProfilePic,
                    Bio = u.Bio
                })
                .ToListAsync();
        }

        public async Task<List<UserSummaryDto>> SearchUsersAsync(string query, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<UserSummaryDto>();

            var loweredQuery = query.ToLower();

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id != currentUserId && u.Username.ToLower().Contains(loweredQuery))
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    ProfilePic = u.ProfilePic,
                    Bio = u.Bio
                })
                .Take(10)
                .ToListAsync();
        }

        public async Task<UserProfileDto?> GetUserByIdAsync(int id, int currentUserId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    ProfilePic = u.ProfilePic,
                    Bio = u.Bio,
                    CreatedAt = u.CreatedAt,
                    FollowersCount = u.Followers.Count(f => f.Status == "accepted"),
                    FollowingCount = u.Following.Count(f => f.Status == "accepted"),
                    IsFollowing = u.Followers.Any(f => f.FollowerId == currentUserId && f.Status == "accepted")
                })
                .FirstOrDefaultAsync();
        }

        public async Task<UserSummaryDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            user.Bio = dto.Bio;
            user.ProfilePic = dto.ProfilePic;
            await _context.SaveChangesAsync();

            return new UserSummaryDto
            {
                Id = user.Id,
                Username = user.Username,
                ProfilePic = user.ProfilePic,
                Bio = user.Bio
            };
        }
    }
}