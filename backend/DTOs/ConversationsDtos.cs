// DTOs/ConversationDtos.cs
namespace backend.DTOs
{
    public class ConversationUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfilePic { get; set; }
    }

    public class LastMessageDto
    {
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int SenderId { get; set; }
    }

    public class ConversationSummaryDto
    {
        public int Id { get; set; }
        public ConversationUserDto OtherUser { get; set; } = null!;
        public LastMessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }

    public class MessageResponseDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int SenderId { get; set; }
        public string? SenderUsername { get; set; }
        public string? SenderProfilePic { get; set; }
    }
}