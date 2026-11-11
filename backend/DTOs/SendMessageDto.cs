using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class SendMessageDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Message can't be empty")]
        [MaxLength(1000, ErrorMessage = "Message is too long (max 1000 characters)")]
        public string Content { get; set; } = string.Empty;
    }
}
