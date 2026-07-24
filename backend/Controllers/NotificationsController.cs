using backend.Helpers;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId.Value);
            return Ok(notifications);
        }

        [HttpPatch("read")]
        public async Task<IActionResult> MarkAllRead()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            await _notificationService.MarkAllReadAsync(userId.Value);
            return Ok();
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId.Value);
            return Ok(new { count });
        }
    }
}