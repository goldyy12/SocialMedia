using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Services
{
    public class UserServiceTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // ---------- GetAllUsersAsync ----------

        [Fact]
        public async Task GetAllUsersAsync_ExcludesCurrentUser()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "me", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "other", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new UserService(context);
            var result = await service.GetAllUsersAsync(currentUserId: 1);

            Assert.Single(result);
            Assert.Equal("other", result[0].Username);
        }

        [Fact]
        public async Task GetAllUsersAsync_NoOtherUsers_ReturnsEmptyList()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "me", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new UserService(context);
            var result = await service.GetAllUsersAsync(currentUserId: 1);

            Assert.Empty(result);
        }

        // ---------- SearchUsersAsync ----------

        [Fact]
        public async Task SearchUsersAsync_EmptyQuery_ReturnsEmptyList()
        {
            using var context = CreateContext();
            var service = new UserService(context);

            var result = await service.SearchUsersAsync(query: "", currentUserId: 1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchUsersAsync_WhitespaceQuery_ReturnsEmptyList()
        {
            using var context = CreateContext();
            var service = new UserService(context);

            var result = await service.SearchUsersAsync(query: "   ", currentUserId: 1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchUsersAsync_MatchesPartialUsernameCaseInsensitive()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "me", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "DiarGoldy", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 3, Username = "someoneelse", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new UserService(context);
            var result = await service.SearchUsersAsync(query: "diar", currentUserId: 1);

            Assert.Single(result);
            Assert.Equal("DiarGoldy", result[0].Username);
        }

        [Fact]
        public async Task SearchUsersAsync_ExcludesCurrentUser()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "diarme", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "diarother", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new UserService(context);
            var result = await service.SearchUsersAsync(query: "diar", currentUserId: 1);

            Assert.Single(result);
            Assert.Equal("diarother", result[0].Username);
        }

        [Fact]
        public async Task SearchUsersAsync_ResultsCappedAtTen()
        {
            using var context = CreateContext();
            for (int i = 1; i <= 15; i++)
            {
                context.Users.Add(new User { Id = i, Username = $"testuser{i}", CreatedAt = DateTime.UtcNow });
            }
            await context.SaveChangesAsync();

            var service = new UserService(context);
            var result = await service.SearchUsersAsync(query: "testuser", currentUserId: 999);

            Assert.Equal(10, result.Count);
        }

        // ---------- GetUserByIdAsync ----------

        [Fact]
        public async Task GetUserByIdAsync_UserNotFound_ReturnsNull()
        {
            using var context = CreateContext();
            var service = new UserService(context);

            var result = await service.GetUserByIdAsync(id: 1, currentUserId: 2);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsCorrectFollowerAndFollowingCounts()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "target", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "follower1", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 3, Username = "follower2", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 4, Username = "followedByTarget", CreatedAt = DateTime.UtcNow });

            context.Follows.Add(new Follow { FollowerId = 2, FollowingId = 1, Status = "accepted", CreatedAt = DateTime.UtcNow });
            context.Follows.Add(new Follow { FollowerId = 3, FollowingId = 1, Status = "accepted", CreatedAt = DateTime.UtcNow });
            context.Follows.Add(new Follow { FollowerId = 999, FollowingId = 1, Status = "pending", CreatedAt = DateTime.UtcNow });
            context.Follows.Add(new Follow { FollowerId = 1, FollowingId = 4, Status = "accepted", CreatedAt = DateTime.UtcNow });

            await context.SaveChangesAsync();

            var service = new UserService(context);
            var result = await service.GetUserByIdAsync(id: 1, currentUserId: 999);

            Assert.NotNull(result);
            Assert.Equal(2, result.FollowersCount);
            Assert.Equal(1, result.FollowingCount);
        }

        [Fact]
        public async Task GetUserByIdAsync_CurrentUserFollowsTarget_IsFollowingReflectsStatus()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "target", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "viewer", CreatedAt = DateTime.UtcNow });
            context.Follows.Add(new Follow { FollowerId = 2, FollowingId = 1, Status = "accepted", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new UserService(context);
            var result = await service.GetUserByIdAsync(id: 1, currentUserId: 2);

            Assert.NotNull(result);
            Assert.Equal("accepted", result.IsFollowing);
        }

        [Fact]
        public async Task GetUserByIdAsync_CurrentUserDoesNotFollowTarget_IsFollowingIsNull()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "target", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "viewer", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new UserService(context);
            var result = await service.GetUserByIdAsync(id: 1, currentUserId: 2);

            Assert.NotNull(result);
            Assert.Null(result.IsFollowing);
        }

        // ---------- UpdateProfileAsync ----------

        [Fact]
        public async Task UpdateProfileAsync_UserNotFound_ReturnsNull()
        {
            using var context = CreateContext();
            var service = new UserService(context);

            var result = await service.UpdateProfileAsync(userId: 1, new UpdateProfileDto { Bio = "New bio" });

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateProfileAsync_ValidData_UpdatesBioAndProfilePic()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "diar", Bio = "old bio", ProfilePic = "old.jpg", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new UserService(context);
            var result = await service.UpdateProfileAsync(userId: 1, new UpdateProfileDto { Bio = "new bio", ProfilePic = "new.jpg" });

            Assert.NotNull(result);
            Assert.Equal("new bio", result.Bio);
            Assert.Equal("new.jpg", result.ProfilePic);
        }

        [Fact]
        public async Task UpdateProfileAsync_OnlyBioProvided_PreservesExistingProfilePic()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "diar", Bio = "old bio", ProfilePic = "old.jpg", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new UserService(context);
            var result = await service.UpdateProfileAsync(userId: 1, new UpdateProfileDto { Bio = "new bio" });

            Assert.NotNull(result);
            Assert.Equal("new bio", result.Bio);
            Assert.Equal("old.jpg", result.ProfilePic);
        }

        [Fact]
        public async Task UpdateProfileAsync_OnlyProfilePicProvided_PreservesExistingBio()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "diar", Bio = "old bio", ProfilePic = "old.jpg", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new UserService(context);
            var result = await service.UpdateProfileAsync(userId: 1, new UpdateProfileDto { ProfilePic = "new.jpg" });

            Assert.NotNull(result);
            Assert.Equal("old bio", result.Bio);
            Assert.Equal("new.jpg", result.ProfilePic);
        }
    }
}