using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Google.Apis.Auth; // needed for GoogleJsonWebSignature.Payload

namespace backend.Tests.Services
{
    public class AuthServiceTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static Mock<ITokenService> CreateTokenServiceMock()
        {
            var mock = new Mock<ITokenService>();
            mock.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("fake-access-token");
            mock.Setup(t => t.GenerateRefreshToken()).Returns("fake-raw-refresh-token");
            mock.Setup(t => t.HashToken(It.IsAny<string>())).Returns<string>(s => $"hashed-{s}");
            return mock;
        }

        private static ILogger<AuthService> CreateLogger() => Mock.Of<ILogger<AuthService>>();



        [Fact]
        public async Task RegisterAsync_NewUser_ReturnsSuccess()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.RegisterAsync(new RegisterDto
            {
                Username = "diar",
                Email = "diar@test.com",
                Password = "Password123!"
            });

            Assert.Equal(RegisterResult.Success, result);
            Assert.Single(context.Users);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ReturnsUserAlreadyExists()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Username = "existing", Email = "diar@test.com", PasswordHash = "x" });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.RegisterAsync(new RegisterDto
            {
                Username = "newname",
                Email = "diar@test.com", // same email, different case would also match via ToLower()
                Password = "Password123!"
            });

            Assert.Equal(RegisterResult.UserAlreadyExists, result);
            Assert.Single(context.Users); // no second user created
        }

        [Fact]
        public async Task RegisterAsync_DuplicateUsername_ReturnsUserAlreadyExists()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Username = "diar", Email = "other@test.com", PasswordHash = "x" });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.RegisterAsync(new RegisterDto
            {
                Username = "diar", // same username, different email
                Email = "new@test.com",
                Password = "Password123!"
            });

            Assert.Equal(RegisterResult.UserAlreadyExists, result);
        }

        [Fact]
        public async Task RegisterAsync_HashesPassword_NotStoredAsPlainText()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            await service.RegisterAsync(new RegisterDto
            {
                Username = "diar",
                Email = "diar@test.com",
                Password = "Password123!"
            });

            var user = await context.Users.SingleAsync();
            Assert.NotEqual("Password123!", user.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", user.PasswordHash));
        }

        // ---------- LoginAsync ----------

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithTokens()
        {
            using var context = CreateContext();
            context.Users.Add(new User
            {
                Id = 1,
                Username = "diar",
                Email = "diar@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.LoginAsync(new LoginDto { Email = "diar@test.com", Password = "Password123!" });

            Assert.True(result.Success);
            Assert.Equal("fake-access-token", result.AccessToken);
            Assert.Equal("fake-raw-refresh-token", result.RawRefreshToken);
            Assert.Single(context.RefreshTokens);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsFailure()
        {
            using var context = CreateContext();
            context.Users.Add(new User
            {
                Id = 1,
                Username = "diar",
                Email = "diar@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.LoginAsync(new LoginDto { Email = "diar@test.com", Password = "WrongPassword" });

            Assert.False(result.Success);
            Assert.Empty(context.RefreshTokens); // no token issued on failed login
        }

        [Fact]
        public async Task LoginAsync_EmailNotFound_ReturnsFailure()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.LoginAsync(new LoginDto { Email = "nobody@test.com", Password = "Whatever123!" });

            Assert.False(result.Success);
        }

        [Fact]
        public async Task LoginAsync_EmailIsCaseInsensitive()
        {
            using var context = CreateContext();
            context.Users.Add(new User
            {
                Id = 1,
                Username = "diar",
                Email = "diar@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.LoginAsync(new LoginDto { Email = "DIAR@TEST.COM", Password = "Password123!" });

            Assert.True(result.Success);
        }

        // ---------- RefreshAsync ----------

        [Fact]
        public async Task RefreshAsync_NoCookie_ReturnsFailure()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.RefreshAsync(null);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task RefreshAsync_UnknownToken_ReturnsFailure()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.RefreshAsync("some-raw-token-that-was-never-issued");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task RefreshAsync_ExpiredToken_ReturnsFailure()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "diar", Email = "diar@test.com", PasswordHash = "x" });
            context.RefreshTokens.Add(new RefreshToken
            {
                UserId = 1,
                TokenHash = "hashed-expired-token",
                ExpiresAt = DateTime.UtcNow.AddDays(-1) // already expired
            });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.RefreshAsync("expired-token");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task RefreshAsync_ValidToken_ReturnsNewTokensAndRotatesOldOne()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "diar", Email = "diar@test.com", PasswordHash = "x" });
            context.RefreshTokens.Add(new RefreshToken
            {
                Id = 99,
                UserId = 1,
                TokenHash = "hashed-valid-token",
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.RefreshAsync("valid-token");

            Assert.True(result.Success);
            Assert.Equal("fake-access-token", result.AccessToken);
            Assert.Equal("fake-raw-refresh-token", result.RawRefreshToken);

            // old token removed, exactly one (new) token remains
            var remaining = await context.RefreshTokens.ToListAsync();
            Assert.Single(remaining);
            Assert.DoesNotContain(remaining, rt => rt.Id == 99);
        }

        [Fact]
        public async Task RefreshAsync_UserDeletedAfterTokenIssued_ReturnsFailure()
        {
            using var context = CreateContext();
            // token exists but no matching user — simulates a deleted account
            context.RefreshTokens.Add(new RefreshToken
            {
                UserId = 999,
                TokenHash = "hashed-orphaned-token",
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var result = await service.RefreshAsync("orphaned-token");

            Assert.False(result.Success);
        }

        // ---------- LogoutAsync ----------

        [Fact]
        public async Task LogoutAsync_ValidToken_RemovesToken()
        {
            using var context = CreateContext();
            context.RefreshTokens.Add(new RefreshToken
            {
                UserId = 1,
                TokenHash = "hashed-token-to-remove",
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            await service.LogoutAsync("token-to-remove");

            Assert.Empty(context.RefreshTokens);
        }

        [Fact]
        public async Task LogoutAsync_NoCookie_DoesNothing()
        {
            using var context = CreateContext();
            context.RefreshTokens.Add(new RefreshToken { UserId = 1, TokenHash = "unrelated", ExpiresAt = DateTime.UtcNow.AddDays(1) });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            await service.LogoutAsync(null);

            Assert.Single(context.RefreshTokens); // untouched
        }

        [Fact]
        public async Task LogoutAsync_UnknownToken_DoesNotThrow()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateTokenServiceMock().Object, CreateLogger());

            var exception = await Record.ExceptionAsync(() => service.LogoutAsync("does-not-exist"));

            Assert.Null(exception);
        }

        // ---------- GoogleLoginAsync ----------

        [Fact]
        public async Task GoogleLoginAsync_InvalidIdToken_ReturnsFailure()
        {
            using var context = CreateContext();
            var tokenMock = CreateTokenServiceMock();
            tokenMock.Setup(t => t.VerifyGoogleIdTokenAsync(It.IsAny<string>())).ReturnsAsync((Google.Apis.Auth.GoogleJsonWebSignature.Payload?)null);

            var service = new AuthService(context, tokenMock.Object, CreateLogger());

            var result = await service.GoogleLoginAsync("bad-id-token");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task GoogleLoginAsync_NewUser_CreatesAccountAndReturnsSuccess()
        {
            using var context = CreateContext();
            var tokenMock = CreateTokenServiceMock();
            tokenMock.Setup(t => t.VerifyGoogleIdTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(new Google.Apis.Auth.GoogleJsonWebSignature.Payload { Email = "newgoogleuser@test.com", Name = "New User", Subject = "google-sub-123", Picture = "http://pic.jpg" });

            var service = new AuthService(context, tokenMock.Object, CreateLogger());

            var result = await service.GoogleLoginAsync("valid-id-token");

            Assert.True(result.Success);
            var user = await context.Users.SingleAsync();
            Assert.Equal("newgoogleuser@test.com", user.Email);
            Assert.Equal("google-sub-123", user.GoogleId);
        }

        [Fact]
        public async Task GoogleLoginAsync_ExistingEmailPasswordAccount_LinksGoogleId()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "diar", Email = "diar@test.com", PasswordHash = "x", GoogleId = null });
            await context.SaveChangesAsync();

            var tokenMock = CreateTokenServiceMock();
            tokenMock.Setup(t => t.VerifyGoogleIdTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(new Google.Apis.Auth.GoogleJsonWebSignature.Payload { Email = "diar@test.com", Name = "Diar", Subject = "google-sub-456", Picture = "http://pic.jpg" });

            var service = new AuthService(context, tokenMock.Object, CreateLogger());

            var result = await service.GoogleLoginAsync("valid-id-token");

            Assert.True(result.Success);
            var user = await context.Users.SingleAsync(); // still just one user, not a duplicate
            Assert.Equal("google-sub-456", user.GoogleId);
        }

        [Fact]
        public async Task GoogleLoginAsync_ExistingGoogleAccount_DoesNotDuplicateOrRelink()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "diar", Email = "diar@test.com", GoogleId = "already-linked-sub" });
            await context.SaveChangesAsync();

            var tokenMock = CreateTokenServiceMock();
            tokenMock.Setup(t => t.VerifyGoogleIdTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(new Google.Apis.Auth.GoogleJsonWebSignature.Payload { Email = "diar@test.com", Name = "Diar", Subject = "already-linked-sub", Picture = "http://pic.jpg" });

            var service = new AuthService(context, tokenMock.Object, CreateLogger());

            var result = await service.GoogleLoginAsync("valid-id-token");

            Assert.True(result.Success);
            Assert.Single(context.Users);
        }

        [Fact]
        public async Task GoogleLoginAsync_DuplicateUsername_GeneratesUniqueUsername()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "newuser", Email = "someoneelse@test.com" });
            await context.SaveChangesAsync();

            var tokenMock = CreateTokenServiceMock();
            tokenMock.Setup(t => t.VerifyGoogleIdTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(new Google.Apis.Auth.GoogleJsonWebSignature.Payload { Email = "brandnew@test.com", Name = "New User", Subject = "google-sub-789", Picture = "http://pic.jpg" });

            var service = new AuthService(context, tokenMock.Object, CreateLogger());

            await service.GoogleLoginAsync("valid-id-token");

            var createdUser = await context.Users.SingleAsync(u => u.Email == "brandnew@test.com");
            Assert.Equal("newuser1", createdUser.Username); // collision resolved by appending 1
        }
    }
}