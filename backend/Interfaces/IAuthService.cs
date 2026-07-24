
using backend.DTOs;

namespace backend.Interfaces
{
    public interface IAuthService
    {
        Task<RegisterResult> RegisterAsync(RegisterDto dto);
        Task<LoginResult> LoginAsync(LoginDto dto);
        Task<RefreshResult> RefreshAsync(string? refreshTokenCookie);
        Task LogoutAsync(string? refreshTokenCookie);
    }
}