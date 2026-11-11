using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class UpdateProfileDto
    {
        [MaxLength(300, ErrorMessage = "Bio is too long (max 300 characters)")]
        public string? Bio { get; set; }

        [MaxLength(500)]
        public string? ProfilePic { get; set; }
    }
}
