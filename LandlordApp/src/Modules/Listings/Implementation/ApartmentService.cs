using System.Security.Claims;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Services;
using Lander.src.Modules.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lander.src.Modules.Listings.Implementation;

public partial class ApartmentService : IApartmentService
{
    private readonly ListingsContext _context;
    private readonly UsersContext _usersContext;
    private readonly HybridCache _hybridCache;
    private readonly ReviewsContext _reviewsContext;
    private readonly IApartmentNotificationService _notificationService;
    private readonly IUserRoleUpgradeService _roleUpgradeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApartmentService> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly TimeProvider _timeProvider;
    private readonly ApartmentCacheVersionService _cacheVersion;
    private readonly IAuditLogService _auditLog;

    public ApartmentService(
        ListingsContext context,
        UsersContext usersContext,
        HybridCache hybridCache,
        ReviewsContext reviewsContext,
        IApartmentNotificationService notificationService,
        IUserRoleUpgradeService roleUpgradeService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApartmentService> logger,
        IAuthorizationService authorizationService,
        TimeProvider timeProvider,
        ApartmentCacheVersionService cacheVersion,
        IAuditLogService auditLog)
    {
        _context = context;
        _usersContext = usersContext;
        _hybridCache = hybridCache;
        _reviewsContext = reviewsContext;
        _notificationService = notificationService;
        _roleUpgradeService = roleUpgradeService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _authorizationService = authorizationService;
        _timeProvider = timeProvider;
        _cacheVersion = cacheVersion;
        _auditLog = auditLog;
    }
}
