using FluentAssertions;
using Lander;
using Lander.src.Common.Exceptions;
using Lander.src.Common.Validation;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Modules.Communication.Implementation;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Notifications.NotificationsHub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LandlordApp.Tests.Services;

/// <summary>
/// The conversation of two roommates and the landlord they both talk to: who may start it, that the
/// earlier history comes along, and who gets told about new messages.
/// </summary>
public class ChatGroupServiceTests : IDisposable
{
    private const int Ana = 1;       // roommate
    private const int Marko = 2;     // her roommate
    private const int Landlord = 3;
    private const int Stranger = 4;

    private readonly CommunicationsContext _comms;
    private readonly UsersContext _users;
    private readonly Mock<IRoommatePairingService> _pairings = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IPresenceTracker> _presence = new();
    private readonly Dictionary<string, List<string>> _chatPushes = new();
    private readonly Dictionary<string, List<string>> _bellPushes = new();
    private readonly Dictionary<string, List<object?[]>> _bellArgs = new();
    private readonly ChatGroupService _service;

    public ChatGroupServiceTests()
    {
        _comms = new CommunicationsContext(InMemory<CommunicationsContext>());
        _users = new UsersContext(InMemory<UsersContext>());

        _users.Users.AddRange(
            NewUser(Ana, "Ana", "Anić"),
            NewUser(Marko, "Marko", "Marković"),
            NewUser(Landlord, "Lazar", "Lazić"),
            NewUser(Stranger, "Petar", "Petrović"));
        _users.SaveChanges();

        // Ana and Marko are roommates
        _pairings.Setup(p => p.GetActivePartnerIdAsync(Ana)).ReturnsAsync(Marko);
        _pairings.Setup(p => p.GetActivePartnerIdAsync(Marko)).ReturnsAsync(Ana);

        _email.Setup(e => e.SendNewMessageEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        _service = new ChatGroupService(
            _comms, _users, _pairings.Object, _email.Object,
            RecordingHub<ChatHub>(_chatPushes), RecordingHub<NotificationHub>(_bellPushes, _bellArgs),
            _presence.Object, NullLogger<ChatGroupService>.Instance);
    }

    private static DbContextOptions<T> InMemory<T>() where T : DbContext =>
        new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private static User NewUser(int id, string first, string last, bool chatHistory = true) => new()
    {
        UserId = id, UserGuid = Guid.NewGuid(), FirstName = first, LastName = last,
        Email = $"{first.ToLower()}@test.com", Password = "hash", IsActive = true,
        ChatHistoryConsent = chatHistory, CreatedDate = DateTime.UtcNow,
    };

    /// <summary>A hub context that remembers which method was pushed to which SignalR group.</summary>
    private static IHubContext<T> RecordingHub<T>(
        Dictionary<string, List<string>> pushes, Dictionary<string, List<object?[]>>? arguments = null) where T : Hub
    {
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns<string>(group =>
        {
            var proxy = new Mock<IClientProxy>();
            proxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, object?[], CancellationToken>((method, args, _) =>
                {
                    if (!pushes.TryGetValue(group, out var list)) pushes[group] = list = new List<string>();
                    list.Add(method);
                    if (arguments is null) return;
                    if (!arguments.TryGetValue(group, out var all)) arguments[group] = all = new List<object?[]>();
                    all.Add(args);
                })
                .Returns(Task.CompletedTask);
            return proxy.Object;
        });
        var hub = new Mock<IHubContext<T>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);
        return hub.Object;
    }

    public void Dispose()
    {
        _comms.Dispose();
        _users.Dispose();
    }

    private async Task Say(int from, int to, string text, int minutesAgo = 0)
    {
        _comms.Messages.Add(new Message
        {
            SenderId = from, ReceiverId = to, MessageText = text, IsRead = true,
            SentAt = DateTime.UtcNow.AddMinutes(-minutesAgo), CreatedDate = DateTime.UtcNow,
        });
        await _comms.SaveChangesAsync();
    }

    private static async Task ShouldBeRule(Func<Task> act, string code) =>
        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(code);

    // ── Opening ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Open_NeedsARoommate()
    {
        await Say(Stranger, Landlord, "Zdravo");
        _pairings.Setup(p => p.GetActivePartnerIdAsync(Stranger)).ReturnsAsync((int?)null);

        await ShouldBeRule(() => _service.OpenWithRoommateAsync(Stranger, Landlord), BusinessErrorCodes.PairingRequired);
    }

    [Fact]
    public async Task Open_WithYourselfOrYourRoommate_IsNotAllowed()
    {
        await Say(Ana, Marko, "Hej");

        await ShouldBeRule(() => _service.OpenWithRoommateAsync(Ana, Ana), BusinessErrorCodes.GroupChatNotAllowed);
        await ShouldBeRule(() => _service.OpenWithRoommateAsync(Ana, Marko), BusinessErrorCodes.GroupChatNotAllowed);
    }

