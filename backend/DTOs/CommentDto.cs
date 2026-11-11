using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class CommentDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Comment can't be empty")]
        [MaxLength(200, ErrorMessage = "Comment is too long (max 200 characters)")]
        public string Content { get; set; } = string.Empty;
    }
}
