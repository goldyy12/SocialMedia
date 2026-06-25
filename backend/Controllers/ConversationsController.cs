using backend.Data;
using backend.Helpers;
using backend.Models;
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
    public class ConversationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ConversationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{userId2}")]
        public async Task<IActionResult> StartConversation(int userId2)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            
            var existing = await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.User1Id == userId.Value && c.User2Id == userId2) ||
                    (c.User1Id == userId2 && c.User2Id == userId.Value));

            if (existing != null)
                return Ok(new { conversationId = existing.Id });

            
            var conversation = new Conversation
            {
                User1Id = userId.Value,
                User2Id = userId2,
                CreatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return Ok(new { conversationId = conversation.Id });
        }
        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var conversations = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.User1Id == userId.Value || c.User2Id == userId.Value)
                .Select(c => new
                {
                    c.Id,
                    OtherUser = c.User1Id == userId.Value
                        ? new { c.User2.Id, c.User2.Username, c.User2.ProfilePic }
                        : new { c.User1.Id, c.User1.Username, c.User1.ProfilePic },
                    LastMessage = c.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => new { m.Content, m.CreatedAt, m.SenderId })
                        .FirstOrDefault(),
                    UnreadCount = c.Messages
                        .Count(m => m.SenderId != userId.Value && !m.IsRead)
                })
                .OrderByDescending(c => c.LastMessage!.CreatedAt)
                .ToListAsync();

            return Ok(conversations);
        }
        [HttpGet("{conversationId}/messages")]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId &&
                    (c.User1Id == userId.Value || c.User2Id == userId.Value));
            if (conversation == null) return NotFound();

            
            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.CreatedAt,
                    m.IsRead,
                    m.SenderId,
                    SenderUsername = m.Sender.Username,
                    SenderProfilePic = m.Sender.ProfilePic
                })
                .ToListAsync();

            
            await _context.Messages
                .Where(m => m.ConversationId == conversationId &&
                            m.SenderId != userId.Value &&
                            !m.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));

            return Ok(messages);
        }


        [HttpPost("{conversationId}/messages")]
        public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendMessageDto dto)
        {
            int? userId = User.GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId &&
                    (c.User1Id == userId.Value || c.User2Id == userId.Value));
            if (conversation == null) return NotFound();

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = userId.Value,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message.Id,
                message.Content,
                message.CreatedAt,
                message.SenderId,
                message.IsRead
            });
        }

    }

}
