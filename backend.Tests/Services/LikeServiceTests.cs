using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using backend.Interfaces;

namespace backend.Tests.Services
{
    public class LikeServiceTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // ---------- LikePostAsync ----------

        [Fact]
        public async Task LikePostAsync_PostNotFound_ReturnsPostNotFound()
        {
            using var context = CreateContext();
            var service = new LikeService(context);

            var result = await service.LikePostAsync(postId: 1, userId: 1);

            Assert.Equal(LikeResult.PostNotFound, result);
        }

        [Fact]
        public async Task LikePostAsync_ValidData_ReturnsSuccessAndCreatesLike()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "poster", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "liker", CreatedAt = DateTime.UtcNow });
            context.Posts.Add(new Post { Id = 1, UserId = 1, Content = "Test Post", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new LikeService(context);
            var result = await service.LikePostAsync(postId: 1, userId: 2);

            Assert.Equal(LikeResult.Success, result);
            var like = await context.Likes.SingleAsync();
            Assert.Equal(2, like.UserId);
            Assert.Equal(1, like.PostId);
        }

        [Fact]
        public async Task LikePostAsync_AlreadyLiked_ReturnsAlreadyLiked()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "poster", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "liker", CreatedAt = DateTime.UtcNow });
            context.Posts.Add(new Post { Id = 1, UserId = 1, Content = "Test Post", CreatedAt = DateTime.UtcNow });
            context.Likes.Add(new Like { UserId = 2, PostId = 1, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new LikeService(context);
            var result = await service.LikePostAsync(postId: 1, userId: 2);

            Assert.Equal(LikeResult.AlreadyLiked, result);
            Assert.Single(context.Likes); // no duplicate like created
        }

        [Fact]
        public async Task LikePostAsync_LikingOwnPost_CreatesNoNotification()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "poster", CreatedAt = DateTime.UtcNow });
            context.Posts.Add(new Post { Id = 1, UserId = 1, Content = "Test Post", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new LikeService(context);
            var result = await service.LikePostAsync(postId: 1, userId: 1); // liking own post

            Assert.Equal(LikeResult.Success, result);
            Assert.Empty(context.Notifications); // no self-notification
        }

        [Fact]
        public async Task LikePostAsync_LikingSomeoneElsesPost_CreatesNotification()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "poster", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "liker", CreatedAt = DateTime.UtcNow });
            context.Posts.Add(new Post { Id = 1, UserId = 1, Content = "Test Post", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new LikeService(context);
            var result = await service.LikePostAsync(postId: 1, userId: 2);

            Assert.Equal(LikeResult.Success, result);
            var notification = await context.Notifications.SingleAsync();
            Assert.Equal(1, notification.UserId); // notified: the post owner
            Assert.Equal("like", notification.Type);
            Assert.Contains("liker", notification.Message);
        }

        // ---------- UnlikePostAsync ----------

        [Fact]
        public async Task UnlikePostAsync_LikeNotFound_ReturnsLikeNotFound()
        {
            using var context = CreateContext();
            var service = new LikeService(context);

            var result = await service.UnlikePostAsync(postId: 1, userId: 1);

            Assert.Equal(UnlikeResult.LikeNotFound, result);
        }

        [Fact]
        public async Task UnlikePostAsync_ValidData_ReturnsSuccessAndRemovesLike()
        {
            using var context = CreateContext();
            context.Likes.Add(new Like { Id = 1, UserId = 1, PostId = 1, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new LikeService(context);
            var result = await service.UnlikePostAsync(postId: 1, userId: 1);

            Assert.Equal(UnlikeResult.Success, result);
            Assert.Empty(context.Likes);
        }

        [Fact]
        public async Task UnlikePostAsync_DifferentUsersLike_DoesNotRemoveIt()
        {
            using var context = CreateContext();
            context.Likes.Add(new Like { Id = 1, UserId = 2, PostId = 1, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new LikeService(context);
            // user 1 tries to unlike a post they never liked (like belongs to user 2)
            var result = await service.UnlikePostAsync(postId: 1, userId: 1);

            Assert.Equal(UnlikeResult.LikeNotFound, result);
            Assert.Single(context.Likes); // user 2's like untouched
        }
    }
}