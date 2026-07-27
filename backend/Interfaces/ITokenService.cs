using backend.Models;
using Google.Apis.Auth;

namespace backend.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        string HashToken(string token);
        Task<GoogleJsonWebSignature.Payload?> VerifyGoogleIdTokenAsync(string idToken);
    }
}