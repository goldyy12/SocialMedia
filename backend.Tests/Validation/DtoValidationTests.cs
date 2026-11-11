using System.ComponentModel.DataAnnotations;
using backend.DTOs;

namespace backend.Tests.Validation
{
    public class DtoValidationTests
    {
        private static List<ValidationResult> Validate(object dto)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void PostDto_EmptyContentAndNoImage_IsInvalid()
            => Assert.NotEmpty(Validate(new PostDto { Content = "  " }));

        [Fact]
        public void PostDto_ImageOnly_IsValid()
            => Assert.Empty(Validate(new PostDto { Content = "", ImageUrl = "https://res.cloudinary.com/x.png" }));

        [Fact]
        public void PostDto_TooLong_IsInvalid()
            => Assert.NotEmpty(Validate(new PostDto { Content = new string('a', 2001) }));

        [Fact]
        public void CommentDto_Empty_IsInvalid()
            => Assert.NotEmpty(Validate(new CommentDto { Content = "" }));

        [Fact]
        public void SendMessageDto_TooLong_IsInvalid()
            => Assert.NotEmpty(Validate(new SendMessageDto { Content = new string('a', 1001) }));

        [Theory]
        [InlineData("ab")]
        [InlineData("has space")]
        [InlineData("<script>")]
        public void RegisterDto_BadUsername_IsInvalid(string username)
            => Assert.NotEmpty(Validate(new RegisterDto { Username = username, Email = "a@b.com", Password = "Password123" }));

        [Fact]
        public void RegisterDto_ShortPassword_IsInvalid()
            => Assert.NotEmpty(Validate(new RegisterDto { Username = "diar", Email = "a@b.com", Password = "short" }));

        [Fact]
        public void RegisterDto_Valid_IsValid()
            => Assert.Empty(Validate(new RegisterDto { Username = "diar_10", Email = "a@b.com", Password = "Password123" }));
    }
}
