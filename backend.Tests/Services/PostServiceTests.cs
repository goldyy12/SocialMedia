using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using backend.Interfaces;

namespace backend.Tests.Services
{
    public class PostServiceTests
    {


        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // fresh DB per test
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreatePostAsync_UserNotFound_ReturnsNull()
        {
            using var context = CreateContext();
            var service = new PostService(context);

            var result = await service.CreatePostAsync(userId: 1, new DTOs.PostDto { Content = "Test" });

            Assert.Null(result);
        }
        [Fact]
        public async Task CreatePostAsync_ValidData_ReturnsPost()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "testuser", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new PostService(context);
            var result = await service.CreatePostAsync(userId: 1, new DTOs.PostDto { Content = "Test" });

            Assert.NotNull(result);
            Assert.Equal("Test", result.Content);
        }
        [Fact]
        public async Task GetFeedAsync_ReturnsPosts()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "user1", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "user2", CreatedAt = DateTime.UtcNow });
            context.Posts.Add(new Post { Id = 1, UserId = 1, Content = "Post by user1", CreatedAt = DateTime.UtcNow });
            context.Posts.Add(new Post { Id = 2, UserId = 2, Content = "Post by user2", CreatedAt = DateTime.UtcNow });
            context.Follows.Add(new Follow { FollowerId = 1, FollowingId = 2, Status = "accepted", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new PostService(context);
            var result = await service.GetFeedAsync(currentUserId: 1);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // user1 sees their own post and user2's post
        }
        [Fact]
        public async Task GetFeedAsync_NoPosts_ReturnsEmptyList()
        {
            using var context = CreateContext();
            var service = new PostService(context);

            var result = await service.GetFeedAsync(currentUserId: 1);

            Assert.NotNull(result);
            Assert.Empty(result);
        }
        [Fact]
        public async Task GetFeedAsync_OnlyOwnPosts_ReturnsOwnPosts()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "user1", CreatedAt = DateTime.UtcNow });
            context.Posts.Add(new Post { Id = 1, UserId = 1, Content = "Post by user1", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new PostService(context);
            var result = await service.GetFeedAsync(currentUserId: 1);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Post by user1", result[0].Content);
        }
        [Fact]
        public async Task DeletePostAsync_ValidData_DeletesPost()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "user1", CreatedAt = DateTime.UtcNow });
            context.Posts.Add(new Post { Id = 1, UserId = 1, Content = "Post by user1", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new PostService(context);
            var result = await service.DeletePostAsync(postId: 1, userId: 1);

            Assert.True(result);
            Assert.Empty(context.Posts);
        }
        [Fact]
        public async Task DeletePostAsync_WrongUser_ReturnsFalse()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "user1", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "user2", CreatedAt = DateTime.UtcNow });
            context.Posts.Add(new Post { Id = 1, UserId = 1, Content = "Post by user1", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new PostService(context);
            var result = await service.DeletePostAsync(postId: 1, userId: 2);

            Assert.False(result);
            Assert.NotEmpty(context.Posts);
        }

    }
}
