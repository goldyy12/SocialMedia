using backend.DTOs;
using backend.Helpers;
using backend.Hubs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _conversationService;

        public ConversationsController(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        [HttpPost("{userId2}")]
        public async Task<IActionResult> StartConversation(int userId2)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var conversationId = await _conversationService.StartConversationAsync(userId.Value, userId2);
            return Ok(new { conversationId });
        }

        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var conversations = await _conversationService.GetConversationsAsync(userId.Value);
            return Ok(conversations);
        }

        [HttpGet("{conversationId}/messages")]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var (result, messages) = await _conversationService.GetMessagesAsync(conversationId, userId.Value);
            return result switch
            {
                GetMessagesResult.ConversationNotFound => NotFound(),
                GetMessagesResult.Success => Ok(messages),
                _ => StatusCode(500)
            };
        }

        [HttpPost("{conversationId}/messages")]
        public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendMessageDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var (result, message) = await _conversationService.SendMessageAsync(conversationId, userId.Value, dto);
            return result switch
            {
                SendMessageResult.ConversationNotFound => NotFound(),
                SendMessageResult.Success => Ok(message),
                _ => StatusCode(500)
            };
        }
    }
}