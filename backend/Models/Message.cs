namespace backend.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; }

        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;

        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
    }
}
