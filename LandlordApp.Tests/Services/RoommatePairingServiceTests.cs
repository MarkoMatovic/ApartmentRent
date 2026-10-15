using FluentAssertions;
using Lander;
using Lander.src.Common.Exceptions;
using Lander.src.Common.Validation;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Roommates.Dtos.InputDto;
using Lander.src.Modules.Roommates.Implementation;
using Lander.src.Modules.Roommates.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LandlordApp.Tests.Services;

/// <summary>
/// Two people agreeing to be roommates: the proposal flow, "taken" people disappearing from roommate
/// search, and the shortlist of apartments the pair shares.
/// </summary>
public class RoommatePairingServiceTests : IDisposable
{
    private const int Ana = 1;
    private const int Marko = 2;
    private const int Jovana = 3;
    private const int Petar = 4;

    private readonly RoommatesContext _roommates;
    private readonly UsersContext _users;
    private readonly ListingsContext _listings;
    private readonly Mock<INotificationService> _notifications = new();
    private readonly List<CreateNotificationInputDto> _sent = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<Lander.src.Modules.Communication.Interfaces.IEmailService> _email = new();
    private readonly Mock<Lander.src.Infrastructure.Services.IPresenceTracker> _presence = new();
    private readonly RoommatePairingService _service;
    private readonly RoommateService _roommateService;

    public RoommatePairingServiceTests()
    {
        _roommates = new RoommatesContext(InMemory<RoommatesContext>());
        _users = new UsersContext(InMemory<UsersContext>());
        _listings = new ListingsContext(InMemory<ListingsContext>());

        _users.Users.AddRange(
            NewUser(Ana, "Ana", "Anić"),
            NewUser(Marko, "Marko", "Marković"),
            NewUser(Jovana, "Jovana", "Jović"),
            NewUser(Petar, "Petar", "Petrović"));
        _users.SaveChanges();

        _notifications
            .Setup(n => n.SendNotificationAsync(It.IsAny<CreateNotificationInputDto>()))
            .Callback<CreateNotificationInputDto>(dto => _sent.Add(dto))
            .ReturnsAsync(new Lander.src.Notifications.Dtos.Dto.NotificationDto());

        _service = new RoommatePairingService(
            _roommates, _users, _listings, _notifications.Object, _cache, _email.Object, _presence.Object,
            NullLogger<RoommatePairingService>.Instance);
        _email.Setup(e => e.SendRoommateProposalEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _email.Setup(e => e.SendRoommateAcceptedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _roommateService = new RoommateService(_roommates, _users, new Mock<IHttpContextAccessor>().Object, _cache);
    }

    private static DbContextOptions<T> InMemory<T>() where T : DbContext =>
        new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private static User NewUser(int id, string first, string last) => new()
    {
        UserId = id, UserGuid = Guid.NewGuid(), FirstName = first, LastName = last,
        Email = $"{first.ToLower()}@test.com", Password = "hash", IsActive = true, CreatedDate = DateTime.UtcNow,
    };

    public void Dispose()
    {
        _roommates.Dispose();
        _users.Dispose();
        _listings.Dispose();
        _cache.Dispose();
    }

    private async Task Pair(int a, int b)
    {
        var proposal = await _service.ProposeAsync(a, b);
        await _service.AcceptAsync(b, proposal.PairingId);
    }

    private async Task<Apartment> SeedApartment(string title = "Stan na Zvezdari", bool deleted = false, bool active = true)
    {
        var apartment = new Apartment { Title = title, Address = "Ulica 1", City = "Beograd", Rent = 400, IsActive = active, IsDeleted = deleted };
        _listings.Apartments.Add(apartment);
        await _listings.SaveChangesAsync();
        return apartment;
    }

    private async Task SeedRoommateProfile(int userId, string location = "Beograd")
    {
        _roommates.Roommates.Add(new Roommate { UserId = userId, PreferredLocation = location, IsActive = true, CreatedDate = DateTime.UtcNow });
        await _roommates.SaveChangesAsync();
    }

    private async Task<IReadOnlyList<int>> ListedUserIds(bool includeTaken = false)
    {
        var page = await _roommateService.GetAllRoommatesAsync(new RoommateFilterQuery { IncludeTaken = includeTaken });
        return page.Items.Select(r => r.UserId).ToList();
    }

    // ── Proposing ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Propose_CreatesPendingProposal_AndTellsThePartner()
    {
        var result = await _service.ProposeAsync(Ana, Marko);

        result.Status.Should().Be(PairingStatus.Pending);
        result.PartnerUserId.Should().Be(Marko);
        result.PartnerName.Should().Be("Marko Marković");
        result.IsIncoming.Should().BeFalse();

        _sent.Should().ContainSingle();
        _sent[0].RecipientUserId.Should().Be(Marko);
        _sent[0].SenderUserId.Should().Be(Ana);
        _sent[0].Message.Should().Contain("Ana Anić");
    }

    [Fact]
    public async Task Notifications_CarryATranslationKeyAndTheNameOfWhoDidIt_AndPointToTheRoommatePage()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);
        await _service.AcceptAsync(Marko, proposal.PairingId);

        var proposed = _sent.Single(n => n.ActionType == "pairing_proposed");
        proposed.Key.Should().Be("pairing_proposed");
        proposed.Params.Should().Contain("name", "Ana Anić");
        proposed.ActionTarget.Should().Be("/my-roommate");

        var accepted = _sent.Single(n => n.ActionType == "pairing_accepted");
        accepted.Key.Should().Be("pairing_accepted");
        accepted.Params.Should().Contain("name", "Marko Marković");
        // the Serbian text stays as the fallback for anything that does not know the key
        accepted.Message.Should().Contain("Marko Marković");
    }

