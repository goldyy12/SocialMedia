using backend.Data;
using backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.DTOs;
namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id != userId.Value)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.ProfilePic,
                    u.Bio
                })
                .ToListAsync();

            return Ok(users);
        }
          [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<object>());

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id != userId.Value && u.Username.ToLower().Contains(query.ToLower()))
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.ProfilePic,
                    u.Bio
                })
                .Take(10)
                .ToListAsync();

            return Ok(users);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.ProfilePic,
                    u.Bio,
                    u.CreatedAt,
                    FollowersCount = u.Followers.Count(f => f.Status == "accepted"),
                    FollowingCount = u.Following.Count(f => f.Status == "accepted"),
                    IsFollowing = u.Followers.Any(f => f.FollowerId == userId.Value && f.Status == "accepted")
                })
                .FirstOrDefaultAsync();
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return Unauthorized();

            user.Bio = dto.Bio;
            user.ProfilePic = dto.ProfilePic;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Bio,
                user.ProfilePic
            });
        }
      
    }
    }
