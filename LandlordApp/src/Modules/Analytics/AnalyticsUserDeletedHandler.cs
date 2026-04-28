using Lander.src.Common;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Analytics;

/// <summary>
/// Anonymises analytics events for a deleted user (nulls out the UserId so
/// aggregate stats remain valid, but no personally-identifiable data is retained).
/// </summary>
public class AnalyticsUserDeletedHandler : IUserDeletedHandler
{
    private readonly AnalyticsContext _context;

    public AnalyticsUserDeletedHandler(AnalyticsContext context)
        => _context = context;

    public async Task HandleAsync(int userId)
    {
        await _context.AnalyticsEvents
            .Where(e => e.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.UserId, (int?)null)
                .SetProperty(e => e.CreatedByGuid, (Guid?)null));
    }
}
