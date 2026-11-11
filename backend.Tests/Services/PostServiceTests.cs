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
        [Fact]
        public async Task DeletePostAsync_PostNotFound_ReturnsFalse()
        {
            using var context = CreateContext();
            var service = new PostService(context);

            var result = await service.DeletePostAsync(postId: 1, userId: 1);

            Assert.Null(result);
        }
        [Fact]
        public async Task UpdatePostAsync_ValidData_UpdatesPost()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "user1", CreatedAt = DateTime.UtcNow });
            context.Posts.Add(new Post { Id = 1, UserId = 1, Content = "Old Content", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new PostService(context);
            var (result, updatedPost) = await service.UpdatePostAsync(postId: 1, userId: 1, new DTOs.PostDto { Content = "New Content" });

            Assert.Equal(UpdatePostResult.Success, result);
            Assert.NotNull(updatedPost);
            Assert.Equal("New Content", updatedPost.Content);
        }

    
        [Fact]
        public async Task GetFeedAsync_Paginates_NewestFirst_WithoutOverlap()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "user1", CreatedAt = DateTime.UtcNow });
            var start = DateTime.UtcNow.AddHours(-1);
            for (int i = 1; i <= 5; i++)
                context.Posts.Add(new Post { Id = i, UserId = 1, Content = $"Post {i}", CreatedAt = start.AddMinutes(i) });
            await context.SaveChangesAsync();

            var service = new PostService(context);
            var page1 = await service.GetFeedAsync(currentUserId: 1, beforeId: null, limit: 2);
            var page2 = await service.GetFeedAsync(currentUserId: 1, beforeId: page1[^1].Id, limit: 2);
            var page3 = await service.GetFeedAsync(currentUserId: 1, beforeId: page2[^1].Id, limit: 2);

            Assert.Equal(new[] { 5, 4 }, page1.Select(p => p.Id));
            Assert.Equal(new[] { 3, 2 }, page2.Select(p => p.Id));
            Assert.Equal(new[] { 1 }, page3.Select(p => p.Id));
        }

        [Fact]
        public async Task GetPostsByUserAsync_LimitIsClampedToMaxPageSize()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "user1", CreatedAt = DateTime.UtcNow });
            for (int i = 1; i <= PostService.MaxPageSize + 5; i++)
                context.Posts.Add(new Post { Id = i, UserId = 1, Content = "x", CreatedAt = DateTime.UtcNow.AddSeconds(i) });
            await context.SaveChangesAsync();

            var service = new PostService(context);
            var result = await service.GetPostsByUserAsync(profileUserId: 1, currentUserId: 1, beforeId: null, limit: 10_000);

            Assert.Equal(PostService.MaxPageSize, result.Count);
        }

        [Fact]
        public async Task UpdatePostAsync_TextOnlyEdit_KeepsExistingImage()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "user1", CreatedAt = DateTime.UtcNow });
            context.Posts.Add(new Post { Id = 1, UserId = 1, Content = "Old", ImageUrl = "https://img/1.png", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new PostService(context);
            var (result, updated) = await service.UpdatePostAsync(postId: 1, userId: 1, new DTOs.PostDto { Content = "New" });

            Assert.Equal(UpdatePostResult.Success, result);
            Assert.Equal("https://img/1.png", updated!.ImageUrl);
        }
    }
}
