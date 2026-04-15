using System;
using System.Security.Claims;
using System.Text.Json;
using Lander.src.Common;
using Lander.src.Common.Exceptions;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Lander.src.Modules.MachineLearning.Services; // .NET 10: Vector Search
using Microsoft.AspNetCore.SignalR;
using Lander.src.Notifications.NotificationsHub;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Listings.Helpers;

namespace Lander.src.Modules.Listings.Implementation;

public partial class ApartmentService : IApartmentService
{
    private readonly ListingsContext _context;
    private readonly UsersContext _usersContext;
    private readonly SavedSearchesContext _savedSearchesContext;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly ReviewsContext _reviewsContext;
    private readonly ILogger<ApartmentService> _logger;

    public ApartmentService(
        ListingsContext context,
        UsersContext usersContext,
        SavedSearchesContext savedSearchesContext,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IHubContext<NotificationHub> notificationHubContext,
        ReviewsContext reviewsContext,
        ILogger<ApartmentService> logger)
    {
        _context = context;
        _usersContext = usersContext;
        _savedSearchesContext = savedSearchesContext;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
        _notificationHubContext = notificationHubContext;
        _reviewsContext = reviewsContext;
        _logger = logger;
    }
}
