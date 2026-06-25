namespace backend.Models
{
    public class Conversation
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public int User1Id { get; set; }
        public User User1 { get; set; } = null!;

        public int User2Id { get; set; }
        public User User2 { get; set; } = null!;

        public ICollection<Message> Messages { get; set; }
    }
}
