// DTOs/FollowDtos.cs
namespace backend.DTOs
{
    public class FollowRequestDto
    {
        public int Id { get; set; }
        public int FollowerId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfilePic { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FollowerDto
    {
        public int FollowerId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfilePic { get; set; }
    }

    public class FollowingDto
    {
        public int FollowingId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfilePic { get; set; }
    }

    public class AvailableFriendDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfilePic { get; set; }
        public string? Bio { get; set; }
        public bool FollowsYou { get; set; }
        public bool IsPending { get; set; }
    }
}