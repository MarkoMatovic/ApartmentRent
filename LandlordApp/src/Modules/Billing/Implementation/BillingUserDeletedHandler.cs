using Lander.src.Common;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Billing.Implementation;

/// <summary>
/// A deleted account takes its billing data along: a landlord's tenancies (with their bills), a tenant's
/// membership, every part they owed or were owed, and their saved payout account. A company bill they
/// paid stays paid, without a name.
/// </summary>
public sealed class BillingUserDeletedHandler : IUserDeletedHandler
{
    private readonly BillingContext _db;

    public BillingUserDeletedHandler(BillingContext db)
    {
        _db = db;
    }

    public async Task HandleAsync(int userId)
    {
        var tenancies = await _db.Tenancies.Where(t => t.LandlordUserId == userId).ToListAsync();
        _db.Tenancies.RemoveRange(tenancies); // members, bills and shares go with them (cascade)

        _db.TenancyMembers.RemoveRange(await _db.TenancyMembers.Where(m => m.UserId == userId).ToListAsync());
        _db.BillShares.RemoveRange(await _db.BillShares
            .Where(s => s.PayerUserId == userId || s.ReceiverUserId == userId).ToListAsync());
        _db.BillingProfiles.RemoveRange(await _db.BillingProfiles.Where(p => p.UserId == userId).ToListAsync());

        foreach (var bill in await _db.Bills.Where(b => b.PaidByUserId == userId).ToListAsync())
            bill.PaidByUserId = null;

        await _db.SaveEntitiesAsync();
    }
}
