using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Common.Exceptions;
using Lander.src.Common.Validation;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Roommates.Dtos.Dto;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Roommates.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Lander.src.Modules.Roommates.Implementation;

public class RoommatePairingService : IRoommatePairingService
{
    private const string PairingPage = "/my-roommate";
    private const int MaxNoteLength = 500;

    private readonly RoommatesContext _context;
    private readonly UsersContext _usersContext;
    private readonly ListingsContext _listingsContext;
    private readonly INotificationService _notifications;
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;
    private readonly IPresenceTracker _presence;
    private readonly ILogger<RoommatePairingService> _logger;

    public RoommatePairingService(
        RoommatesContext context,
        UsersContext usersContext,
        ListingsContext listingsContext,
        INotificationService notifications,
        IMemoryCache cache,
        IEmailService emailService,
        IPresenceTracker presence,
        ILogger<RoommatePairingService> logger)
    {
        _context = context;
        _usersContext = usersContext;
        _listingsContext = listingsContext;
        _notifications = notifications;
        _cache = cache;
        _emailService = emailService;
        _presence = presence;
        _logger = logger;
    }

    // ── Pairing ──────────────────────────────────────────────────────────────

    public async Task<MyPairingStateDto> GetMyStateAsync(int userId)
    {
        var rows = await _context.Pairings
            .AsNoTracking()
            .Where(p => (p.RequesterUserId == userId || p.PartnerUserId == userId)
                        && (p.Status == PairingStatus.Pending || p.Status == PairingStatus.Accepted))
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();

        var people = await LoadPeopleAsync(rows.Select(p => OtherUser(p, userId)));

        var state = new MyPairingStateDto();
        foreach (var row in rows)
        {
            var dto = ToDto(row, userId, people);
            if (row.Status == PairingStatus.Accepted) state.Active ??= dto;
            else if (dto.IsIncoming) state.Incoming.Add(dto);
            else state.Outgoing.Add(dto);
        }
        return state;
    }

    public async Task<PairingDto> ProposeAsync(int userId, int partnerUserId)
    {
        if (userId == partnerUserId)
            throw new BusinessRuleException(BusinessErrorCodes.PairingWithSelf);

        var partnerExists = await _usersContext.Users.AsNoTracking()
            .AnyAsync(u => u.UserId == partnerUserId && u.IsActive);
        if (!partnerExists) throw new NotFoundException("User", partnerUserId);

        RoommatePairing pairing = null!;
        var confirmed = new ConfirmResult();
        var isNewProposal = false;

        await _context.RunInTransactionAsync(async () =>
        {
            confirmed = new ConfirmResult();
            isNewProposal = false;
            await EnsureBothFreeAsync(userId, partnerUserId);

            // They already asked me: asking back means yes.
            var theirs = await _context.Pairings.FirstOrDefaultAsync(p =>
                p.RequesterUserId == partnerUserId && p.PartnerUserId == userId && p.Status == PairingStatus.Pending);
            if (theirs != null)
            {
                pairing = theirs;
                confirmed = await ConfirmAsync(theirs);
                return;
            }

            var mine = await _context.Pairings.FirstOrDefaultAsync(p =>
                p.RequesterUserId == userId && p.PartnerUserId == partnerUserId && p.Status == PairingStatus.Pending);
            if (mine != null)
            {
                pairing = mine;
                return;
            }

            pairing = new RoommatePairing
            {
                RequesterUserId = userId,
                PartnerUserId = partnerUserId,
                Status = PairingStatus.Pending,
                CreatedDate = DateTime.UtcNow,
            };
            _context.Pairings.Add(pairing);
            await _context.SaveEntitiesAsync();
            isNewProposal = true;
        });

        if (confirmed.Confirmed) await AfterConfirmedAsync(pairing, userId, confirmed);
        else if (isNewProposal)
        {
            var proposerName = await NameOfAsync(userId);
            await NotifyAsync(userId, proposerName, partnerUserId,
                "Predlog za cimerstvo",
                $"{proposerName} vam predlaže da budete cimeri.",
                "pairing_proposed");
            await EmailIfAwayAsync(partnerUserId, (to, firstName) =>
                _emailService.SendRoommateProposalEmailAsync(to, firstName, proposerName));
        }

        var people = await LoadPeopleAsync(new[] { partnerUserId });
        return ToDto(pairing, userId, people);
    }

