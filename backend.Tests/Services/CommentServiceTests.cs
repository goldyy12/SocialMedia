using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using backend.Interfaces;

namespace backend.Tests.Services
{
    public class CommentServiceTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // fresh DB per test
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetCommentsForPostAsync_PostNotFound_ReturnsNull()
        {
            using var context = CreateContext();
            var service = new CommentService(context);

            var result = await service.GetCommentsForPostAsync(postId: 1);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddCommentAsync_PostNotFound_ReturnsNull()
        {
            using var context = CreateContext();
            var service = new CommentService(context);

            var result = await service.AddCommentAsync(postId: 1, userId: 1, new DTOs.CommentDto { Content = "Test" });

            Assert.Null(result);
        }
        [Fact]
        public async Task GetCommentsForPostAsync_ReturnsComments()
        {
            using var context = CreateContext();
            var author = new User { Id = 2, Username = "testuser", CreatedAt = DateTime.UtcNow };
            var post = new Post { Id = 1, UserId = 1, Content = "Test Post", CreatedAt = DateTime.UtcNow };
            context.Posts.Add(post);
            context.Comments.Add(new Comment { Id = 1, PostId = 1, UserId = 2, Content = "Test Comment", CreatedAt = DateTime.UtcNow, User = author });
            await context.SaveChangesAsync();

            var service = new CommentService(context);
            var result = await service.GetCommentsForPostAsync(postId: 1);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Test Comment", result[0].Content);
        }
        [Fact]
        public async Task AddCommentAsync_ValidData_ReturnsComment()
        {
            using var context = CreateContext();
            var post = new Post { Id = 1, UserId = 1, Content = "Test Post", CreatedAt = DateTime.UtcNow };
            var user = new User { Id = 2, Username = "testuser", CreatedAt = DateTime.UtcNow };
            context.Posts.Add(post);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = new CommentService(context);
            var result = await service.AddCommentAsync(postId: 1, userId: 2, new DTOs.CommentDto { Content = "Test Comment" });

            Assert.NotNull(result);
            Assert.Equal("Test Comment", result.Content);
        }
        [Fact]
        public async Task DeleteCommentAsync_ValidData_DeletesComment()
        {
            using var context = CreateContext();
            var comment = new Comment { Id = 1, PostId = 1, UserId = 2, Content = "Test Comment", CreatedAt = DateTime.UtcNow };
            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            var service = new CommentService(context);
            var result = await service.DeleteCommentAsync(commentId: 1, userId: 2);

            Assert.True(result);
            Assert.Empty(context.Comments);
        }
        [Fact]
        public async Task DeleteCommentAsync_NotFound_ReturnsNull()
        {
            using var context = CreateContext();
            var service = new CommentService(context);

            var result = await service.DeleteCommentAsync(commentId: 1, userId: 2);

            Assert.Null(result);
        }
        [Fact]
        public async Task EditCommentAsync_ValidData_EditsComment()
        {
            using var context = CreateContext();
            var user = new User { Id = 2, Username = "testuser", CreatedAt = DateTime.UtcNow };
            var comment = new Comment { Id = 1, PostId = 1, UserId = 2, Content = "Test Comment", CreatedAt = DateTime.UtcNow, User = user };
            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            var service = new CommentService(context);
            var (result, updatedComment) = await service.EditCommentAsync(commentId: 1, userId: 2, new DTOs.CommentDto { Content = "Edited Comment" });

            Assert.Equal(EditCommentsResults.Success, result);
            Assert.NotNull(updatedComment);
            Assert.Equal("Edited Comment", updatedComment.Content);
        }

        [Fact]
        public async Task EditCommentAsync_NotFound_ReturnsNotFound()
        {
            using var context = CreateContext();
            var service = new CommentService(context);

            var (result, comment) = await service.EditCommentAsync(commentId: 1, userId: 2, new DTOs.CommentDto { Content = "Edited Comment" });

            Assert.Equal(EditCommentsResults.NotFound, result);
            Assert.Null(comment);
        }

        [Fact]
        public async Task EditCommentAsync_WrongUser_ReturnsForbidden()
        {
            using var context = CreateContext();
            var user = new User { Id = 2, Username = "testuser", CreatedAt = DateTime.UtcNow };
            var comment = new Comment { Id = 1, PostId = 1, UserId = 2, Content = "Test Comment", CreatedAt = DateTime.UtcNow, User = user };
            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            var service = new CommentService(context);
            var (result, updatedComment) = await service.EditCommentAsync(commentId: 1, userId: 3, new DTOs.CommentDto { Content = "Edited Comment" });

            Assert.Equal(EditCommentsResults.Forbidden, result);
            Assert.Null(updatedComment);
        }

    }
}