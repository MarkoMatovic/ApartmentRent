using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Common.Exceptions;
using Lander.src.Common.Validation;
using Lander.src.Modules.Billing.Dtos;
using Lander.src.Modules.Billing.Models;
using Lander.src.Modules.Roommates.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Billing.Implementation;

public interface ITenancyService
{
    Task<List<TenancyDto>> GetMineAsync(int userId);
    Task<TenancyDto> GetAsync(int userId, int tenancyId);
    /// <summary>Who the landlord can add: people with an approved application for the apartment, and their roommates.</summary>
    Task<List<TenancyCandidateDto>> GetCandidatesAsync(int userId, int apartmentId);
    Task<TenancyDto> CreateAsync(int userId, CreateTenancyInputDto input);
    Task<TenancyDto> UpdateAsync(int userId, int tenancyId, UpdateTenancyInputDto input);
    Task<TenancyDto> AddTenantAsync(int userId, int tenancyId, int tenantUserId);
    /// <summary>The landlord removes a tenant, or a tenant leaves (<paramref name="tenantUserId"/> is themselves).</summary>
    Task<TenancyDto> RemoveTenantAsync(int userId, int tenancyId, int tenantUserId);
    Task<TenancyDto> EndAsync(int userId, int tenancyId);
}

public sealed class TenancyService : ITenancyService
{
    private readonly BillingContext _db;
    private readonly ListingsContext _listings;
    private readonly ApplicationsContext _applications;
    private readonly IRoommatePairingService _pairings;
    private readonly BillingPeople _people;
    private readonly BillingNotifier _notifier;
    private readonly TimeProvider _time;