    public async Task<PairingDto> AcceptAsync(int userId, int pairingId)
    {
        var pairing = await _context.Pairings.FirstOrDefaultAsync(p => p.PairingId == pairingId)
            ?? throw new NotFoundException("Pairing", pairingId);
        if (pairing.PartnerUserId != userId)
            throw new ForbiddenException("Only the person who was asked can accept.");
        if (pairing.Status != PairingStatus.Pending)
            throw new BusinessRuleException(BusinessErrorCodes.PairingNotPending);

        var confirmed = new ConfirmResult();
        await _context.RunInTransactionAsync(async () =>
        {
            await EnsureBothFreeAsync(pairing.RequesterUserId, pairing.PartnerUserId);
            confirmed = await ConfirmAsync(pairing);
        });
        await AfterConfirmedAsync(pairing, userId, confirmed);

        var people = await LoadPeopleAsync(new[] { pairing.RequesterUserId });
        return ToDto(pairing, userId, people);
    }

    public async Task DeclineAsync(int userId, int pairingId)
    {
        var pairing = await _context.Pairings.FirstOrDefaultAsync(p => p.PairingId == pairingId)
            ?? throw new NotFoundException("Pairing", pairingId);
        if (pairing.PartnerUserId != userId)
            throw new ForbiddenException("Only the person who was asked can decline.");
        if (pairing.Status != PairingStatus.Pending)
            throw new BusinessRuleException(BusinessErrorCodes.PairingNotPending);

        pairing.Status = PairingStatus.Declined;
        pairing.RespondedDate = DateTime.UtcNow;
        await _context.RunInTransactionAsync(() => _context.SaveEntitiesAsync());

        var declinedBy = await NameOfAsync(userId);
        await NotifyAsync(userId, declinedBy, pairing.RequesterUserId,
            "Predlog za cimerstvo odbijen",
            $"{declinedBy} je odbio/la vaš predlog da budete cimeri.",
            "pairing_declined");
    }

    public async Task CancelAsync(int userId, int pairingId)
    {
        var pairing = await _context.Pairings.FirstOrDefaultAsync(p => p.PairingId == pairingId)
            ?? throw new NotFoundException("Pairing", pairingId);
        if (pairing.RequesterUserId != userId)
            throw new ForbiddenException("Only the person who proposed can withdraw it.");
        if (pairing.Status != PairingStatus.Pending)
            throw new BusinessRuleException(BusinessErrorCodes.PairingNotPending);

        pairing.Status = PairingStatus.Cancelled;
        pairing.RespondedDate = DateTime.UtcNow;
        await _context.RunInTransactionAsync(() => _context.SaveEntitiesAsync());
    }

    public async Task EndAsync(int userId, int pairingId)
    {
        var pairing = await _context.Pairings.FirstOrDefaultAsync(p => p.PairingId == pairingId)
            ?? throw new NotFoundException("Pairing", pairingId);
        if (pairing.RequesterUserId != userId && pairing.PartnerUserId != userId)
            throw new ForbiddenException("You are not part of this pairing.");
        if (pairing.Status != PairingStatus.Accepted)
            throw new BusinessRuleException(BusinessErrorCodes.PairingNotActive);

        pairing.Status = PairingStatus.Ended;
        pairing.EndedDate = DateTime.UtcNow;
        await _context.RunInTransactionAsync(() => _context.SaveEntitiesAsync());
        RoommateListCache.Invalidate(_cache);

        var endedBy = await NameOfAsync(userId);
        await NotifyAsync(userId, endedBy, OtherUser(pairing, userId),
            "Cimerstvo je prekinuto",
            $"{endedBy} je prekinuo/la cimerstvo. Ponovo ste dostupni drugim cimerima.",
            "pairing_ended");
    }

    public async Task EndAllForUserAsync(int userId)
    {
        var open = await _context.Pairings
            .Where(p => (p.RequesterUserId == userId || p.PartnerUserId == userId)
                        && (p.Status == PairingStatus.Pending || p.Status == PairingStatus.Accepted))
            .ToListAsync();
        if (open.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var pairing in open)
        {
            if (pairing.Status == PairingStatus.Accepted)
            {
                pairing.Status = PairingStatus.Ended;
                pairing.EndedDate = now;
            }
            else
            {
                pairing.Status = PairingStatus.Cancelled;
                pairing.RespondedDate = now;
            }
        }
        await _context.RunInTransactionAsync(() => _context.SaveEntitiesAsync());
        RoommateListCache.Invalidate(_cache);
    }

