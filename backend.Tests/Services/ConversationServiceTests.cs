using backend.Data;
using backend.DTOs;
using backend.Hubs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using backend.Interfaces;
using Xunit;

namespace backend.Tests.Services
{
    public class ConversationServiceTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static Mock<IHubContext<ChatHub>> CreateHubContextMock()
        {
            var mockClientProxy = new Mock<IClientProxy>();
            mockClientProxy
                .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default))
                .Returns(Task.CompletedTask);

            var mockClients = new Mock<IHubClients>();
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

            var mockHubContext = new Mock<IHubContext<ChatHub>>();
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

            return mockHubContext;
        }

        // ---------- StartConversationAsync ----------

        [Fact]
        public async Task StartConversationAsync_NoExisting_CreatesNewConversation()
        {
            using var context = CreateContext();
            var service = new ConversationService(context, CreateHubContextMock().Object);

            var conversationId = await service.StartConversationAsync(userId: 1, userId2: 2);

            Assert.True(conversationId > 0);
            Assert.Single(context.Conversations);
        }

        [Fact]
        public async Task StartConversationAsync_ExistingConversation_ReturnsSameId()
        {
            using var context = CreateContext();
            context.Conversations.Add(new Conversation { Id = 1, User1Id = 1, User2Id = 2, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new ConversationService(context, CreateHubContextMock().Object);
            var conversationId = await service.StartConversationAsync(userId: 1, userId2: 2);

            Assert.Equal(1, conversationId);
            Assert.Single(context.Conversations); // no duplicate created
        }

        [Fact]
        public async Task StartConversationAsync_ExistingConversation_ReversedUserOrder_StillFindsIt()
        {
            using var context = CreateContext();
            context.Conversations.Add(new Conversation { Id = 1, User1Id = 1, User2Id = 2, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new ConversationService(context, CreateHubContextMock().Object);
            // calling with users reversed compared to how the conversation was stored
            var conversationId = await service.StartConversationAsync(userId: 2, userId2: 1);

            Assert.Equal(1, conversationId);
            Assert.Single(context.Conversations);
        }

        // ---------- GetConversationsAsync ----------

        [Fact]
        public async Task GetConversationsAsync_NoConversations_ReturnsEmptyList()
        {
            using var context = CreateContext();
            var service = new ConversationService(context, CreateHubContextMock().Object);

            var result = await service.GetConversationsAsync(userId: 1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetConversationsAsync_ReturnsOtherUserCorrectly_RegardlessOfPosition()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "me", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "otherUser", CreatedAt = DateTime.UtcNow });
            context.Conversations.Add(new Conversation { Id = 1, User1Id = 1, User2Id = 2, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new ConversationService(context, CreateHubContextMock().Object);
            var result = await service.GetConversationsAsync(userId: 1);

            Assert.Single(result);
            Assert.Equal("otherUser", result[0].OtherUser.Username);
        }

        [Fact]
        public async Task GetConversationsAsync_UnreadCount_OnlyCountsMessagesFromOtherUser()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "me", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "otherUser", CreatedAt = DateTime.UtcNow });
            context.Conversations.Add(new Conversation { Id = 1, User1Id = 1, User2Id = 2, CreatedAt = DateTime.UtcNow });
            context.Messages.Add(new Message { Id = 1, ConversationId = 1, SenderId = 2, Content = "unread from other", IsRead = false, CreatedAt = DateTime.UtcNow });
            context.Messages.Add(new Message { Id = 2, ConversationId = 1, SenderId = 1, Content = "sent by me", IsRead = false, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new ConversationService(context, CreateHubContextMock().Object);
            var result = await service.GetConversationsAsync(userId: 1);

            Assert.Equal(1, result[0].UnreadCount); // only the message from user 2 counts
        }

        [Fact]
        public async Task GetConversationsAsync_ConversationWithNoMessages_DoesNotThrow()
        {
            // Regression test for the OrderByDescending(c => c.LastMessage!.CreatedAt) NullReferenceException bug
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "me", CreatedAt = DateTime.UtcNow });
            context.Users.Add(new User { Id = 2, Username = "otherUser", CreatedAt = DateTime.UtcNow });
            context.Conversations.Add(new Conversation { Id = 1, User1Id = 1, User2Id = 2, CreatedAt = DateTime.UtcNow });
            // no messages added at all
            await context.SaveChangesAsync();

            var service = new ConversationService(context, CreateHubContextMock().Object);

            var exception = await Record.ExceptionAsync(() => service.GetConversationsAsync(userId: 1));

            Assert.Null(exception); // fails today against the current code — that's the point
        }

        // ---------- GetMessagesAsync ----------

        [Fact]
        public async Task GetMessagesAsync_ConversationNotFound_ReturnsConversationNotFound()
        {
            using var context = CreateContext();
            var service = new ConversationService(context, CreateHubContextMock().Object);

            var (result, messages) = await service.GetMessagesAsync(conversationId: 1, userId: 1);

            Assert.Equal(GetMessagesResult.ConversationNotFound, result);
            Assert.Null(messages);
        }

        [Fact]
        public async Task GetMessagesAsync_UserNotPartOfConversation_ReturnsConversationNotFound()
        {
            using var context = CreateContext();
            context.Conversations.Add(new Conversation { Id = 1, User1Id = 1, User2Id = 2, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new ConversationService(context, CreateHubContextMock().Object);
            // user 3 isn't part of this conversation at all
            var (result, messages) = await service.GetMessagesAsync(conversationId: 1, userId: 3);

            Assert.Equal(GetMessagesResult.ConversationNotFound, result);
            Assert.Null(messages);
        }

        // NOTE: not testing the happy path here directly, since ExecuteUpdateAsync
        // is not supported by the EF Core InMemory provider and will throw at runtime.
        // Would need a SQLite in-memory context to exercise this method end-to-end.

        // ---------- SendMessageAsync ----------

        [Fact]
        public async Task SendMessageAsync_ConversationNotFound_ReturnsConversationNotFound()
        {
            using var context = CreateContext();
            var service = new ConversationService(context, CreateHubContextMock().Object);

            var (result, message) = await service.SendMessageAsync(conversationId: 1, userId: 1, new SendMessageDto { Content = "hi" });

            Assert.Equal(SendMessageResult.ConversationNotFound, result);
            Assert.Null(message);
        }

        [Fact]
        public async Task SendMessageAsync_ValidData_CreatesMessageAndReturnsSuccess()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "sender", CreatedAt = DateTime.UtcNow });
            context.Conversations.Add(new Conversation { Id = 1, User1Id = 1, User2Id = 2, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new ConversationService(context, CreateHubContextMock().Object);
            var (result, message) = await service.SendMessageAsync(conversationId: 1, userId: 1, new SendMessageDto { Content = "Hello!" });

            Assert.Equal(SendMessageResult.Success, result);
            Assert.NotNull(message);
            Assert.Equal("Hello!", message.Content);
            Assert.Equal("sender", message.SenderUsername);
            Assert.Single(context.Messages);
        }

        [Fact]
        public async Task SendMessageAsync_UserNotPartOfConversation_ReturnsConversationNotFound()
        {
            using var context = CreateContext();
            context.Conversations.Add(new Conversation { Id = 1, User1Id = 1, User2Id = 2, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new ConversationService(context, CreateHubContextMock().Object);
            var (result, message) = await service.SendMessageAsync(conversationId: 1, userId: 3, new SendMessageDto { Content = "hi" });

            Assert.Equal(SendMessageResult.ConversationNotFound, result);
            Assert.Empty(context.Messages); // nothing was created
        }

        [Fact]
        public async Task SendMessageAsync_NotifiesBothSenderAndRecipientGroups()
        {
            using var context = CreateContext();
            context.Users.Add(new User { Id = 1, Username = "sender", CreatedAt = DateTime.UtcNow });
            context.Conversations.Add(new Conversation { Id = 1, User1Id = 1, User2Id = 2, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var mockClientProxy = new Mock<IClientProxy>();
            mockClientProxy
                .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default))
                .Returns(Task.CompletedTask);

            var mockClients = new Mock<IHubClients>();
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

            var mockHubContext = new Mock<IHubContext<ChatHub>>();
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

            var service = new ConversationService(context, mockHubContext.Object);
            await service.SendMessageAsync(conversationId: 1, userId: 1, new SendMessageDto { Content = "hi" });

            // verify both groups (sender "1" and recipient "2") were notified
            mockClients.Verify(c => c.Group("1"), Times.Once);
            mockClients.Verify(c => c.Group("2"), Times.Once);
            mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveMessage", It.IsAny<object?[]>(), default), Times.Exactly(2));
        }
    }
}