    public TenancyService(
        BillingContext db,
        ListingsContext listings,
        ApplicationsContext applications,
        IRoommatePairingService pairings,
        BillingPeople people,
        BillingNotifier notifier,
        TimeProvider time)
    {
        _db = db;
        _listings = listings;
        _applications = applications;
        _pairings = pairings;
        _people = people;
        _notifier = notifier;
        _time = time;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    public async Task<List<TenancyDto>> GetMineAsync(int userId)
    {
        var tenancies = await _db.Tenancies.AsNoTracking()
            .Include(t => t.Members)
            .Where(t => t.LandlordUserId == userId || t.Members.Any(m => m.UserId == userId))
            .OrderBy(t => t.Status).ThenByDescending(t => t.CreatedDate)
            .ToListAsync();
        if (tenancies.Count == 0) return new();

        var ids = tenancies.Select(t => t.TenancyId).ToList();
        var openBills = await OpenBillCountsAsync(ids);
        var people = await _people.LoadAsync(tenancies.SelectMany(t => t.Members.Select(m => m.UserId).Append(t.LandlordUserId)));
        return tenancies
            .Select(t => BillingMapper.ToDto(t, userId, people, openBills.GetValueOrDefault(t.TenancyId)))
            .ToList();
    }

    public async Task<TenancyDto> GetAsync(int userId, int tenancyId)
    {
        var tenancy = await LoadAsync(tenancyId, tracking: false);
        EnsureCanView(tenancy, userId);
        return await ToDtoAsync(tenancy, userId);
    }

    public async Task<List<TenancyCandidateDto>> GetCandidatesAsync(int userId, int apartmentId)
    {
        await LoadOwnApartmentAsync(userId, apartmentId);
        var candidates = await CandidatesAsync(apartmentId, userId);

        var active = await _db.TenancyMembers.AsNoTracking()
            .Where(m => m.LeftDate == null && m.Tenancy.ApartmentId == apartmentId && m.Tenancy.Status == TenancyStatus.Active)
            .Select(m => m.UserId)
            .ToListAsync();
        var people = await _people.LoadAsync(candidates.Keys);
        return candidates
            .Where(c => !active.Contains(c.Key))
            .Select(c =>
            {
                var person = BillingPeople.Of(people, c.Key);
                return new TenancyCandidateDto { UserId = c.Key, Name = person.Name, ProfilePicture = person.ProfilePicture, Reason = c.Value };
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    public async Task<TenancyDto> CreateAsync(int userId, CreateTenancyInputDto input)
    {
        var apartment = await LoadOwnApartmentAsync(userId, input.ApartmentId);
        if (await _db.Tenancies.AnyAsync(t => t.ApartmentId == input.ApartmentId && t.Status == TenancyStatus.Active))
            throw new BusinessRuleException(BusinessErrorCodes.TenancyAlreadyActive);

        var tenantIds = input.TenantUserIds.Distinct().ToList();
        if (tenantIds.Contains(userId)) throw new BusinessRuleException(BusinessErrorCodes.LandlordCannotBeTenant);
        var candidates = await CandidatesAsync(input.ApartmentId, userId);
        if (tenantIds.Any(id => !candidates.ContainsKey(id)))
            throw new BusinessRuleException(BusinessErrorCodes.TenantNotEligible);

        var now = Now;
        var rentBills = new List<Bill>();
        var tenancy = new Tenancy
        {
            ApartmentId = apartment.ApartmentId,
            LandlordUserId = userId,
            ApartmentTitle = apartment.Title.Length > 200 ? apartment.Title[..200] : apartment.Title,
            MonthlyRent = input.MonthlyRent,
            RentDueDay = input.RentDueDay,
            PayeeName = input.PayeeName.Trim(),
            PayeeAccount = SerbianAccount.Normalize(input.PayeeAccount)!,
            StartDate = input.StartDate,
            Status = TenancyStatus.Active,
            CreatedDate = now,
            Members = tenantIds.Select(id => new TenancyMember { UserId = id, JoinedDate = now }).ToList(),
        };

        try
        {
            await _db.RunInTransactionAsync(async () =>
            {
                _db.Tenancies.Add(tenancy);
                await _db.SaveEntitiesAsync();
                // This month's rent, when it is already due within the lead time.
                rentBills = RentBills.AddMissing(_db, tenancy, new HashSet<string>(), BillingCalendar.Today(_time), BillingCalendar.Today(_time), now);
                await _db.SaveEntitiesAsync();
            });
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Two tenancies for one apartment at the same moment: the unique index lets one through.
            throw new BusinessRuleException(BusinessErrorCodes.TenancyAlreadyActive);
        }

        var landlordName = await _notifier.NameOfAsync(userId);
        foreach (var tenantId in tenantIds)
            await NotifyAddedAsync(tenancy, tenantId, landlordName);
        foreach (var bill in rentBills)
            await _notifier.NotifyRentBillAsync(bill);

        return await GetAsync(userId, tenancy.TenancyId);
    }

    public async Task<TenancyDto> UpdateAsync(int userId, int tenancyId, UpdateTenancyInputDto input)
    {
        var tenancy = await LoadAsync(tenancyId, tracking: true);
        EnsureLandlord(tenancy, userId);
        EnsureActive(tenancy);

        // Bills already made keep their amounts; the next months follow the new settings.
        tenancy.MonthlyRent = input.MonthlyRent;
        tenancy.RentDueDay = input.RentDueDay;
        tenancy.PayeeName = input.PayeeName.Trim();
        tenancy.PayeeAccount = SerbianAccount.Normalize(input.PayeeAccount)!;
        await _db.SaveEntitiesAsync();
        return await ToDtoAsync(tenancy, userId);
    }

    public async Task<TenancyDto> AddTenantAsync(int userId, int tenancyId, int tenantUserId)
    {
        var tenancy = await LoadAsync(tenancyId, tracking: true);
        EnsureLandlord(tenancy, userId);
        EnsureActive(tenancy);
        if (tenantUserId == userId) throw new BusinessRuleException(BusinessErrorCodes.LandlordCannotBeTenant);
        if (tenancy.Members.Any(m => m.UserId == tenantUserId && m.LeftDate is null))
            throw new BusinessRuleException(BusinessErrorCodes.TenantAlreadyInTenancy);
        var candidates = await CandidatesAsync(tenancy.ApartmentId, userId);
        if (!candidates.ContainsKey(tenantUserId))
            throw new BusinessRuleException(BusinessErrorCodes.TenantNotEligible);

        tenancy.Members.Add(new TenancyMember { UserId = tenantUserId, JoinedDate = Now });
        try
        {
            await _db.SaveEntitiesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new BusinessRuleException(BusinessErrorCodes.TenantAlreadyInTenancy);
        }

        await NotifyAddedAsync(tenancy, tenantUserId, await _notifier.NameOfAsync(userId));
        return await ToDtoAsync(tenancy, userId);
    }

    public async Task<TenancyDto> RemoveTenantAsync(int userId, int tenancyId, int tenantUserId)
    {
        var tenancy = await LoadAsync(tenancyId, tracking: true);
        var leaving = userId == tenantUserId;
        if (!leaving) EnsureLandlord(tenancy, userId);
        EnsureActive(tenancy);

        var member = tenancy.Members.FirstOrDefault(m => m.UserId == tenantUserId && m.LeftDate is null)
            ?? throw new NotFoundException("Tenant", tenantUserId);
        // Unpaid parts of earlier bills stay: leaving does not cancel what is owed.
        member.LeftDate = Now;
        await _db.SaveEntitiesAsync();

        var name = await _notifier.NameOfAsync(userId);
        var values = new Dictionary<string, string> { ["name"] = name, ["title"] = tenancy.ApartmentTitle };
        if (leaving)
            await _notifier.NotifyAsync(tenancy.LandlordUserId, userId, "tenancy_member_left",
                "Stanar je napustio najam", $"{name} je napustio/la najam „{tenancy.ApartmentTitle}“.",
                values, BillingNotifier.TenancyPage(tenancy.TenancyId));
        else
            await _notifier.NotifyAsync(tenantUserId, userId, "tenancy_removed",
                "Uklonjeni ste iz najma", $"{name} vas je uklonio/la iz najma „{tenancy.ApartmentTitle}“.",
                values, BillingNotifier.BillsPage);

        return await ToDtoAsync(tenancy, userId);
    }

    public async Task<TenancyDto> EndAsync(int userId, int tenancyId)
    {
        var tenancy = await LoadAsync(tenancyId, tracking: true);
        EnsureLandlord(tenancy, userId);
        EnsureActive(tenancy);

        tenancy.Status = TenancyStatus.Ended;
        tenancy.EndDate = BillingCalendar.Today(_time);
        await _db.SaveEntitiesAsync();

        var values = new Dictionary<string, string> { ["title"] = tenancy.ApartmentTitle };
        foreach (var member in tenancy.Members.Where(m => m.LeftDate is null))
            await _notifier.NotifyAsync(member.UserId, userId, "tenancy_ended",
                "Najam je završen", $"Najam stana „{tenancy.ApartmentTitle}“ je završen. Neplaćeni računi ostaju vidljivi.",
                values, BillingNotifier.TenancyPage(tenancy.TenancyId));

        return await ToDtoAsync(tenancy, userId);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<Tenancy> LoadAsync(int tenancyId, bool tracking)
    {
        var query = _db.Tenancies.Include(t => t.Members).Where(t => t.TenancyId == tenancyId);
        if (!tracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync() ?? throw new NotFoundException("Tenancy", tenancyId);
    }

    private async Task<TenancyDto> ToDtoAsync(Tenancy tenancy, int viewerId)
    {
        var openBills = await OpenBillCountsAsync(new[] { tenancy.TenancyId });
        var people = await _people.LoadAsync(tenancy.Members.Select(m => m.UserId).Append(tenancy.LandlordUserId));
        return BillingMapper.ToDto(tenancy, viewerId, people, openBills.GetValueOrDefault(tenancy.TenancyId));
    }

    private async Task<Dictionary<int, int>> OpenBillCountsAsync(IEnumerable<int> tenancyIds)
    {
        var ids = tenancyIds.ToList();
        return await _db.Bills.AsNoTracking()
            .Where(b => ids.Contains(b.TenancyId) && b.CancelledAt == null
                        && ((b.PayTo == BillPayee.Company && b.PaidAt == null)
                            || b.Shares.Any(s => s.Status != ShareStatus.Confirmed)))
            .GroupBy(b => b.TenancyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    private async Task<Lander.src.Modules.Listings.Models.Apartment> LoadOwnApartmentAsync(int userId, int apartmentId)
    {
        var apartment = await _listings.Apartments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId && !a.IsDeleted)
            ?? throw new NotFoundException("Apartment", apartmentId);
        if (apartment.LandlordId != userId)
            throw new ForbiddenException("Only the landlord of the apartment can do this.");
        return apartment;
    }

    /// <summary>User id → why they may be added: an approved application for the apartment, or the roommate of such an applicant.</summary>
    private async Task<Dictionary<int, string>> CandidatesAsync(int apartmentId, int landlordUserId)
    {
        var applicants = await _applications.ApartmentApplications.AsNoTracking()
            .Where(a => a.ApartmentId == apartmentId && a.Status == ApplicationStatuses.Approved && a.UserId != null)
            .Select(a => a.UserId!.Value)
            .Distinct()
            .ToListAsync();

        var result = applicants.Where(id => id != landlordUserId).ToDictionary(id => id, _ => "application");
        foreach (var applicant in applicants)
        {
            var partner = await _pairings.GetActivePartnerIdAsync(applicant);
            if (partner is int p && p != landlordUserId) result.TryAdd(p, "roommate");
        }
        return result;
    }

    private async Task NotifyAddedAsync(Tenancy tenancy, int tenantId, string landlordName)
    {
        await _notifier.NotifyAsync(tenantId, tenancy.LandlordUserId, "tenancy_added",
            "Dodati ste u najam",
            $"{landlordName} vas je dodao/la u najam stana „{tenancy.ApartmentTitle}“. Računi za kiriju i režije stižu ovde.",
            new Dictionary<string, string> { ["name"] = landlordName, ["title"] = tenancy.ApartmentTitle },
            BillingNotifier.TenancyPage(tenancy.TenancyId), "success");
    }

    internal static void EnsureCanView(Tenancy tenancy, int userId)
    {
        if (tenancy.LandlordUserId != userId && tenancy.Members.All(m => m.UserId != userId))
            throw new ForbiddenException("You are not part of this tenancy.");
    }

    private static void EnsureLandlord(Tenancy tenancy, int userId)
    {
        if (tenancy.LandlordUserId != userId)
            throw new ForbiddenException("Only the landlord can do this.");
    }

    private static void EnsureActive(Tenancy tenancy)
    {
        if (tenancy.Status != TenancyStatus.Active)
            throw new BusinessRuleException(BusinessErrorCodes.TenancyNotActive);
    }

    internal static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 };
}
