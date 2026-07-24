
namespace backend.Interfaces
{
    public enum RegisterResult { Success, UserAlreadyExists }

    public class LoginResult
    {
        public bool Success { get; init; }
        public string? AccessToken { get; init; }
        public string? RawRefreshToken { get; init; }
        public DateTime RefreshTokenExpiresAt { get; init; }
    }

    public class RefreshResult
    {
        public bool Success { get; init; }
        public string? AccessToken { get; init; }
        public string? RawRefreshToken { get; init; }
        public DateTime RefreshTokenExpiresAt { get; init; }
    }
}