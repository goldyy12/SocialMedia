
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, TokenService tokenService, ILogger<AuthService> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<RegisterResult> RegisterAsync(RegisterDto dto)
        {
            bool userExists = await _context.Users.AnyAsync(u =>
                u.Email.ToLower() == dto.Email.ToLower() ||
                u.Username.ToLower() == dto.Username.ToLower());

            if (userExists)
            {
                _logger.LogWarning("Registration attempt failed — duplicate user for {Email}", dto.Email);
                return RegisterResult.UserAlreadyExists;
            }

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User registered {UserId} {Username}", user.Id, user.Username);

            return RegisterResult.Success;
        }

        public async Task<LoginResult> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for {Email}", dto.Email);
                return new LoginResult { Success = false };
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

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            return new LoginResult
            {
                Success = true,
                AccessToken = accessToken,
                RawRefreshToken = rawRefreshToken,
                RefreshTokenExpiresAt = refreshTokenEntity.ExpiresAt
            };
        }

        public async Task<RefreshResult> RefreshAsync(string? refreshTokenCookie)
        {
            if (string.IsNullOrEmpty(refreshTokenCookie))
                return new RefreshResult { Success = false };

            var hashedToken = _tokenService.HashToken(refreshTokenCookie);
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken);

            if (tokenEntity == null || tokenEntity.ExpiresAt < DateTime.UtcNow)
                return new RefreshResult { Success = false };

            var user = await _context.Users.FindAsync(tokenEntity.UserId);
            if (user == null)
                return new RefreshResult { Success = false };

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
            _context.RefreshTokens.Remove(tokenEntity);
            await _context.SaveChangesAsync(); // single save instead of two round trips

            return new RefreshResult
            {
                Success = true,
                AccessToken = newAccessToken,
                RawRefreshToken = newRawRefreshToken,
                RefreshTokenExpiresAt = newRefreshTokenEntity.ExpiresAt
            };
        }

        public async Task LogoutAsync(string? refreshTokenCookie)
        {
            if (string.IsNullOrEmpty(refreshTokenCookie)) return;

            var hashedToken = _tokenService.HashToken(refreshTokenCookie);
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken);

            if (tokenEntity != null)
            {
                _context.RefreshTokens.Remove(tokenEntity);
                await _context.SaveChangesAsync();
            }
        }
    }
}