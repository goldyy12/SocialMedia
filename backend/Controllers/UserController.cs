using backend.DTOs;
using backend.Helpers;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            return Ok(await _userService.GetAllUsersAsync(userId.Value));
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            return Ok(await _userService.SearchUsersAsync(query, userId.Value));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var user = await _userService.GetUserByIdAsync(id, userId.Value);
            if (user == null) return NotFound();

            return Ok(user);
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _userService.UpdateProfileAsync(userId.Value, dto);
            if (result == null) return Unauthorized();

            return Ok(result);
        }
    }
}