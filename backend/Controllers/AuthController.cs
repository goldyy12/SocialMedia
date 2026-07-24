using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (result == RegisterResult.UserAlreadyExists)
                return BadRequest(new { error = "Username or Email is already in use." });

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Success)
                return Unauthorized(new { error = "Invalid username or password" });

            SetRefreshTokenCookie(result.RawRefreshToken!, result.RefreshTokenExpiresAt);
            return Ok(new { accessToken = result.AccessToken });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshTokenCookie = Request.Cookies["refreshToken"];
            var result = await _authService.RefreshAsync(refreshTokenCookie);

            if (!result.Success)
                return Unauthorized(new { error = "Invalid or expired refresh token." });

            SetRefreshTokenCookie(result.RawRefreshToken!, result.RefreshTokenExpiresAt);
            return Ok(new { accessToken = result.AccessToken });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshTokenCookie = Request.Cookies["refreshToken"];
            await _authService.LogoutAsync(refreshTokenCookie);

            Response.Cookies.Delete("refreshToken");
            return Ok();
        }

        private void SetRefreshTokenCookie(string rawRefreshToken, DateTime expiresAt)
        {
            Response.Cookies.Append("refreshToken", rawRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expiresAt
            });
        }
    }
}