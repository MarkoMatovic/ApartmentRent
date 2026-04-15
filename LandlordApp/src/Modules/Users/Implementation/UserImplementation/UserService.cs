using Lander.Helpers;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Lander.src.Common;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;
public partial class UserService : IUserInterface
{
    private readonly UsersContext _context;
    private readonly ReviewsContext _reviewsContext;
    private readonly TokenProvider _tokenProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IEmailService _emailService;
    private readonly IApartmentService _apartmentService;
    private readonly IRoommateService _roommateService;
    private readonly IEnumerable<IUserDeletedHandler> _deletionHandlers;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly ILogger<UserService> _logger;
    private readonly IConfiguration _configuration;
    public UserService(
        UsersContext context,
        ReviewsContext reviewsContext,
        TokenProvider tokenProvider,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        IApartmentService apartmentService,
        IRoommateService roommateService,
        IEnumerable<IUserDeletedHandler> deletionHandlers,
        RefreshTokenService refreshTokenService,
        ILogger<UserService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _reviewsContext = reviewsContext;
        _tokenProvider = tokenProvider;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _apartmentService = apartmentService;
        _roommateService = roommateService;
        _deletionHandlers = deletionHandlers;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
        _configuration = configuration;
    }
}