    public async Task<int?> GetActivePartnerIdAsync(int userId)
    {
        var pairing = await _context.Pairings.AsNoTracking()
            .Where(p => p.Status == PairingStatus.Accepted
                        && (p.RequesterUserId == userId || p.PartnerUserId == userId))
            .Select(p => new { p.RequesterUserId, p.PartnerUserId })
            .FirstOrDefaultAsync();

        return pairing is null ? null : (pairing.RequesterUserId == userId ? pairing.PartnerUserId : pairing.RequesterUserId);
    }

    public async Task<bool> AreRoommatesAsync(int userId, int otherUserId) =>
        await _context.Pairings.AsNoTracking().AnyAsync(p =>
            p.Status == PairingStatus.Accepted
            && ((p.RequesterUserId == userId && p.PartnerUserId == otherUserId)
                || (p.RequesterUserId == otherUserId && p.PartnerUserId == userId)));

    public async Task<bool> IsLandlordOfSharedApartmentAsync(int userId, int landlordUserId)
    {
        var pairing = await GetActivePairingAsync(userId);
        if (pairing is null) return false;

        var apartmentIds = await _context.PairingApartments.AsNoTracking()
            .Where(a => a.PairingId == pairing.PairingId)
            .Select(a => a.ApartmentId)
            .ToListAsync();
        if (apartmentIds.Count == 0) return false;

        return await _listingsContext.Apartments.AsNoTracking()
            .AnyAsync(a => apartmentIds.Contains(a.ApartmentId) && !a.IsDeleted && a.LandlordId == landlordUserId);
    }

    // ── Shared apartment list ────────────────────────────────────────────────

    public async Task<List<SharedApartmentDto>> GetSharedApartmentsAsync(int userId)
    {
        var pairing = await GetActivePairingAsync(userId);
        if (pairing is null) return new List<SharedApartmentDto>();

        var items = await _context.PairingApartments.AsNoTracking()
            .Where(a => a.PairingId == pairing.PairingId)
            .OrderByDescending(a => a.AddedDate)
            .ToListAsync();
        return await ToSharedDtosAsync(items);
    }

    public async Task<SharedApartmentDto> ShareApartmentAsync(int userId, int apartmentId, string? note)
    {
        var pairing = await GetActivePairingAsync(userId)
            ?? throw new BusinessRuleException(BusinessErrorCodes.PairingRequired);

        var apartment = await _listingsContext.Apartments.AsNoTracking()
            .Where(a => a.ApartmentId == apartmentId && !a.IsDeleted)
            .Select(a => new { a.ApartmentId, a.Title })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Apartment", apartmentId);

        var cleanNote = CleanNote(note);

        var item = await _context.PairingApartments
            .FirstOrDefaultAsync(a => a.PairingId == pairing.PairingId && a.ApartmentId == apartmentId);
        var isNew = item is null;
        if (item is null)
        {
            item = new PairingApartment
            {
                PairingId = pairing.PairingId,
                ApartmentId = apartmentId,
                AddedByUserId = userId,
                Note = cleanNote,
                AddedDate = DateTime.UtcNow,
            };
            _context.PairingApartments.Add(item);
        }
        else if (cleanNote != null)
        {
            item.Note = cleanNote;
        }
        await _context.RunInTransactionAsync(() => _context.SaveEntitiesAsync());

        if (isNew)
        {
            var sharedBy = await NameOfAsync(userId);
            await NotifyAsync(userId, sharedBy, OtherUser(pairing, userId),
                "Novi stan na zajedničkoj listi",
                $"{sharedBy} je podelio/la stan „{apartment.Title}“ sa vama.",
                "pairing_apartment",
                apartmentTitle: apartment.Title);
        }

        return (await ToSharedDtosAsync(new List<PairingApartment> { item }))[0];
    }

