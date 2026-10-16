using Lander.src.Modules.Communication.Hubs;
using System.Text.Json;
using FluentAssertions;
using Lander;
using Lander.src.Notifications;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LandlordApp.Tests.Notifications;

/// <summary>
/// The live push carries a translation key and a route in its 5th argument, so the browser can show the
/// notification in the user's language and open the right page when it is clicked.
/// </summary>
public class NotificationDescriptorTests
{
    private static JsonElement Parse(string? json) => JsonDocument.Parse(json!).RootElement;

    [Fact]
    public void Create_CarriesKeyParamsAndRoute()
    {
        var json = NotificationDescriptor.Create(
            "pairing_apartment",
            new Dictionary<string, string> { ["name"] = "Ana Anić", ["title"] = "Stan na Zvezdari" },
            "/my-roommate");

        var d = Parse(json);
        d.GetProperty("key").GetString().Should().Be("pairing_apartment");
        d.GetProperty("params").GetProperty("name").GetString().Should().Be("Ana Anić");
        d.GetProperty("params").GetProperty("title").GetString().Should().Be("Stan na Zvezdari");
        d.GetProperty("link").GetString().Should().Be("/my-roommate");
    }

    [Fact]
    public void Create_KeepsQuotesAndNonAsciiInNamesIntact()
    {
        var json = NotificationDescriptor.Create("new_message", new Dictionary<string, string> { ["name"] = "Đorđe \"Đole\" Šćepanović" }, null);

        Parse(json).GetProperty("params").GetProperty("name").GetString().Should().Be("Đorđe \"Đole\" Šćepanović");
    }

    [Fact]
    public void Create_CarriesTheIdOfTheStoredNotification_AndIgnoresAnEmptyOne()
    {
        Parse(NotificationDescriptor.Create("pairing_ended", null, null, 17)).GetProperty("id").GetInt32().Should().Be(17);
        Parse(NotificationDescriptor.Create("pairing_ended", null, null, 0)).TryGetProperty("id", out _).Should().BeFalse();
        Parse(NotificationDescriptor.Create(null, null, null, 17)).GetProperty("id").GetInt32().Should().Be(17);
        NotificationDescriptor.Create(null, null, null, 0).Should().BeNull();
    }

    [Fact]
    public void Create_OmitsWhatIsNotThere()
    {
        var keyOnly = Parse(NotificationDescriptor.Create("pairing_ended", null, null));
        keyOnly.TryGetProperty("params", out _).Should().BeFalse();
        keyOnly.TryGetProperty("link", out _).Should().BeFalse();

        var linkOnly = Parse(NotificationDescriptor.Create(null, null, "/appointments/manage"));
        linkOnly.TryGetProperty("key", out _).Should().BeFalse();
        linkOnly.GetProperty("link").GetString().Should().Be("/appointments/manage");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]                       // an apartment id, not a route
    [InlineData("https://evil.example/x")]    // never follow a full URL
    [InlineData("//evil.example/x")]          // protocol-relative URL
    [InlineData("javascript:alert(1)")]
    public void Create_IgnoresAnythingThatIsNotAnInAppRoute(string? target)
    {
        NotificationDescriptor.Create(null, null, target).Should().BeNull();
        Parse(NotificationDescriptor.Create("pairing_ended", null, target)).TryGetProperty("link", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("/chat", true)]
    [InlineData("/chat?userId=5", true)]
    [InlineData("chat", false)]
    [InlineData("//x", false)]
    [InlineData(null, false)]
    public void IsRoute_OnlyAcceptsSingleSlashPaths(string? target, bool expected) =>
        NotificationDescriptor.IsRoute(target).Should().Be(expected);

    // ── NotificationService: what is actually pushed ─────────────────────────

    private static (NotificationService service, List<object?[]> pushes, NotificationContext context) BuildService()
    {
        var pushes = new List<object?[]>();
        var proxy = new Mock<IClientProxy>();
        proxy.Setup(p => p.SendCoreAsync("ReceiveNotification", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) => pushes.Add(args))
            .Returns(Task.CompletedTask);
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(proxy.Object);
        var hub = new Mock<IHubContext<ChatHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);

        var options = new DbContextOptionsBuilder<NotificationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new NotificationContext(options);
        var service = new NotificationService(context, hub.Object, new Mock<IHttpContextAccessor>().Object);
        return (service, pushes, context);
    }

    private static CreateNotificationInputDto Input(string target, string? key = null, Dictionary<string, string>? parameters = null) => new()
    {
        Title = "Naslov", Message = "Poruka", ActionType = "x", ActionTarget = target,
        SenderUserId = 1, RecipientUserId = 2, Key = key, Params = parameters,
    };

    [Fact]
    public async Task Send_WithAKey_PushesTheDescriptorAsTheFifthArgument_AndKeepsTheSerbianFallback()
    {
        var (service, pushes, context) = BuildService();

        await service.SendNotificationAsync(Input("/my-roommate", "pairing_accepted", new() { ["name"] = "Marko" }));

        var args = pushes.Should().ContainSingle().Subject;
        args.Should().HaveCount(5);
        args[0].Should().Be("Naslov");
        args[1].Should().Be("Poruka");
        var d = Parse((string)args[4]!);
        d.GetProperty("key").GetString().Should().Be("pairing_accepted");
        d.GetProperty("params").GetProperty("name").GetString().Should().Be("Marko");
        d.GetProperty("link").GetString().Should().Be("/my-roommate");
    }

