using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Services
{
    public class NotificationServiceTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // ---------- GetUnreadNotificationsAsync ----------

        [Fact]
        public async Task GetUnreadNotificationsAsync_NoNotifications_ReturnsEmptyList()
        {
            using var context = CreateContext();
            var service = new NotificationService(context);

            var result = await service.GetUnreadNotificationsAsync(userId: 1);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUnreadNotificationsAsync_ReturnsOnlyUnreadForThatUser()
        {
            using var context = CreateContext();
            context.Notifications.Add(new Notification { Id = 1, UserId = 1, Type = "like", Message = "unread for user 1", IsRead = false, CreatedAt = DateTime.UtcNow });
            context.Notifications.Add(new Notification { Id = 2, UserId = 1, Type = "comment", Message = "already read", IsRead = true, CreatedAt = DateTime.UtcNow });
            context.Notifications.Add(new Notification { Id = 3, UserId = 2, Type = "like", Message = "unread for user 2", IsRead = false, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new NotificationService(context);
            var result = await service.GetUnreadNotificationsAsync(userId: 1);

            Assert.Single(result);
            Assert.Equal("unread for user 1", result[0].Message);
        }

        [Fact]
        public async Task GetUnreadNotificationsAsync_OrdersByCreatedAtDescending()
        {
            using var context = CreateContext();
            var older = DateTime.UtcNow.AddMinutes(-10);
            var newer = DateTime.UtcNow;
            context.Notifications.Add(new Notification { Id = 1, UserId = 1, Type = "like", Message = "older", IsRead = false, CreatedAt = older });
            context.Notifications.Add(new Notification { Id = 2, UserId = 1, Type = "comment", Message = "newer", IsRead = false, CreatedAt = newer });
            await context.SaveChangesAsync();

            var service = new NotificationService(context);
            var result = await service.GetUnreadNotificationsAsync(userId: 1);

            Assert.Equal(2, result.Count);
            Assert.Equal("newer", result[0].Message); // most recent first
            Assert.Equal("older", result[1].Message);
        }

        // ---------- GetUnreadCountAsync ----------

        [Fact]
        public async Task GetUnreadCountAsync_NoNotifications_ReturnsZero()
        {
            using var context = CreateContext();
            var service = new NotificationService(context);

            var result = await service.GetUnreadCountAsync(userId: 1);

            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetUnreadCountAsync_CountsOnlyUnreadForThatUser()
        {
            using var context = CreateContext();
            context.Notifications.Add(new Notification { Id = 1, UserId = 1, Type = "like", Message = "a", IsRead = false, CreatedAt = DateTime.UtcNow });
            context.Notifications.Add(new Notification { Id = 2, UserId = 1, Type = "like", Message = "b", IsRead = false, CreatedAt = DateTime.UtcNow });
            context.Notifications.Add(new Notification { Id = 3, UserId = 1, Type = "like", Message = "c", IsRead = true, CreatedAt = DateTime.UtcNow });
            context.Notifications.Add(new Notification { Id = 4, UserId = 2, Type = "like", Message = "d", IsRead = false, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new NotificationService(context);
            var result = await service.GetUnreadCountAsync(userId: 1);

            Assert.Equal(2, result); // only user 1's unread ones
        }
    }
}