
using backend.DTOs;

namespace backend.Interfaces
{
    public interface IConversationService
    {
        Task<int> StartConversationAsync(int userId, int userId2);
        Task<List<ConversationSummaryDto>> GetConversationsAsync(int userId);
        Task<(GetMessagesResult Result, List<MessageResponseDto>? Messages)> GetMessagesAsync(int conversationId, int userId);
        Task<(SendMessageResult Result, MessageResponseDto? Message)> SendMessageAsync(int conversationId, int userId, SendMessageDto dto);
    }
}