    public async Task<bool> RemoveSharedApartmentAsync(int userId, int apartmentId)
    {
        var pairing = await GetActivePairingAsync(userId);
        if (pairing is null) return false;

        var item = await _context.PairingApartments
            .FirstOrDefaultAsync(a => a.PairingId == pairing.PairingId && a.ApartmentId == apartmentId);
        if (item is null) return false;

        _context.PairingApartments.Remove(item);
        await _context.RunInTransactionAsync(() => _context.SaveEntitiesAsync());
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static int OtherUser(RoommatePairing p, int userId) =>
        p.RequesterUserId == userId ? p.PartnerUserId : p.RequesterUserId;

    private Task<RoommatePairing?> GetActivePairingAsync(int userId) =>
        _context.Pairings.AsNoTracking().FirstOrDefaultAsync(p =>
            p.Status == PairingStatus.Accepted && (p.RequesterUserId == userId || p.PartnerUserId == userId));

    /// <summary>Neither side may already have a roommate (the signed-in user gets the message about themselves first).</summary>
    private async Task EnsureBothFreeAsync(int userId, int otherUserId)
    {
        var taken = await _context.Pairings.AsNoTracking()
            .Where(p => p.Status == PairingStatus.Accepted
                        && (p.RequesterUserId == userId || p.PartnerUserId == userId
                            || p.RequesterUserId == otherUserId || p.PartnerUserId == otherUserId))
            .ToListAsync();

        if (taken.Any(p => p.RequesterUserId == userId || p.PartnerUserId == userId))
            throw new BusinessRuleException(BusinessErrorCodes.YouAlreadyHaveRoommate);
        if (taken.Count > 0)
            throw new BusinessRuleException(BusinessErrorCodes.RoommateAlreadyTaken);
    }

    /// <summary>What <see cref="ConfirmAsync"/> changed, handed on so the notifications can go out after the commit.</summary>
    private sealed class ConfirmResult
    {
        public bool Confirmed { get; init; }
        public List<RoommatePairing> Voided { get; init; } = new();
    }

    /// <summary>Marks the pairing accepted and voids every other open proposal of either person.</summary>
    private async Task<ConfirmResult> ConfirmAsync(RoommatePairing pairing)
    {
        pairing.Status = PairingStatus.Accepted;
        pairing.RespondedDate = DateTime.UtcNow;

        var a = pairing.RequesterUserId;
        var b = pairing.PartnerUserId;
        var others = await _context.Pairings
            .Where(p => p.PairingId != pairing.PairingId && p.Status == PairingStatus.Pending
                        && (p.RequesterUserId == a || p.PartnerUserId == a || p.RequesterUserId == b || p.PartnerUserId == b))
            .ToListAsync();
        foreach (var other in others)
        {
            other.Status = PairingStatus.Cancelled;
            other.RespondedDate = DateTime.UtcNow;
        }

        await _context.SaveEntitiesAsync();
        return new ConfirmResult { Confirmed = true, Voided = others };
    }

    /// <summary>Cache, bell and courtesy notes: done after the commit so they can never roll the pairing back.</summary>
    private async Task AfterConfirmedAsync(RoommatePairing pairing, int acceptedByUserId, ConfirmResult confirmed)
    {
        RoommateListCache.Invalidate(_cache);

        var a = pairing.RequesterUserId;
        var b = pairing.PartnerUserId;
        var proposer = acceptedByUserId == a ? b : a;

        var acceptorName = await NameOfAsync(acceptedByUserId);
        await NotifyAsync(acceptedByUserId, acceptorName, proposer,
            "Cimerstvo potvrđeno 🎉",
            $"{acceptorName} je prihvatio/la. Vas dvoje ste sada cimeri i niste više dostupni drugima.",
            "pairing_accepted");
        await EmailIfAwayAsync(proposer, (to, firstName) =>
            _emailService.SendRoommateAcceptedEmailAsync(to, firstName, acceptorName));

        // Whoever else had asked one of the two is told that the proposal is no longer open.
        foreach (var voided in confirmed.Voided)
        {
            var askedOneOfPair = voided.PartnerUserId == a || voided.PartnerUserId == b;
            var asker = askedOneOfPair ? voided.RequesterUserId : voided.PartnerUserId;
            var target = askedOneOfPair ? voided.PartnerUserId : voided.RequesterUserId;
            if (asker == a || asker == b) continue; // the other half of the pair: they know already
            var targetName = await NameOfAsync(target);
            await NotifyAsync(target, targetName, asker,
                "Predlog za cimerstvo više nije aktuelan",
                $"{targetName} je u međuvremenu našao/la cimera.",
                "pairing_void");
        }
    }

    /// <summary>
    /// Title and message are the Serbian fallback; the action type doubles as the translation key the
    /// browser uses (with the name of whoever did it) to show the notification in the user's language.
    /// The caller passes the sender's name it already read for the Serbian message, so it is looked up once.
    /// </summary>
    private async Task NotifyAsync(
        int senderUserId, string senderName, int recipientUserId, string title, string message, string actionType,
        string? apartmentTitle = null)
    {
        try
        {
            var parameters = new Dictionary<string, string> { ["name"] = senderName };
            if (apartmentTitle is not null) parameters["title"] = apartmentTitle;

            await _notifications.SendNotificationAsync(new CreateNotificationInputDto
            {
                Title = title,
                Message = message,
                ActionType = actionType,
                ActionTarget = PairingPage,
                SenderUserId = senderUserId,
                RecipientUserId = recipientUserId,
                Key = actionType,
                Params = parameters,
            });
        }
        catch (Exception ex)
        {
            // The pairing itself is saved; a failed bell must not turn that into an error.
            _logger.LogWarning(ex, "Could not send pairing notification {ActionType} to user {UserId}", actionType, recipientUserId);
        }
    }

    /// <summary>
    /// The bell reaches only people who are on the site. Somebody who is away is told by mail too, so a proposal
    /// or its answer is not found by chance days later. A failing mail never fails the pairing.
    /// </summary>
    private async Task EmailIfAwayAsync(int recipientUserId, Func<string, string, Task<bool>> send)
    {
        if (_presence.IsOnline(recipientUserId)) return;
        try
        {
            var recipient = await _usersContext.Users.AsNoTracking()
                .Where(u => u.UserId == recipientUserId)
                .Select(u => new { u.Email, u.FirstName })
                .FirstOrDefaultAsync();
            if (recipient is null || string.IsNullOrEmpty(recipient.Email)) return;

            await send(recipient.Email, recipient.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not e-mail user {UserId} about a roommate agreement", recipientUserId);
        }
    }

    private async Task<string> NameOfAsync(int userId)
    {
        var user = await _usersContext.Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstOrDefaultAsync();
        return user is null ? "Neko" : $"{user.FirstName} {user.LastName}".Trim();
    }

    private async Task<Dictionary<int, User>> LoadPeopleAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, User>();
        return await _usersContext.Users.AsNoTracking()
            .Where(u => ids.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);
    }

    private static PairingDto ToDto(RoommatePairing pairing, int viewerId, Dictionary<int, User> people)
    {
        var otherId = OtherUser(pairing, viewerId);
        people.TryGetValue(otherId, out var other);
        return new PairingDto
        {
            PairingId = pairing.PairingId,
            Status = pairing.Status,
            PartnerUserId = otherId,
            PartnerName = other is null ? string.Empty : $"{other.FirstName} {other.LastName}".Trim(),
            PartnerProfilePicture = other?.ProfilePicture,
            IsIncoming = pairing.PartnerUserId == viewerId,
            CreatedDate = pairing.CreatedDate,
            RespondedDate = pairing.RespondedDate,
        };
    }

    private async Task<List<SharedApartmentDto>> ToSharedDtosAsync(List<PairingApartment> items)
    {
        var apartmentIds = items.Select(i => i.ApartmentId).Distinct().ToList();
        var apartments = await _listingsContext.Apartments.AsNoTracking()
            .Where(a => apartmentIds.Contains(a.ApartmentId))
            .Select(a => new { a.ApartmentId, a.Title, a.City, a.Address, a.Rent, a.Price, a.IsActive, a.IsDeleted, a.LandlordId })
            .ToDictionaryAsync(a => a.ApartmentId);
        var landlordIds = apartments.Values.Where(a => a.LandlordId.HasValue).Select(a => a.LandlordId!.Value);
        var people = await LoadPeopleAsync(items.Select(i => i.AddedByUserId).Concat(landlordIds));

        return items.Select(i =>
        {
            apartments.TryGetValue(i.ApartmentId, out var apartment);
            people.TryGetValue(i.AddedByUserId, out var adder);
            User? landlord = null;
            if (apartment?.LandlordId is int landlordId) people.TryGetValue(landlordId, out landlord);
            return new SharedApartmentDto
            {
                ApartmentId = i.ApartmentId,
                Title = apartment?.Title ?? string.Empty,
                City = apartment?.City,
                Address = apartment?.Address,
                Rent = apartment?.Rent ?? 0,
                Price = apartment?.Price,
                LandlordUserId = apartment?.LandlordId,
                LandlordName = landlord is null ? null : $"{landlord.FirstName} {landlord.LastName}".Trim(),
                IsAvailable = apartment is { IsDeleted: false, IsActive: true },
                AddedByUserId = i.AddedByUserId,
                AddedByName = adder is null ? string.Empty : $"{adder.FirstName} {adder.LastName}".Trim(),
                Note = i.Note,
                AddedDate = i.AddedDate,
            };
        }).ToList();
    }

    private static string? CleanNote(string? note)
    {
        var clean = HtmlSanitizationHelper.SanitizePlainText(note)?.Trim();
        if (string.IsNullOrEmpty(clean)) return null;
        return clean.Length > MaxNoteLength ? clean[..MaxNoteLength] : clean;
    }
}
