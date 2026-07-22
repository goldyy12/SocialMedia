using backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.DTOs;
using backend.Services; // fixed: was backend.Service

namespace backend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly TokenService _tokenService; // fixed: named field

        public AuthController(
            IConfiguration configuration,
            AppDbContext context,
            ILogger<AuthController> logger,
            TokenService tokenService) // fixed: injected
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            bool userExists = await _context.Users.AnyAsync(u =>
                u.Email.ToLower() == dto.Email.ToLower() ||
                u.Username.ToLower() == dto.Username.ToLower());

            if (userExists)
            {
                _logger.LogWarning("Registration attempt failed — duplicate user for {Email}", dto.Email);
                return BadRequest(new { error = "Username or Email is already in use." });
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = hashedPassword
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User registered {UserId} {Username}", user.Id, user.Username);

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for {Email}", dto.Email);
                return Unauthorized(new { error = "Invalid username or password" });
            }


            var accessToken = _tokenService.GenerateAccessToken(user);
            var rawRefreshToken = _tokenService.GenerateRefreshToken();
            var hashedToken = _tokenService.HashToken(rawRefreshToken);

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = hashedToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            Response.Cookies.Append("refreshToken", rawRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = refreshTokenEntity.ExpiresAt
            });

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            return Ok(new { accessToken });
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshTokenCookie = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshTokenCookie))
            {
                return Unauthorized(new { error = "No refresh token provided." });
            }

            var hashedToken = _tokenService.HashToken(refreshTokenCookie);
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken);

            if (tokenEntity == null || tokenEntity.ExpiresAt < DateTime.UtcNow)
            {
                return Unauthorized(new { error = "Invalid or expired refresh token." });
            }

            var user = await _context.Users.FindAsync(tokenEntity.UserId);
            if (user == null)
            {
                return Unauthorized(new { error = "User no longer exists." });
            }

            // Rotate: remove the old refresh token, issue a new one
            _context.RefreshTokens.Remove(tokenEntity);

            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRawRefreshToken = _tokenService.GenerateRefreshToken();
            var newHashedToken = _tokenService.HashToken(newRawRefreshToken);

            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newHashedToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync();

            Response.Cookies.Append("refreshToken", newRawRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = newRefreshTokenEntity.ExpiresAt
            });

            return Ok(new { accessToken = newAccessToken });
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshTokenCookie = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshTokenCookie))
            {
                var hashedToken = _tokenService.HashToken(refreshTokenCookie);
                var tokenEntity = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken);

                if (tokenEntity != null)
                {
                    _context.RefreshTokens.Remove(tokenEntity);
                    await _context.SaveChangesAsync();
                }
            }

            Response.Cookies.Delete("refreshToken");
            return Ok();
        }
    }
}