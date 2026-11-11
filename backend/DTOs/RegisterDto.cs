using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class RegisterDto
    {
        [Required]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
        [MaxLength(30, ErrorMessage = "Username must be at most 30 characters")]
        [RegularExpression("^[a-zA-Z0-9_.]+$", ErrorMessage = "Username may only contain letters, numbers, '_' and '.'")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        // Matches the frontend signup schema (min 8).
        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [MaxLength(128, ErrorMessage = "Password must be at most 128 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
