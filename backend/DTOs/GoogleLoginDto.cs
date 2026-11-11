using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class GoogleLoginDto
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }
}