    [Fact]
    public async Task Send_WithOnlyARoute_StillTellsTheBrowserWhereToGo()
    {
        var (service, pushes, context) = BuildService();

        await service.SendNotificationAsync(Input("/appointments/manage"));

        var d = Parse((string)pushes.Single()[4]!);
        d.TryGetProperty("key", out _).Should().BeFalse();
        d.GetProperty("link").GetString().Should().Be("/appointments/manage");
    }

    [Fact]
    public async Task Send_WithNeitherKeyNorRoute_StillPushesTheIdSoItCanBeMarkedRead()
    {
        var (service, pushes, context) = BuildService();

        var sent = await service.SendNotificationAsync(Input("42"));   // an apartment id, not a route

        var d = Parse((string)pushes.Single()[4]!);
        d.GetProperty("id").GetInt32().Should().Be(sent.Id);
        d.TryGetProperty("key", out _).Should().BeFalse();
        d.TryGetProperty("link", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Send_PushesTheIdOfTheStoredNotification()
    {
        var (service, pushes, context) = BuildService();

        var sent = await service.SendNotificationAsync(Input("/my-roommate", "pairing_ended", new() { ["name"] = "Marko" }));

        sent.Id.Should().BeGreaterThan(0);
        Parse((string)pushes.Single()[4]!).GetProperty("id").GetInt32().Should().Be(sent.Id);
    }

    [Fact]
    public async Task Send_UsesTheGivenTypeAndMetadata_ForTheRejectionDialog()
    {
        var (service, pushes, context) = BuildService();
        var input = Input("/applications/sent", "application_rejected");
        input.Type = "rejection";
        input.Metadata = "{\"apartmentId\":5}";

        await service.SendNotificationAsync(input);

        var args = pushes.Single();
        args[2].Should().Be("rejection");
        args[3].Should().Be("{\"apartmentId\":5}");
    }

    [Fact]
    public async Task Send_StoresTheKeyAndTheValues_SoALaterLoadCanTranslateThem()
    {
        var (service, _, context) = BuildService();

        await service.SendNotificationAsync(Input("/my-roommate", "pairing_apartment", new() { ["name"] = "Ana Anić", ["title"] = "Stan \"A\"" }));

        var stored = (await service.GetUserNotificationsAsync(2)).Single();
        stored.Key.Should().Be("pairing_apartment");
        stored.Params.Should().Contain("name", "Ana Anić").And.Contain("title", "Stan \"A\"");
        stored.Title.Should().Be("Naslov");   // the Serbian fallback is kept
    }

    [Fact]
    public async Task Persist_ReturnsTheIdAndStoresTheKey()
    {
        var (service, pushes, context) = BuildService();

        var id = await service.PersistNotificationAsync(Input("/applications/received", "application_new", new() { ["name"] = "Tara" }));

        id.Should().BeGreaterThan(0);
        pushes.Should().BeEmpty("persisting only stores; the caller pushes");
        var stored = (await service.GetUserNotificationsAsync(2)).Single();
        stored.Id.Should().Be(id);
        stored.Key.Should().Be("application_new");
    }

    [Fact]
    public async Task GetUserNotifications_ReturnsOnlyTheNewest_AtMostTheLimit()
    {
        var (service, _, _) = BuildService();
        for (var i = 0; i < 60; i++)
            await service.PersistNotificationAsync(Input("/chat", "new_message", new() { ["name"] = $"N{i}" }));

        var page = (await service.GetUserNotificationsAsync(2)).ToList();
        var few = (await service.GetUserNotificationsAsync(2, limit: 5)).ToList();
        var tooMany = (await service.GetUserNotificationsAsync(2, limit: 10_000)).ToList();

        page.Should().HaveCount(NotificationService.DefaultPageSize);
        page.Should().BeInDescendingOrder(n => n.CreatedDate);
        few.Should().HaveCount(5);
        tooMany.Should().HaveCount(60, "there are only 60; the cap of 100 is not reached");
    }

    [Fact]
    public async Task MarkAsRead_MovesItToTheReadTable_WithItsKeyAndValues()
    {
        var (service, _, context) = BuildService();
        var sent = await service.SendNotificationAsync(Input("/my-roommate", "pairing_accepted", new() { ["name"] = "Marko" }));

        await service.MarkAsReadAsync(sent.Id);

        (await service.GetUserNotificationsAsync(2)).Should().BeEmpty();
        var read = context.ReadNotifications.Single();
        read.Key.Should().Be("pairing_accepted");
        read.ParamsJson.Should().Contain("Marko");
    }

    [Fact]
    public async Task MarkAllAsRead_KeepsTheKeyAndValuesToo()
    {
        var (service, _, context) = BuildService();
        await service.SendNotificationAsync(Input("/my-roommate", "pairing_accepted", new() { ["name"] = "Marko" }));
        await service.SendNotificationAsync(Input("/my-roommate", "pairing_ended", new() { ["name"] = "Marko" }));

        await service.MarkAllAsReadAsync(2);

        (await service.GetUserNotificationsAsync(2)).Should().BeEmpty();
        context.ReadNotifications.Select(r => r.Key).Should().BeEquivalentTo(new[] { "pairing_accepted", "pairing_ended" });
    }

    [Fact]
    public async Task OnlyTheRecipientsOwnNotificationsAreListed()
    {
        var (service, _, _) = BuildService();
        await service.SendNotificationAsync(Input("/my-roommate", "pairing_ended"));

        (await service.GetUserNotificationsAsync(2)).Should().HaveCount(1);
        (await service.GetUserNotificationsAsync(99)).Should().BeEmpty();
    }
}