    [Fact]
    public async Task Open_WithSomeoneNeitherRoommateHasTalkedTo_IsNotAllowed()
    {
        await ShouldBeRule(() => _service.OpenWithRoommateAsync(Ana, Landlord), BusinessErrorCodes.GroupChatNotAllowed);
    }

    [Fact]
    public async Task Open_WithTheLandlordOfASharedApartment_NeedsNoEarlierConversation()
    {
        _pairings.Setup(p => p.IsLandlordOfSharedApartmentAsync(Ana, Landlord)).ReturnsAsync(true);

        var group = await _service.OpenWithRoommateAsync(Ana, Landlord);

        group.Members.Select(m => m.UserId).Should().BeEquivalentTo(new[] { Ana, Marko, Landlord });
        (await _service.GetMessagesAsync(group.GroupId, Landlord)).Messages.Should().BeEmpty();
        _pairings.Verify(p => p.IsLandlordOfSharedApartmentAsync(Ana, Landlord), Times.Once);
    }

    [Fact]
    public async Task Open_WithTheLandlordOfASharedApartment_StillCarriesOverAnEarlierConversation()
    {
        _pairings.Setup(p => p.IsLandlordOfSharedApartmentAsync(Ana, Landlord)).ReturnsAsync(true);
        await Say(Ana, Landlord, "Dobar dan");

        var group = await _service.OpenWithRoommateAsync(Ana, Landlord);

        (await _service.GetMessagesAsync(group.GroupId, Marko)).Messages.Select(m => m.MessageText).Should().Equal("Dobar dan");
        // an earlier conversation is enough: the shared list is not even asked
        _pairings.Verify(p => p.IsLandlordOfSharedApartmentAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Open_WhenTheLandlordBlockedOneOfThem_IsNotAllowed()
    {
        await Say(Ana, Landlord, "Zdravo");
        _comms.ConversationSettings.Add(new ConversationSettings { UserId = Landlord, OtherUserId = Marko, IsBlocked = true });
        await _comms.SaveChangesAsync();

        await ShouldBeRule(() => _service.OpenWithRoommateAsync(Ana, Landlord), BusinessErrorCodes.MessagingNotAllowed);
    }

    [Fact]
    public async Task Open_CreatesTheGroupOfThree_AndCarriesOverWhatBothRoommatesSaidToTheLandlord()
    {
        await Say(Ana, Landlord, "Zanima me stan", minutesAgo: 30);
        await Say(Landlord, Ana, "Slobodan je od prvog", minutesAgo: 20);
        await Say(Marko, Landlord, "I ja bih ga gledao", minutesAgo: 10);

        var group = await _service.OpenWithRoommateAsync(Ana, Landlord);

        group.Members.Select(m => m.UserId).Should().BeEquivalentTo(new[] { Ana, Marko, Landlord });
        group.Members.Single(m => m.UserId == Landlord).Name.Should().Be("Lazar Lazić");

        // Marko did not take part in Ana's talk, yet reads all of it: nobody has to retell it.
        var history = await _service.GetMessagesAsync(group.GroupId, Marko);
        history.Messages.Select(m => m.MessageText).Should().Equal("Zanima me stan", "Slobodan je od prvog", "I ja bih ga gledao");
        history.Messages.Select(m => m.SenderId).Should().Equal(Ana, Landlord, Marko);

        // ...and the carried-over history is not "news": nothing shows as unread.
        group.UnreadCount.Should().Be(0);
        (await _service.GetUnreadCountAsync(Marko)).Should().Be(0);
    }

    [Fact]
    public async Task Open_TellsTheOtherTwoMembers()
    {
        await Say(Ana, Landlord, "Zdravo");

        await _service.OpenWithRoommateAsync(Ana, Landlord);

        _chatPushes["user_" + Marko].Should().Contain("GroupCreated");
        _chatPushes["user_" + Landlord].Should().Contain("GroupCreated");
        _chatPushes.Should().NotContainKey("user_" + Ana);
        _bellPushes.Keys.Should().BeEquivalentTo(new[] { Marko.ToString(), Landlord.ToString() });
    }

    [Fact]
    public async Task Open_NotificationIsTranslatableAndOpensTheChatOnClick()
    {
        await Say(Ana, Landlord, "Zdravo");

        await _service.OpenWithRoommateAsync(Ana, Landlord);

        // title and message stay as the Serbian fallback; the 5th argument names the key, the name and the page
        var args = _bellArgs[Marko.ToString()].Single();
        args.Should().HaveCount(5);
        ((string)args[0]!).Should().Be("Novi grupni razgovor");
        using var descriptor = System.Text.Json.JsonDocument.Parse((string)args[4]!);
        descriptor.RootElement.GetProperty("key").GetString().Should().Be("group_created");
        descriptor.RootElement.GetProperty("params").GetProperty("name").GetString().Should().Be("Ana Anić");
        descriptor.RootElement.GetProperty("link").GetString().Should().Be("/chat");
    }

    [Fact]
    public async Task Open_Twice_GivesTheSameGroup()
    {
        await Say(Ana, Landlord, "Zdravo");

        var first = await _service.OpenWithRoommateAsync(Ana, Landlord);
        var again = await _service.OpenWithRoommateAsync(Marko, Landlord); // the other roommate opens it too

        again.GroupId.Should().Be(first.GroupId);
        (await _comms.ChatGroups.CountAsync()).Should().Be(1);
        (await _comms.ChatGroupMessages.CountAsync()).Should().Be(1, "the history is copied once");
    }

    [Fact]
    public async Task Open_CopiesNothing_WhenSomeoneKeepsNoChatHistory()
    {
        var landlord = await _users.Users.FindAsync(Landlord);
        landlord!.ChatHistoryConsent = false;
        await _users.SaveChangesAsync();
        await Say(Ana, Landlord, "Zdravo");

        var group = await _service.OpenWithRoommateAsync(Ana, Landlord);

        (await _comms.ChatGroupMessages.CountAsync(m => m.GroupId == group.GroupId)).Should().Be(0);
    }

    // ── Sending ──────────────────────────────────────────────────────────────

    private async Task<int> OpenGroup()
    {
        await Say(Ana, Landlord, "Zdravo", minutesAgo: 5);
        var group = await _service.OpenWithRoommateAsync(Ana, Landlord);
        _chatPushes.Clear();
        _bellPushes.Clear();
        _bellArgs.Clear();
        return group.GroupId;
    }

    [Fact]
    public async Task Send_StoresTheMessage_AndPushesItToEveryMember()
    {
        var groupId = await OpenGroup();

        var sent = await _service.SendMessageAsync(groupId, Marko, "Može u subotu?");

        sent.SenderName.Should().Be("Marko Marković");
        (await _service.GetMessagesAsync(groupId, Landlord)).Messages.Last().MessageText.Should().Be("Može u subotu?");
        foreach (var member in new[] { Ana, Marko, Landlord })
            _chatPushes["user_" + member].Should().Contain("ReceiveGroupMessage");
    }

    [Fact]
    public async Task Send_NotificationNamesTheSenderAndOpensThatGroupOnClick()
    {
        var groupId = await OpenGroup();

        await _service.SendMessageAsync(groupId, Marko, "Stižemo u 10");

        var args = _bellArgs[Landlord.ToString()].Single();
        using var descriptor = System.Text.Json.JsonDocument.Parse((string)args[4]!);
        descriptor.RootElement.GetProperty("key").GetString().Should().Be("group_message");
        descriptor.RootElement.GetProperty("params").GetProperty("name").GetString().Should().Be("Marko Marković");
        descriptor.RootElement.GetProperty("link").GetString().Should().Be($"/chat?group={groupId}");
    }

    [Fact]
    public async Task Send_ByANonMember_IsForbidden()
    {
        var groupId = await OpenGroup();

        await FluentActions.Awaiting(() => _service.SendMessageAsync(groupId, Stranger, "Ja sam ovde"))
            .Should().ThrowAsync<ForbiddenException>();
        (await _comms.ChatGroupMessages.CountAsync(m => m.SenderId == Stranger)).Should().Be(0);
    }

    [Fact]
    public async Task Send_ToAGroupThatDoesNotExist_IsNotFound()
    {
        await FluentActions.Awaiting(() => _service.SendMessageAsync(999, Ana, "Hej"))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Send_StripsMarkup_AndRejectsAMessageThatIsEmptyAfterThat()
    {
        var groupId = await OpenGroup();

        var sent = await _service.SendMessageAsync(groupId, Ana, "<b>Zdravo</b> svima");
        sent.MessageText.Should().NotContain("<");

        await ShouldBeRule(() => _service.SendMessageAsync(groupId, Ana, "<script></script>"), ValidationCodes.MessageEmpty);
    }

    [Fact]
    public async Task Send_MailsOnlyThoseWhoAreAway()
    {
        var groupId = await OpenGroup();
        _presence.Setup(p => p.IsOnline(Landlord)).Returns(true);   // reads it live
        _presence.Setup(p => p.IsOnline(Ana)).Returns(false);       // away

        await _service.SendMessageAsync(groupId, Marko, "Stižemo u 10");

        _email.Verify(e => e.SendNewMessageEmailAsync("ana@test.com", "Marko Marković", It.IsAny<string>()), Times.Once);
        _email.Verify(e => e.SendNewMessageEmailAsync("lazar@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _email.Verify(e => e.SendNewMessageEmailAsync("marko@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Send_DoesNotBotherSomeoneWhoBlockedTheSender()
    {
        var groupId = await OpenGroup();
        _comms.ConversationSettings.Add(new ConversationSettings { UserId = Landlord, OtherUserId = Marko, IsBlocked = true });
        await _comms.SaveChangesAsync();

        await _service.SendMessageAsync(groupId, Marko, "Hej");

        _bellPushes.Keys.Should().BeEquivalentTo(new[] { Ana.ToString() });
        _email.Verify(e => e.SendNewMessageEmailAsync("lazar@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── Reading ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unread_CountsOthersMessages_UntilTheGroupIsRead()
    {
        var groupId = await OpenGroup();

        await _service.SendMessageAsync(groupId, Landlord, "Prvi");
        await _service.SendMessageAsync(groupId, Landlord, "Drugi");

        (await _service.GetUnreadCountAsync(Marko)).Should().Be(2);
        (await _service.GetUnreadCountAsync(Ana)).Should().Be(2);
        (await _service.GetUnreadCountAsync(Landlord)).Should().Be(0, "your own messages are never unread");
        (await _service.GetUserGroupsAsync(Marko)).Single().UnreadCount.Should().Be(2);

        await _service.MarkReadAsync(groupId, Marko);

        (await _service.GetUnreadCountAsync(Marko)).Should().Be(0);
        (await _service.GetUnreadCountAsync(Ana)).Should().Be(2, "reading is per person");
    }

    [Fact]
    public async Task Replying_CountsAsHavingReadTheConversation()
    {
        var groupId = await OpenGroup();
        await _service.SendMessageAsync(groupId, Landlord, "Pitanje");

        await _service.SendMessageAsync(groupId, Ana, "Odgovor");

        (await _service.GetUnreadCountAsync(Ana)).Should().Be(0);
        (await _service.GetUnreadCountAsync(Marko)).Should().Be(2, "the roommate has still seen neither");
    }

    [Fact]
    public async Task GetUserGroups_ListsMyGroupsWithMembersAndLastMessage_NewestFirst()
    {
        var first = await OpenGroup();
        await _service.SendMessageAsync(first, Ana, "Stara grupa");

        // a second landlord, a second group
        _users.Users.Add(NewUser(5, "Sara", "Savić"));
        await _users.SaveChangesAsync();
        await Say(Marko, 5, "Dobar dan");
        var second = (await _service.OpenWithRoommateAsync(Marko, 5)).GroupId;
        await _service.SendMessageAsync(second, Marko, "Nova grupa");

        var groups = await _service.GetUserGroupsAsync(Ana);

        groups.Select(g => g.GroupId).Should().Equal(second, first);
        groups[0].LastMessage!.MessageText.Should().Be("Nova grupa");
        groups[1].Members.Should().HaveCount(3);

        (await _service.GetUserGroupsAsync(Stranger)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessages_ForANonMember_IsForbidden()
    {
        var groupId = await OpenGroup();

        await FluentActions.Awaiting(() => _service.GetMessagesAsync(groupId, Stranger))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetMessages_IsEmpty_WhenAMemberKeepsNoChatHistory()
    {
        var groupId = await OpenGroup();
        await _service.SendMessageAsync(groupId, Ana, "Vidi se");
        var marko = await _users.Users.FindAsync(Marko);
        marko!.ChatHistoryConsent = false;
        await _users.SaveChangesAsync();

        (await _service.GetMessagesAsync(groupId, Landlord)).Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessages_PagesFromTheNewestEnd()
    {
        var groupId = await OpenGroup();
        for (var i = 1; i <= 5; i++) await _service.SendMessageAsync(groupId, Ana, $"Poruka {i}");

        var page = await _service.GetMessagesAsync(groupId, Ana, page: 1, pageSize: 2);

        page.TotalCount.Should().Be(6); // the carried-over "Zdravo" + 5
        page.Messages.Select(m => m.MessageText).Should().Equal("Poruka 4", "Poruka 5");
    }

    // ── Leaving ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Leave_RemovesThePersonFromTheGroup_AndOthersStay()
    {
        var groupId = await OpenGroup();

        await _service.LeaveAsync(groupId, Marko);

        await FluentActions.Awaiting(() => _service.GetMessagesAsync(groupId, Marko)).Should().ThrowAsync<ForbiddenException>();
        (await _service.GetUserGroupsAsync(Marko)).Should().BeEmpty();
        (await _service.GetMessagesAsync(groupId, Ana)).Messages.Should().NotBeEmpty();
    }
}
