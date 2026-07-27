using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using backend.Interfaces;

namespace backend.Tests.Services
{
    public class FollowServiceTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // fresh DB per test
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task SendFollowRequestAsync_CannotFollowSelf()
        {
            using var context = CreateContext();
            var service = new FollowService(context);

            var result = await service.SendFollowRequestAsync(userId: 1, targetUserId: 1);

            Assert.Equal(SendFollowResult.CannotFollowSelf, result);
        }

        [Fact]
        public async Task SendFollowRequestAsync_AlreadySent_ReturnsAlreadySent()
        {
            using var context = CreateContext();
            context.Follows.Add(new Follow
            {
                FollowerId = 1,
                FollowingId = 2,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new FollowService(context);
            var result = await service.SendFollowRequestAsync(userId: 1, targetUserId: 2);

            Assert.Equal(SendFollowResult.AlreadySent, result);
        }

        [Fact]
        public async Task SendFollowRequestAsync_NewRequest_ReturnsSuccessAndPersists()
        {
            using var context = CreateContext();
            var service = new FollowService(context);

            var result = await service.SendFollowRequestAsync(userId: 1, targetUserId: 2);

            Assert.Equal(SendFollowResult.Success, result);

            var follow = await context.Follows.SingleAsync();
            Assert.Equal(1, follow.FollowerId);
            Assert.Equal(2, follow.FollowingId);
            Assert.Equal("pending", follow.Status);
        }

        [Fact]
        public async Task AcceptFollowRequestAsync_NotFound_ReturnsNotFound()
        {
            using var context = CreateContext();
            var service = new FollowService(context);

            var result = await service.AcceptFollowRequestAsync(userId: 1, followerId: 2);

            Assert.Equal(AcceptFollowResult.NotFound, result);
        }

        [Fact]
        public async Task AcceptFollowRequestAsync_AlreadyAccepted_ReturnsAlreadyAccepted()
        {
            using var context = CreateContext();
            context.Follows.Add(new Follow
            {
                FollowerId = 2,
                FollowingId = 1,
                Status = "accepted",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new FollowService(context);
            var result = await service.AcceptFollowRequestAsync(userId: 1, followerId: 2);

            Assert.Equal(AcceptFollowResult.AlreadyAccepted, result);
        }

        [Fact]
        public async Task AcceptFollowRequestAsync_PendingRequest_AcceptsAndCreatesNotification()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "diar" });
            context.Follows.Add(new Follow
            {
                FollowerId = 2,
                FollowingId = 1,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new FollowService(context);
            var result = await service.AcceptFollowRequestAsync(userId: 1, followerId: 2);

            Assert.Equal(AcceptFollowResult.Success, result);

            var follow = await context.Follows.SingleAsync();
            Assert.Equal("accepted", follow.Status);

            var notification = await context.Notifications.SingleAsync();
            Assert.Equal(2, notification.UserId);
            Assert.Equal("follow_accepted", notification.Type);
            Assert.Contains("diar", notification.Message);
        }

        [Fact]
        public async Task UnfollowAsync_ExistingFollow_RemovesItAndReturnsSuccess()
        {
            using var context = CreateContext();
            context.Follows.Add(new Follow
            {
                FollowerId = 1,
                FollowingId = 2,
                Status = "accepted",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new FollowService(context);
            var result = await service.UnfollowAsync(userId: 1, targetUserId: 2);

            Assert.Equal(UnfollowResult.Success, result);
            Assert.False(await context.Follows.AnyAsync());
        }

        [Fact]
        public async Task UnfollowAsync_NoExistingFollow_ReturnsNotFound()
        {
            using var context = CreateContext();
            var service = new FollowService(context);

            var result = await service.UnfollowAsync(userId: 1, targetUserId: 2);

            Assert.Equal(UnfollowResult.NotFound, result);
        }
    }
}