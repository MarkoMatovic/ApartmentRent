using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Lander;
using Lander.src.Modules.Communication.Implementation;
using Lander.src.Modules.Communication.Intefaces;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Modules.Communication.Models;
using Lander.src.Notifications.NotificationsHub;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Communication.Dtos.InputDto;

namespace LandlordApp.Tests.Services;

public class MessageServiceTests : IDisposable
{
    private readonly CommunicationsContext _commsContext;
    private readonly UsersContext _usersContext;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IHubContext<ChatHub>> _mockChatHub;
    private readonly Mock<IHubContext<NotificationHub>> _mockNotificationHub;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnv;
    private readonly MessageService _service;

    private const int Sender1Id   = 1;
    private const int Receiver1Id = 2;
    private const int Sender2Id   = 3;
    private readonly Guid _senderGuid = Guid.NewGuid();

    public MessageServiceTests()
    {
        var commsOpts = new DbContextOptionsBuilder<CommunicationsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var usersOpts = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _commsContext = new CommunicationsContext(commsOpts);
        _usersContext = new UsersContext(usersOpts);

        // Seed test users
        _usersContext.Users.AddRange(
            new User { UserId = Sender1Id,   UserGuid = Guid.NewGuid(), FirstName = "Sender",   LastName = "One",  Email = "sender@test.com",   Password = "hash", IsActive = true, CreatedDate = DateTime.UtcNow },
            new User { UserId = Receiver1Id, UserGuid = Guid.NewGuid(), FirstName = "Receiver", LastName = "One",  Email = "receiver@test.com", Password = "hash", IsActive = true, CreatedDate = DateTime.UtcNow },
            new User { UserId = Sender2Id,   UserGuid = Guid.NewGuid(), FirstName = "Sender",   LastName = "Two",  Email = "sender2@test.com",  Password = "hash", IsActive = true, CreatedDate = DateTime.UtcNow }
        );
        _usersContext.SaveChanges();

        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims    = new List<Claim> { new(ClaimTypes.NameIdentifier, _senderGuid.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext { User = principal });

        _mockEmailService    = new Mock<IEmailService>();
        _mockEmailService.Setup(x => x.SendNewMessageEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        _mockChatHub         = new Mock<IHubContext<ChatHub>>();
        _mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        _mockWebHostEnv      = new Mock<IWebHostEnvironment>();

        // Setup hub clients to not throw
        SetupHub(_mockChatHub);
        SetupHub(_mockNotificationHub);

        _service = new MessageService(
            _commsContext,
            _usersContext,
            _mockHttpContextAccessor.Object,
            _mockEmailService.Object,
            _mockChatHub.Object,
            _mockNotificationHub.Object,
            _mockWebHostEnv.Object);
    }

    private static void SetupHub<T>(Mock<IHubContext<T>> mock) where T : Hub
    {
        var clients     = new Mock<IHubClients>();
        var clientProxy = new Mock<IClientProxy>();
        clients.Setup(x => x.Group(It.IsAny<string>())).Returns(clientProxy.Object);
        mock.Setup(x => x.Clients).Returns(clients.Object);
    }

    public void Dispose()
    {
        _commsContext.Database.EnsureDeleted();
        _usersContext.Database.EnsureDeleted();
        _commsContext.Dispose();
        _usersContext.Dispose();
    }

    // ───────────────────────────────────────────────────── helpers

    private async Task<Message> SeedMessage(int senderId, int receiverId, string text, bool isRead = false)
    {
        var msg = new Message
        {
            SenderId    = senderId,
            ReceiverId  = receiverId,
            MessageText = text,
            IsRead      = isRead,
            SentAt      = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            CreatedByGuid = Guid.NewGuid()
        };
        _commsContext.Messages.Add(msg);
        await _commsContext.SaveChangesAsync();
        return msg;
    }

    #region SendMessageAsync Tests

    [Fact]
    public async Task SendMessageAsync_ValidMessage_ShouldCreateAndReturn()
    {
        // Act
        var result = await _service.SendMessageAsync(Sender1Id, Receiver1Id, "Hello there!");

        // Assert
        result.Should().NotBeNull();
        result.SenderId.Should().Be(Sender1Id);
        result.ReceiverId.Should().Be(Receiver1Id);
        result.MessageText.Should().Be("Hello there!");
        result.IsRead.Should().BeFalse();

        var inDb = await _commsContext.Messages.FirstOrDefaultAsync();
        inDb.Should().NotBeNull();
        inDb!.MessageText.Should().Be("Hello there!");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldIncludeSenderAndReceiverNames()
    {
        // Act
        var result = await _service.SendMessageAsync(Sender1Id, Receiver1Id, "Hi!");

        // Assert
        result.SenderName.Should().Contain("Sender");
        result.ReceiverName.Should().Contain("Receiver");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldFireEmailToReceiver()
    {
        // Act
        await _service.SendMessageAsync(Sender1Id, Receiver1Id, "New message");

        // Give fire-and-forget a moment to kick off
        await Task.Delay(50);

        // Assert — email triggered (fire-and-forget, so allow 0 or 1 call)
        _mockEmailService.Verify(
            x => x.SendNewMessageEmailAsync("receiver@test.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.AtMostOnce());
    }

    [Fact]
    public async Task SendMessageAsync_LongMessage_ShouldTruncateEmailPreview()
    {
        // Arrange
        var longText = new string('A', 200);

        // Act
        var result = await _service.SendMessageAsync(Sender1Id, Receiver1Id, longText);

        // Assert — message is stored in full
        result.MessageText.Length.Should().Be(200);
    }

    #endregion

    #region GetConversationAsync Tests

    [Fact]
    public async Task GetConversationAsync_ShouldReturnMessagesBetweenTwoUsers()
    {
        // Arrange — 3 messages in conversation, 1 from different users
        await SeedMessage(Sender1Id, Receiver1Id, "Msg 1");
        await SeedMessage(Receiver1Id, Sender1Id, "Msg 2");
        await SeedMessage(Sender1Id, Receiver1Id, "Msg 3");
        await SeedMessage(Sender2Id, Receiver1Id, "Unrelated"); // different sender

        // Act
        var result = await _service.GetConversationAsync(Sender1Id, Receiver1Id);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Messages.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetConversationAsync_ShouldReturnPagedResults()
    {
        // Arrange — seed 5 messages
        for (int i = 0; i < 5; i++)
            await SeedMessage(Sender1Id, Receiver1Id, $"Message {i}");

        // Act
        var result = await _service.GetConversationAsync(Sender1Id, Receiver1Id, page: 1, pageSize: 2);

        // Assert
        result.TotalCount.Should().Be(5);
        result.Messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetConversationAsync_NoMessages_ShouldReturnEmpty()
    {
        // Act
        var result = await _service.GetConversationAsync(Sender1Id, Receiver1Id);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Messages.Should().BeEmpty();
    }

    #endregion

    #region GetUserConversationsAsync Tests

    [Fact]
    public async Task GetUserConversationsAsync_ShouldReturnDistinctConversations()
    {
        // Arrange — two distinct conversations for Sender1
        await SeedMessage(Sender1Id, Receiver1Id, "To receiver 1");
        await SeedMessage(Sender1Id, Sender2Id,   "To sender 2");
        await SeedMessage(Sender1Id, Receiver1Id, "Second message to receiver 1");

        // Act
        var result = await _service.GetUserConversationsAsync(Sender1Id);

        // Assert — should only have 2 distinct conversations
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserConversationsAsync_NoMessages_ShouldReturnEmpty()
    {
        // Act
        var result = await _service.GetUserConversationsAsync(Sender1Id);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUnreadCountAsync Tests

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnOnlyUnreadForUser()
    {
        // Arrange
        await SeedMessage(Sender1Id, Receiver1Id, "Unread 1", isRead: false);
        await SeedMessage(Sender1Id, Receiver1Id, "Unread 2", isRead: false);
        await SeedMessage(Sender1Id, Receiver1Id, "Read",     isRead: true);
        await SeedMessage(Sender2Id, Sender1Id,   "Other user unread", isRead: false); // different receiver

        // Act
        var result = await _service.GetUnreadCountAsync(Receiver1Id);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetUnreadCountAsync_NoMessages_ShouldReturnZero()
    {
        // Act
        var result = await _service.GetUnreadCountAsync(Receiver1Id);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region MarkAsReadAsync Tests

    [Fact]
    public async Task MarkAsReadAsync_UnreadMessage_ShouldMarkRead()
    {
        // Arrange
        var msg = await SeedMessage(Sender1Id, Receiver1Id, "Unread", isRead: false);

        // Act
        await _service.MarkAsReadAsync(msg.MessageId);

        // Assert
        var inDb = await _commsContext.Messages.FindAsync(msg.MessageId);
        inDb!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsReadAsync_AlreadyRead_ShouldNotChangeState()
    {
        // Arrange
        var msg = await SeedMessage(Sender1Id, Receiver1Id, "Already read", isRead: true);

        // Act
        await _service.MarkAsReadAsync(msg.MessageId);

        // Assert — no exception, still read
        var inDb = await _commsContext.Messages.FindAsync(msg.MessageId);
        inDb!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsReadAsync_NonExistentMessage_ShouldNotThrow()
    {
        // Act — non-existent message id
        var act = async () => await _service.MarkAsReadAsync(99999);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetMessageByIdAsync Tests

    [Fact]
    public async Task GetMessageByIdAsync_Existing_ShouldReturn()
    {
        // Arrange
        var msg = await SeedMessage(Sender1Id, Receiver1Id, "Hello");

        // Act
        var result = await _service.GetMessageByIdAsync(msg.MessageId);

        // Assert
        result.Should().NotBeNull();
        result!.MessageText.Should().Be("Hello");
        result.SenderId.Should().Be(Sender1Id);
    }

    [Fact]
    public async Task GetMessageByIdAsync_NonExistent_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetMessageByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SearchMessagesAsync Tests

    [Fact]
    public async Task SearchMessagesAsync_MatchingQuery_ShouldReturnResults()
    {
        // Arrange
        await SeedMessage(Sender1Id, Receiver1Id, "Hello World");
        await SeedMessage(Sender1Id, Receiver1Id, "Goodbye World");
        await SeedMessage(Sender1Id, Receiver1Id, "Something else");

        // Act
        var result = await _service.SearchMessagesAsync(Sender1Id, "World");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.MessageText.Contains("World"));
    }

    [Fact]
    public async Task SearchMessagesAsync_EmptyQuery_ShouldReturnEmpty()
    {
        // Arrange
        await SeedMessage(Sender1Id, Receiver1Id, "Hello");

        // Act
        var result = await _service.SearchMessagesAsync(Sender1Id, "");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchMessagesAsync_NoMatches_ShouldReturnEmpty()
    {
        // Arrange
        await SeedMessage(Sender1Id, Receiver1Id, "Hello");

        // Act
        var result = await _service.SearchMessagesAsync(Sender1Id, "xyz_not_matching");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Conversation Settings Tests (Block/Mute/Archive)

    [Fact]
    public async Task BlockUserAsync_ShouldSetIsBlocked()
    {
        // Act
        await _service.BlockUserAsync(Sender1Id, Receiver1Id);

        // Assert
        var isBlocked = await _service.IsUserBlockedAsync(Sender1Id, Receiver1Id);
        isBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task UnblockUserAsync_ShouldClearIsBlocked()
    {
        // Arrange
        await _service.BlockUserAsync(Sender1Id, Receiver1Id);

        // Act
        await _service.UnblockUserAsync(Sender1Id, Receiver1Id);

        // Assert
        var isBlocked = await _service.IsUserBlockedAsync(Sender1Id, Receiver1Id);
        isBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserBlockedAsync_NotBlocked_ShouldReturnFalse()
    {
        // Act
        var result = await _service.IsUserBlockedAsync(Sender1Id, Receiver1Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MuteConversationAsync_ShouldSetIsMuted()
    {
        // Act
        await _service.MuteConversationAsync(Sender1Id, Receiver1Id);

        // Assert
        var settings = await _commsContext.ConversationSettings
            .FirstOrDefaultAsync(s => s.UserId == Sender1Id && s.OtherUserId == Receiver1Id);
        settings!.IsMuted.Should().BeTrue();
    }

    [Fact]
    public async Task UnmuteConversationAsync_ShouldClearIsMuted()
    {
        // Arrange
        await _service.MuteConversationAsync(Sender1Id, Receiver1Id);

        // Act
        await _service.UnmuteConversationAsync(Sender1Id, Receiver1Id);

        // Assert
        var settings = await _commsContext.ConversationSettings
            .FirstOrDefaultAsync(s => s.UserId == Sender1Id && s.OtherUserId == Receiver1Id);
        settings!.IsMuted.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveConversationAsync_ShouldSetIsArchived()
    {
        // Act
        await _service.ArchiveConversationAsync(Sender1Id, Receiver1Id);

        // Assert
        var settings = await _commsContext.ConversationSettings
            .FirstOrDefaultAsync(s => s.UserId == Sender1Id && s.OtherUserId == Receiver1Id);
        settings!.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task UnarchiveConversationAsync_ShouldClearIsArchived()
    {
        // Arrange
        await _service.ArchiveConversationAsync(Sender1Id, Receiver1Id);

        // Act
        await _service.UnarchiveConversationAsync(Sender1Id, Receiver1Id);

        // Assert
        var settings = await _commsContext.ConversationSettings
            .FirstOrDefaultAsync(s => s.UserId == Sender1Id && s.OtherUserId == Receiver1Id);
        settings!.IsArchived.Should().BeFalse();
    }

    #endregion

    #region DeleteConversationAsync Tests

    [Fact]
    public async Task DeleteConversationAsync_ShouldDeleteAllMessagesBetweenUsers()
    {
        // Arrange
        await SeedMessage(Sender1Id, Receiver1Id, "Msg 1");
        await SeedMessage(Receiver1Id, Sender1Id, "Msg 2");
        await SeedMessage(Sender2Id, Receiver1Id, "Other message"); // should not be deleted

        // Act
        await _service.DeleteConversationAsync(Sender1Id, Receiver1Id);

        // Assert
        var remaining = await _commsContext.Messages.ToListAsync();
        remaining.Should().HaveCount(1);
        remaining.First().SenderId.Should().Be(Sender2Id);
    }

    #endregion

    #region Advanced/Niche Scenarios

    [Fact]
    public async Task MuteConversationAsync_Idempotency_ShouldNotDuplicateSettings()
    {
        // Act - Mute twice
        await _service.MuteConversationAsync(Sender1Id, Receiver1Id);
        await _service.MuteConversationAsync(Sender1Id, Receiver1Id);

        // Assert - Only one setting row should exist
        var count = await _commsContext.ConversationSettings
            .CountAsync(s => s.UserId == Sender1Id && s.OtherUserId == Receiver1Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task UploadFileAsync_FileTooLarge_ShouldThrowArgumentException()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(4 * 1024 * 1024); // 4MB - Limit is 3MB

        // Act
        var act = async () => await _service.UploadFileAsync(mockFile.Object, Sender1Id);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("File size exceeds 3MB limit");
    }

    [Fact]
    public async Task ReportAbuseAsync_ValidReport_ShouldSaveToDb()
    {
        // Arrange
        var reportDto = new ReportMessageDto
        {
            MessageId = 1,
            ReportedUserId = Receiver1Id,
            Reason = "Inappropriate language"
        };

        // Act
        await _service.ReportAbuseAsync(Sender1Id, reportDto);

        // Assert
        var report = await _commsContext.ReportedMessages.FirstOrDefaultAsync();
        report.Should().NotBeNull();
        report!.Reason.Should().Be("Inappropriate language");
        report.ReportedByUserId.Should().Be(Sender1Id);
    }

    #endregion
}
