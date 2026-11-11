using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class PostDto : IValidatableObject
    {
        [MaxLength(2000, ErrorMessage = "Post is too long (max 2000 characters)")]
        public string Content { get; set; } = string.Empty;

        [Url]
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Content) && string.IsNullOrWhiteSpace(ImageUrl))
            {
                yield return new ValidationResult(
                    "A post needs text or an image",
                    new[] { nameof(Content), nameof(ImageUrl) });
            }
        }
    }
}