    [Fact]
    public async Task Propose_ToYourself_IsRejected()
    {
        var act = () => _service.ProposeAsync(Ana, Ana);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.PairingWithSelf);
    }

    [Fact]
    public async Task Propose_ToUnknownUser_IsNotFound()
    {
        var act = () => _service.ProposeAsync(Ana, 999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Propose_Twice_KeepsOneProposalAndNotifiesOnce()
    {
        var first = await _service.ProposeAsync(Ana, Marko);
        var second = await _service.ProposeAsync(Ana, Marko);

        second.PairingId.Should().Be(first.PairingId);
        (await _roommates.Pairings.CountAsync()).Should().Be(1);
        _sent.Should().ContainSingle();
    }

    [Fact]
    public async Task Propose_BackToSomeoneWhoAlreadyAskedMe_AcceptsIt()
    {
        var theirs = await _service.ProposeAsync(Marko, Ana);

        var result = await _service.ProposeAsync(Ana, Marko);

        result.PairingId.Should().Be(theirs.PairingId);
        result.Status.Should().Be(PairingStatus.Accepted);
        (await _service.AreRoommatesAsync(Ana, Marko)).Should().BeTrue();
    }

    [Fact]
    public async Task Propose_WhenYouAlreadyHaveARoommate_IsRejected()
    {
        await Pair(Ana, Marko);

        var act = () => _service.ProposeAsync(Ana, Jovana);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.YouAlreadyHaveRoommate);
    }

    [Fact]
    public async Task Propose_ToSomeoneWhoAlreadyHasARoommate_IsRejected()
    {
        await Pair(Marko, Jovana);

        var act = () => _service.ProposeAsync(Ana, Marko);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.RoommateAlreadyTaken);
    }

    // ── E-mail for somebody who is away ──────────────────────────────────────

    [Fact]
    public async Task Propose_EmailsThePartnerWhoIsAway()
    {
        _presence.Setup(p => p.IsOnline(Marko)).Returns(false);

        await _service.ProposeAsync(Ana, Marko);

        _email.Verify(e => e.SendRoommateProposalEmailAsync("marko@test.com", "Marko", "Ana Anić"), Times.Once);
    }

    [Fact]
    public async Task Propose_DoesNotEmailThePartnerWhoIsOnTheSite_ButStillNotifiesThem()
    {
        _presence.Setup(p => p.IsOnline(Marko)).Returns(true);

        await _service.ProposeAsync(Ana, Marko);

        _email.Verify(e => e.SendRoommateProposalEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _sent.Should().ContainSingle(n => n.RecipientUserId == Marko);
    }

    [Fact]
    public async Task Propose_Twice_EmailsOnlyOnce()
    {
        await _service.ProposeAsync(Ana, Marko);
        await _service.ProposeAsync(Ana, Marko);

        _email.Verify(e => e.SendRoommateProposalEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Propose_WhenTheMailFails_TheProposalIsStillSaved()
    {
        _email.Setup(e => e.SendRoommateProposalEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("mail down"));

        var result = await _service.ProposeAsync(Ana, Marko);

        result.Status.Should().Be(PairingStatus.Pending);
        (await _roommates.Pairings.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Accept_EmailsTheProposerWhoIsAway_AndOnlyThem()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);
        _email.Invocations.Clear();
        _presence.Setup(p => p.IsOnline(Ana)).Returns(false);

        await _service.AcceptAsync(Marko, proposal.PairingId);

        _email.Verify(e => e.SendRoommateAcceptedEmailAsync("ana@test.com", "Ana", "Marko Marković"), Times.Once);
        _email.Verify(e => e.SendRoommateAcceptedEmailAsync("marko@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Accept_DoesNotEmailAProposerWhoIsOnTheSite()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);
        _presence.Setup(p => p.IsOnline(Ana)).Returns(true);

        await _service.AcceptAsync(Marko, proposal.PairingId);

        _email.Verify(e => e.SendRoommateAcceptedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProposingBackToSomeoneWhoAskedFirst_EmailsThemTheAcceptance_NotAnotherProposal()
    {
        await _service.ProposeAsync(Marko, Ana);
        _email.Invocations.Clear();

        await _service.ProposeAsync(Ana, Marko);

        _email.Verify(e => e.SendRoommateAcceptedEmailAsync("marko@test.com", "Marko", "Ana Anić"), Times.Once);
        _email.Verify(e => e.SendRoommateProposalEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeclineAndEnd_SendNoMail()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);
        _email.Invocations.Clear();
        await _service.DeclineAsync(Marko, proposal.PairingId);

        var second = await _service.ProposeAsync(Ana, Jovana);
        await _service.AcceptAsync(Jovana, second.PairingId);
        _email.Invocations.Clear();
        await _service.EndAsync(Ana, second.PairingId);

        _email.Invocations.Should().BeEmpty();
    }

    // ── Answering ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Accept_MakesBothRoommates_AndTellsTheProposer()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);
        _sent.Clear();

        var result = await _service.AcceptAsync(Marko, proposal.PairingId);

        result.Status.Should().Be(PairingStatus.Accepted);
        (await _service.GetActivePartnerIdAsync(Ana)).Should().Be(Marko);
        (await _service.GetActivePartnerIdAsync(Marko)).Should().Be(Ana);
        _sent.Should().ContainSingle(n => n.RecipientUserId == Ana && n.ActionType == "pairing_accepted");
    }

    [Fact]
    public async Task Accept_ByTheProposer_IsForbidden()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);

        var act = () => _service.AcceptAsync(Ana, proposal.PairingId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Accept_Twice_IsRejected()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);
        await _service.AcceptAsync(Marko, proposal.PairingId);

        var act = () => _service.AcceptAsync(Marko, proposal.PairingId);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.PairingNotPending);
    }

    [Fact]
    public async Task Accept_VoidsOtherOpenProposalsOfBothPeople_AndTellsThoseWhoAsked()
    {
        var toMarko = await _service.ProposeAsync(Ana, Marko);     // the one that will be accepted
        var petarAsksMarko = await _service.ProposeAsync(Petar, Marko);
        var markoAsksJovana = await _service.ProposeAsync(Marko, Jovana);
        _sent.Clear();

        await _service.AcceptAsync(Marko, toMarko.PairingId);

        (await _roommates.Pairings.FindAsync(petarAsksMarko.PairingId))!.Status.Should().Be(PairingStatus.Cancelled);
        (await _roommates.Pairings.FindAsync(markoAsksJovana.PairingId))!.Status.Should().Be(PairingStatus.Cancelled);
        _sent.Should().Contain(n => n.RecipientUserId == Petar && n.ActionType == "pairing_void");
        _sent.Should().Contain(n => n.RecipientUserId == Jovana && n.ActionType == "pairing_void");
    }

    [Fact]
    public async Task Accept_WhenTheProposerPairedUpMeanwhile_IsRejected()
    {
        var anaAsksMarko = await _service.ProposeAsync(Ana, Marko);
        await Pair(Ana, Jovana); // Ana found someone else first

        var act = () => _service.AcceptAsync(Marko, anaAsksMarko.PairingId);

        // the proposal was voided when Ana paired up
        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.PairingNotPending);
    }

    [Fact]
    public async Task Decline_MarksItDeclined_AndTellsTheProposer()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);
        _sent.Clear();

        await _service.DeclineAsync(Marko, proposal.PairingId);

        (await _roommates.Pairings.FindAsync(proposal.PairingId))!.Status.Should().Be(PairingStatus.Declined);
        _sent.Should().ContainSingle(n => n.RecipientUserId == Ana && n.ActionType == "pairing_declined");
        (await _service.GetActivePartnerIdAsync(Ana)).Should().BeNull();
    }

    [Fact]
    public async Task Decline_ByTheProposer_IsForbidden()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);

        var act = () => _service.DeclineAsync(Ana, proposal.PairingId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Cancel_WithdrawsTheProposal_OnlyForTheProposer()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);

        var byOther = () => _service.CancelAsync(Marko, proposal.PairingId);
        await byOther.Should().ThrowAsync<ForbiddenException>();

        await _service.CancelAsync(Ana, proposal.PairingId);
        (await _roommates.Pairings.FindAsync(proposal.PairingId))!.Status.Should().Be(PairingStatus.Cancelled);
    }

    // ── State ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyState_SeparatesActiveIncomingAndOutgoing()
    {
        await Pair(Ana, Marko);
        await _service.ProposeAsync(Petar, Jovana);

        var anasState = await _service.GetMyStateAsync(Ana);
        anasState.Active!.PartnerName.Should().Be("Marko Marković");
        anasState.Incoming.Should().BeEmpty();
        anasState.Outgoing.Should().BeEmpty();

        var jovanasState = await _service.GetMyStateAsync(Jovana);
        jovanasState.Active.Should().BeNull();
        jovanasState.Incoming.Should().ContainSingle(p => p.PartnerUserId == Petar && p.IsIncoming);

        var petarsState = await _service.GetMyStateAsync(Petar);
        petarsState.Outgoing.Should().ContainSingle(p => p.PartnerUserId == Jovana && !p.IsIncoming);
    }

    // ── Ending ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task End_FreesBothPeople_AndTheyCanPairWithSomeoneElse()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);
        await _service.AcceptAsync(Marko, proposal.PairingId);
        _sent.Clear();

        await _service.EndAsync(Marko, proposal.PairingId);

        (await _service.GetActivePartnerIdAsync(Ana)).Should().BeNull();
        (await _service.GetActivePartnerIdAsync(Marko)).Should().BeNull();
        _sent.Should().ContainSingle(n => n.RecipientUserId == Ana && n.ActionType == "pairing_ended");

        await Pair(Ana, Jovana);
        (await _service.AreRoommatesAsync(Ana, Jovana)).Should().BeTrue();
    }

    [Fact]
    public async Task End_ByAStranger_IsForbidden_AndOnAPendingProposalIsRejected()
    {
        var proposal = await _service.ProposeAsync(Ana, Marko);

        var pending = () => _service.EndAsync(Ana, proposal.PairingId);
        (await pending.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.PairingNotActive);

        await _service.AcceptAsync(Marko, proposal.PairingId);
        var stranger = () => _service.EndAsync(Petar, proposal.PairingId);
        await stranger.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task EndAllForUser_EndsTheActivePairing_AndFreesTheRoommate()
    {
        await Pair(Ana, Marko);

        await _service.EndAllForUserAsync(Ana);

        (await _service.GetActivePartnerIdAsync(Marko)).Should().BeNull();
        (await _service.GetMyStateAsync(Ana)).Active.Should().BeNull();
    }

    [Fact]
    public async Task EndAllForUser_CancelsOpenProposalsInBothDirections()
    {
        await _service.ProposeAsync(Ana, Marko);
        await _service.ProposeAsync(Petar, Ana);

        await _service.EndAllForUserAsync(Ana);

        var state = await _service.GetMyStateAsync(Ana);
        state.Incoming.Should().BeEmpty();
        state.Outgoing.Should().BeEmpty();
        (await _service.GetMyStateAsync(Marko)).Incoming.Should().BeEmpty();
    }

    // ── Roommate search ──────────────────────────────────────────────────────

    [Fact]
    public async Task Search_HidesPeopleWhoAgreedToBeRoommates_UntilThePairingEnds()
    {
        await SeedRoommateProfile(Ana);
        await SeedRoommateProfile(Marko);
        await SeedRoommateProfile(Jovana);
        (await ListedUserIds()).Should().BeEquivalentTo(new[] { Ana, Marko, Jovana }); // also fills the list cache

        var proposal = await _service.ProposeAsync(Ana, Marko);
        (await ListedUserIds()).Should().HaveCount(3, "a proposal alone does not take anybody");

        await _service.AcceptAsync(Marko, proposal.PairingId);
        (await ListedUserIds()).Should().BeEquivalentTo(new[] { Jovana });

        await _service.EndAsync(Ana, proposal.PairingId);
        (await ListedUserIds()).Should().BeEquivalentTo(new[] { Ana, Marko, Jovana });
    }

    [Fact]
    public async Task Search_CanStillIncludeTakenPeople_MarkedAsTaken()
    {
        await SeedRoommateProfile(Ana);
        await SeedRoommateProfile(Jovana);
        await Pair(Ana, Marko);

        var page = await _roommateService.GetAllRoommatesAsync(new RoommateFilterQuery { IncludeTaken = true });

        page.Items.Single(r => r.UserId == Ana).IsTaken.Should().BeTrue();
        page.Items.Single(r => r.UserId == Jovana).IsTaken.Should().BeFalse();
    }

    [Fact]
    public async Task ProfileLookup_ReportsWhetherThePersonIsTaken()
    {
        await SeedRoommateProfile(Ana);
        await SeedRoommateProfile(Jovana);
        await Pair(Ana, Marko);

        (await _roommateService.GetRoommateByUserIdAsync(Ana))!.IsTaken.Should().BeTrue();
        (await _roommateService.GetRoommateByUserIdAsync(Jovana))!.IsTaken.Should().BeFalse();
    }

    // ── Shared apartment list ────────────────────────────────────────────────

    [Fact]
    public async Task ShareApartment_WithoutARoommate_IsRejected()
    {
        var apartment = await SeedApartment();

        var act = () => _service.ShareApartmentAsync(Ana, apartment.ApartmentId, null);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.PairingRequired);
    }

    [Fact]
    public async Task ShareApartment_AddsItToTheSharedList_AndTellsTheRoommate()
    {
        await Pair(Ana, Marko);
        var apartment = await SeedApartment("Stan na Zvezdari");
        _sent.Clear();

        var item = await _service.ShareApartmentAsync(Ana, apartment.ApartmentId, "Blizu posla");

        item.Title.Should().Be("Stan na Zvezdari");
        item.AddedByName.Should().Be("Ana Anić");
        item.Note.Should().Be("Blizu posla");
        item.IsAvailable.Should().BeTrue();
        _sent.Should().ContainSingle(n => n.RecipientUserId == Marko && n.ActionType == "pairing_apartment");

        // both of them see the same list
        (await _service.GetSharedApartmentsAsync(Marko)).Should().ContainSingle(a => a.ApartmentId == apartment.ApartmentId);
        (await _service.GetSharedApartmentsAsync(Ana)).Should().ContainSingle();
    }

    [Fact]
    public async Task ShareApartment_NotificationCarriesTheApartmentTitleForTheTranslation()
    {
        await Pair(Ana, Marko);
        var apartment = await SeedApartment("Stan na Zvezdari");
        _sent.Clear();

        await _service.ShareApartmentAsync(Ana, apartment.ApartmentId, null);

        var notification = _sent.Single();
        notification.Key.Should().Be("pairing_apartment");
        notification.Params.Should().Contain("name", "Ana Anić").And.Contain("title", "Stan na Zvezdari");
    }

    [Fact]
    public async Task SharedList_NamesTheLandlord_SoThePairCanTalkToThemTogether()
    {
        await Pair(Ana, Marko);
        var apartment = await SeedApartment();
        apartment.LandlordId = Petar;
        await _listings.SaveChangesAsync();
        await _service.ShareApartmentAsync(Ana, apartment.ApartmentId, null);

        var item = (await _service.GetSharedApartmentsAsync(Marko)).Single();

        item.LandlordUserId.Should().Be(Petar);
        item.LandlordName.Should().Be("Petar Petrović");
    }

    [Fact]
    public async Task IsLandlordOfSharedApartment_IsTrueOnlyForTheLandlordOfAnApartmentOnTheList_AndOnlyForTheTwoRoommates()
    {
        await Pair(Ana, Marko);
        var onList = await SeedApartment("Na listi");
        onList.LandlordId = Petar;
        var notOnList = await SeedApartment("Nije na listi");
        notOnList.LandlordId = Jovana;
        await _listings.SaveChangesAsync();
        await _service.ShareApartmentAsync(Ana, onList.ApartmentId, null);

        (await _service.IsLandlordOfSharedApartmentAsync(Ana, Petar)).Should().BeTrue();
        (await _service.IsLandlordOfSharedApartmentAsync(Marko, Petar)).Should().BeTrue("the list is shared");
        (await _service.IsLandlordOfSharedApartmentAsync(Ana, Jovana)).Should().BeFalse("their apartment was never shared");
        (await _service.IsLandlordOfSharedApartmentAsync(Jovana, Petar)).Should().BeFalse("Jovana has no roommate");
    }

    [Fact]
    public async Task IsLandlordOfSharedApartment_IsFalseOnceTheListIsEmptyOrTheListingIsDeleted()
    {
        await Pair(Ana, Marko);
        var apartment = await SeedApartment();
        apartment.LandlordId = Petar;
        await _listings.SaveChangesAsync();
        (await _service.IsLandlordOfSharedApartmentAsync(Ana, Petar)).Should().BeFalse("nothing is shared yet");

        await _service.ShareApartmentAsync(Ana, apartment.ApartmentId, null);
        apartment.IsDeleted = true;
        await _listings.SaveChangesAsync();

        (await _service.IsLandlordOfSharedApartmentAsync(Ana, Petar)).Should().BeFalse();
    }

    [Fact]
    public async Task ShareApartment_Twice_KeepsOneEntryAndDoesNotNotifyAgain()
    {
        await Pair(Ana, Marko);
        var apartment = await SeedApartment();
        await _service.ShareApartmentAsync(Ana, apartment.ApartmentId, null);
        _sent.Clear();

        await _service.ShareApartmentAsync(Marko, apartment.ApartmentId, "Meni se sviđa");

        var list = await _service.GetSharedApartmentsAsync(Ana);
        list.Should().ContainSingle();
        list[0].Note.Should().Be("Meni se sviđa");
        list[0].AddedByName.Should().Be("Ana Anić", "the first person stays the one who added it");
        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task ShareApartment_UnknownOrDeleted_IsNotFound()
    {
        await Pair(Ana, Marko);
        var deleted = await SeedApartment(deleted: true);

        await FluentActions.Awaiting(() => _service.ShareApartmentAsync(Ana, 999, null)).Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => _service.ShareApartmentAsync(Ana, deleted.ApartmentId, null)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SharedList_FlagsListingsThatAreNoLongerAvailable()
    {
        await Pair(Ana, Marko);
        var apartment = await SeedApartment();
        await _service.ShareApartmentAsync(Ana, apartment.ApartmentId, null);

        apartment.IsActive = false;
        await _listings.SaveChangesAsync();

        (await _service.GetSharedApartmentsAsync(Ana)).Single().IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveSharedApartment_WorksForEitherRoommate_AndNotForOutsiders()
    {
        await Pair(Ana, Marko);
        var apartment = await SeedApartment();
        await _service.ShareApartmentAsync(Ana, apartment.ApartmentId, null);

        (await _service.RemoveSharedApartmentAsync(Petar, apartment.ApartmentId)).Should().BeFalse();
        (await _service.RemoveSharedApartmentAsync(Marko, apartment.ApartmentId)).Should().BeTrue();
        (await _service.GetSharedApartmentsAsync(Ana)).Should().BeEmpty();
    }

    [Fact]
    public async Task SharedList_StartsEmptyAfterANewPairing()
    {
        await Pair(Ana, Marko);
        var apartment = await SeedApartment();
        await _service.ShareApartmentAsync(Ana, apartment.ApartmentId, null);
        var old = (await _service.GetMyStateAsync(Ana)).Active!;
        await _service.EndAsync(Ana, old.PairingId);

        await Pair(Ana, Jovana);

        (await _service.GetSharedApartmentsAsync(Ana)).Should().BeEmpty();
    }
}
