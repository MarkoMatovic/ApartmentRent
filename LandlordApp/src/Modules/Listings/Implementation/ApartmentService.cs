using System.Security.Claims;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Analytics.Interfaces;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Services;
using Lander.src.Modules.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lander.src.Modules.Listings.Implementation;

public partial class ApartmentService : IApartmentService
{
    /// <summary>
    /// Tag applied to every cached apartment-list entry, in both the HybridCache
    /// (see Queries) and the OutputCache "ApartmentDetail" policy. Mutations evict
    /// this single tag from both stores via <see cref="InvalidateApartmentCachesAsync"/>.
    /// </summary>
    internal const string ApartmentsTag = "apartments";

    private static readonly string[] ApartmentCacheTags = [ApartmentsTag];

    private readonly ListingsContext _context;
    // Cross-module data access via interfaces — ApartmentService no longer depends on
    // ReviewsContext or UsersContext directly, preserving bounded context isolation.
    private readonly IReviewStatsProvider _reviewStats;
    private readonly IListingsUserLookup _userLookup;
    private readonly HybridCache _hybridCache;
    private readonly IApartmentNotificationService _notificationService;
    private readonly IUserRoleUpgradeService _roleUpgradeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApartmentService> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly TimeProvider _timeProvider;
    private readonly IAuditLogService _auditLog;
    private readonly IAnalyticsService _analyticsService;
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly IConfiguration _configuration;
    private readonly Lander.src.Infrastructure.FileStorage.IImageUrlBuilder _imageUrlBuilder;

    public ApartmentService(
        ListingsContext context,
        IReviewStatsProvider reviewStats,
        IListingsUserLookup userLookup,
        HybridCache hybridCache,
        IApartmentNotificationService notificationService,
        IUserRoleUpgradeService roleUpgradeService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApartmentService> logger,
        IAuthorizationService authorizationService,
        TimeProvider timeProvider,
        IAuditLogService auditLog,
        IAnalyticsService analyticsService,
        IOutputCacheStore outputCacheStore,
        IConfiguration configuration,
        Lander.src.Infrastructure.FileStorage.IImageUrlBuilder imageUrlBuilder)
    {
        _context = context;
        _reviewStats = reviewStats;
        _userLookup = userLookup;
        _hybridCache = hybridCache;
        _notificationService = notificationService;
        _roleUpgradeService = roleUpgradeService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _authorizationService = authorizationService;
        _timeProvider = timeProvider;
        _auditLog = auditLog;
        _analyticsService = analyticsService;
        _outputCacheStore = outputCacheStore;
        _configuration = configuration;
        _imageUrlBuilder = imageUrlBuilder;
    }

    /// <summary>
    /// Drops every cached representation of apartment data after a mutation:
    /// the HTTP-level OutputCache entries (detail endpoint) and the HybridCache
    /// list entries (L1 + L2). Both stores share the "apartments" tag.
    /// </summary>
    private async Task InvalidateApartmentCachesAsync(CancellationToken ct = default)
    {
        await _outputCacheStore.EvictByTagAsync(ApartmentsTag, ct);
        await _hybridCache.RemoveByTagAsync(ApartmentsTag, ct);
    }
}
