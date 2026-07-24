
using backend.Data;
using backend.DTOs;
using backend.Hubs;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ConversationService : IConversationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ConversationService(AppDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<int> StartConversationAsync(int userId, int userId2)
        {
            var existing = await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.User1Id == userId && c.User2Id == userId2) ||
                    (c.User1Id == userId2 && c.User2Id == userId));

            if (existing != null) return existing.Id;

            var conversation = new Conversation
            {
                User1Id = userId,
                User2Id = userId2,
                CreatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return conversation.Id;
        }

        public async Task<List<ConversationSummaryDto>> GetConversationsAsync(int userId)
        {
            return await _context.Conversations
                .AsNoTracking()
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Select(c => new ConversationSummaryDto
                {
                    Id = c.Id,
                    OtherUser = c.User1Id == userId
                        ? new ConversationUserDto { Id = c.User2.Id, Username = c.User2.Username, ProfilePic = c.User2.ProfilePic }
                        : new ConversationUserDto { Id = c.User1.Id, Username = c.User1.Username, ProfilePic = c.User1.ProfilePic },
                    LastMessage = c.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => new LastMessageDto { Content = m.Content, CreatedAt = m.CreatedAt, SenderId = m.SenderId })
                        .FirstOrDefault(),
                    UnreadCount = c.Messages.Count(m => m.SenderId != userId && !m.IsRead)
                })
                .OrderByDescending(c => c.LastMessage!.CreatedAt)
                .ToListAsync();
        }

        public async Task<(GetMessagesResult, List<MessageResponseDto>?)> GetMessagesAsync(int conversationId, int userId)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId &&
                    (c.User1Id == userId || c.User2Id == userId));
            if (conversation == null) return (GetMessagesResult.ConversationNotFound, null);

            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageResponseDto
                {
                    Id = m.Id,
                    ConversationId = conversationId,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    IsRead = m.IsRead,
                    SenderId = m.SenderId,
                    SenderUsername = m.Sender.Username,
                    SenderProfilePic = m.Sender.ProfilePic
                })
                .ToListAsync();

            await _context.Messages
                .Where(m => m.ConversationId == conversationId &&
                            m.SenderId != userId &&
                            !m.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));

            return (GetMessagesResult.Success, messages);
        }

        public async Task<(SendMessageResult, MessageResponseDto?)> SendMessageAsync(int conversationId, int userId, SendMessageDto dto)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId &&
                    (c.User1Id == userId || c.User2Id == userId));
            if (conversation == null) return (SendMessageResult.ConversationNotFound, null);

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var senderInfo = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Username, u.ProfilePic })
                .FirstOrDefaultAsync();

            var payload = new MessageResponseDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead,
                SenderId = message.SenderId,
                SenderUsername = senderInfo?.Username,
                SenderProfilePic = senderInfo?.ProfilePic
            };

            int recipientId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;

            await _hubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveMessage", payload);
            await _hubContext.Clients.Group(recipientId.ToString()).SendAsync("ReceiveMessage", payload);

            return (SendMessageResult.Success, payload);
        }
    }
}