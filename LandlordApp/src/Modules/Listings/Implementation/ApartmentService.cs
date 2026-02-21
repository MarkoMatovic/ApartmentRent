using System;
using System.Security.Claims;
using System.Text.Json;
using Lander.src.Common;
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
using Lander.src.Modules.Communication.Intefaces;
namespace Lander.src.Modules.Listings.Implementation;
public class ApartmentService : IApartmentService
{
    private readonly ListingsContext _context;
    private readonly UsersContext _usersContext;
    private readonly SavedSearchesContext _savedSearchesContext;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly ILogger<ApartmentService> _logger;

    public ApartmentService(
        ListingsContext context,
        UsersContext usersContext,
        SavedSearchesContext savedSearchesContext,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IHubContext<NotificationHub> notificationHubContext,
        ILogger<ApartmentService> logger)
    {
        _context = context;
        _usersContext = usersContext;
        _savedSearchesContext = savedSearchesContext;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
        _notificationHubContext = notificationHubContext;
        _logger = logger;
    }
    public async Task<bool> ActivateApartmentAsync(int apartmentId)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub") 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var apartment = await _context.Apartments
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId);
        if (apartment == null) return false;
        apartment.IsDeleted = false;
        apartment.IsActive = true;
        apartment.ModifiedByGuid = Guid.TryParse(currentUserGuid, out Guid parsedGuid) ? parsedGuid : null;
        apartment.ModifiedDate = DateTime.UtcNow;
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        return true;
    }
    public async Task<ApartmentDto> CreateApartmentAsync(ApartmentInputDto apartmentInputDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub") 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int? landlordId = null;
        if (currentUserGuid != null && Guid.TryParse(currentUserGuid, out Guid parsedGuid))
        {
            var user = await _usersContext.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.UserGuid == parsedGuid);
            
            if (user != null)
            {
                landlordId = user.UserId;
                
                // Auto-upgrade: If user is Tenant, upgrade to TenantLandlord when creating first apartment
                if (user.UserRole?.RoleName == "Tenant")
                {
                    var tenantLandlordRole = await _usersContext.Roles
                        .FirstOrDefaultAsync(r => r.RoleName == "TenantLandlord");
                    
                    if (tenantLandlordRole != null)
                    {
                        user.UserRoleId = tenantLandlordRole.RoleId;
                        await _usersContext.SaveEntitiesAsync();
                    }
                }
            }
        }
        var apartment = new Apartment
        {
            LandlordId = landlordId,
            Title = apartmentInputDto.Title,
            Description = apartmentInputDto.Description,
            Rent = apartmentInputDto.Rent,
            Price = apartmentInputDto.Price,
            Address = apartmentInputDto.Address,
            City = apartmentInputDto.City,
            PostalCode = apartmentInputDto.PostalCode,
            AvailableFrom = apartmentInputDto.AvailableFrom,
            AvailableUntil = apartmentInputDto.AvailableUntil,
            NumberOfRooms = apartmentInputDto.NumberOfRooms,
            RentIncludeUtilities = apartmentInputDto.RentIncludeUtilities ?? false,
            Latitude = apartmentInputDto.Latitude,
            Longitude = apartmentInputDto.Longitude,
            SizeSquareMeters = apartmentInputDto.SizeSquareMeters,
            ApartmentType = apartmentInputDto.ApartmentType ?? ApartmentType.Studio,
            ListingType = apartmentInputDto.ListingType ?? ListingType.Rent,
            IsFurnished = apartmentInputDto.IsFurnished ?? false,
            HasBalcony = apartmentInputDto.HasBalcony ?? false,
            HasElevator = apartmentInputDto.HasElevator ?? false,
            HasParking = apartmentInputDto.HasParking ?? false,
            HasInternet = apartmentInputDto.HasInternet ?? false,
            HasAirCondition = apartmentInputDto.HasAirCondition ?? false,
            IsPetFriendly = apartmentInputDto.IsPetFriendly ?? false,
            IsSmokingAllowed = apartmentInputDto.IsSmokingAllowed ?? false,
            DepositAmount = apartmentInputDto.DepositAmount,
            MinimumStayMonths = apartmentInputDto.MinimumStayMonths,
            MaximumStayMonths = apartmentInputDto.MaximumStayMonths,
            IsImmediatelyAvailable = apartmentInputDto.IsImmediatelyAvailable ?? false,
            IsLookingForRoommate = apartmentInputDto.IsLookingForRoommate ?? false,
            ContactPhone = apartmentInputDto.ContactPhone,
            IsActive = true,
            CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            ModifiedDate = DateTime.UtcNow
        };
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.Apartments.Add(apartment);
            await _context.SaveEntitiesAsync(); // Save apartment first to get ApartmentId
            if (apartmentInputDto.ImageUrls != null && apartmentInputDto.ImageUrls.Any())
            {
                var apartmentImages = apartmentInputDto.ImageUrls.Select((url, index) => new ApartmentImage
                {
                    ApartmentId = apartment.ApartmentId,
                    ImageUrl = url,
                    DisplayOrder = index,
                    IsPrimary = index == 0,
                    IsDeleted = false,
                    CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
                    ModifiedDate = DateTime.UtcNow
                }).ToList();
                _context.ApartmentImages.AddRange(apartmentImages);
                await _context.SaveEntitiesAsync(); // Save images
            }
            await _context.CommitTransactionAsync(transaction);
        }
        catch (Exception ex)
        {
            _context.RollBackTransaction();
            throw;
        }

        // Broadcast new listing notification
        if (apartment.IsActive)
        {
             await _notificationHubContext.Clients.All.SendAsync("ReceiveNotification", 
                "New Apartment Listed!", 
                $"A new apartment '{apartment.Title}' is now available in {apartment.City}.", 
                "success");
        }

        return new ApartmentDto
        {
            ApartmentId = apartment.ApartmentId,
            Title = apartment.Title,
            Rent = apartment.Rent,
            Price = apartment.Price,
            Address = apartment.Address,
            City = apartment.City,
            Latitude = apartment.Latitude,
            Longitude = apartment.Longitude,
            SizeSquareMeters = apartment.SizeSquareMeters,
            ApartmentType = apartment.ApartmentType,
            ListingType = apartment.ListingType,
            IsFurnished = apartment.IsFurnished,
            IsImmediatelyAvailable = apartment.IsImmediatelyAvailable
        };
    }
    public async Task<bool> DeleteApartmentAsync(int apartmentId)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub") 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var apartment = await _context.Apartments
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId && !a.IsDeleted);
        if (apartment == null) return false;

        // Notify saved search users BEFORE deactivating
        try
        {
            // Find all active saved searches that reference this apartment ID in their filters
            var allSavedSearches = await _savedSearchesContext.SavedSearches
                .Where(ss => ss.IsActive && ss.EmailNotificationsEnabled)
                .ToListAsync();

            var affectedUserIds = allSavedSearches
                .Where(ss => ss.FiltersJson != null &&
                             ss.FiltersJson.Contains($"\"apartmentId\":{apartmentId}",
                                 StringComparison.OrdinalIgnoreCase))
                .Select(ss => ss.UserId)
                .Distinct()
                .ToList();

            if (affectedUserIds.Any())
            {
                var usersToNotify = await _usersContext.Users
                    .Where(u => affectedUserIds.Contains(u.UserId) && u.IsActive)
                    .ToListAsync();

                foreach (var user in usersToNotify)
                {
                    _ = _emailService.SendListingUnavailableEmailAsync(
                        user.Email,
                        user.FirstName,
                        apartment.Title,
                        "This listing has been removed by the landlord.");
                }

                _logger.LogInformation("Sent listing unavailable notifications to {Count} user(s) for apartment {ApartmentId}",
                    usersToNotify.Count, apartmentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying saved search users for deleted apartment {ApartmentId}", apartmentId);
            // Do not block deletion on notification failure
        }

        apartment.IsDeleted = true;
        apartment.IsActive = false;
        apartment.ModifiedByGuid = Guid.TryParse(currentUserGuid, out Guid parsedGuid)? parsedGuid : null;
        apartment.ModifiedDate = DateTime.UtcNow;
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        return true;
    }
    public async Task<PagedResult<ApartmentDto>> GetAllApartmentsAsync()
    {
        var apartments = await _context.Apartments
            .Include(a => a.ApartmentImages)
            // Named query filter automatically applies: !a.IsDeleted && a.IsActive
            .AsNoTracking()
            .AsSplitQuery()
            .OrderBy(a => a.Rent)
            .Take(100)
            .Select(a => new
            {
                Apartment = a,
                LandlordId = a.LandlordId
            })
            .ToListAsync();
        var apartmentDtos = apartments.Select(a => new ApartmentDto
        {
            ApartmentId = a.Apartment.ApartmentId,
            Title = a.Apartment.Title,
            Rent = a.Apartment.Rent,
            Price = a.Apartment.Price,
            Address = a.Apartment.Address,
            City = a.Apartment.City ?? string.Empty,
            Latitude = a.Apartment.Latitude,
            Longitude = a.Apartment.Longitude,
            SizeSquareMeters = a.Apartment.SizeSquareMeters,
            ApartmentType = a.Apartment.ApartmentType,
            ListingType = a.Apartment.ListingType,
            IsFurnished = a.Apartment.IsFurnished,
            IsImmediatelyAvailable = a.Apartment.IsImmediatelyAvailable,
            IsLookingForRoommate = a.Apartment.IsLookingForRoommate,
            ApartmentImages = a.Apartment.ApartmentImages
                .Where(img => !img.IsDeleted)
                .OrderBy(img => img.DisplayOrder)
                .Take(5)
                .Select(img => new ApartmentImageDto
                {
                    ImageId = img.ImageId,
                    ApartmentId = img.ApartmentId,
                    ImageUrl = img.ImageUrl,
                    IsPrimary = img.IsPrimary
                }).ToList()
        }).ToList();
        return new PagedResult<ApartmentDto>
        {
            Items = apartmentDtos,
            TotalCount = apartmentDtos.Count,
            Page = 1,
            PageSize = 100
        };
    }
    public async Task<PagedResult<ApartmentDto>> GetAllApartmentsAsync(ApartmentFilterDto filters)
    {
        var cacheKey = $"Apartments_{filters.GetHashCode()}_{filters.Page}_{filters.PageSize}_{filters.SortBy}_{filters.SortOrder}_{filters.City}_{filters.MinRent}_{filters.MaxRent}_{filters.ApartmentType}_{filters.NumberOfRooms}";
        
        if (_cache.TryGetValue(cacheKey, out PagedResult<ApartmentDto>? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        var query = _context.Apartments
            // Named query filter automatically applies: !a.IsDeleted && a.IsActive
            .AsNoTracking();
        if (filters.ListingType.HasValue)
        {
            query = query.Where(a => a.ListingType == filters.ListingType.Value);
        }
        if (!string.IsNullOrEmpty(filters.City))
        {
            query = query.Where(a => a.City != null && a.City.Contains(filters.City));
        }
        if (filters.MinRent.HasValue)
        {
            query = query.Where(a => a.Rent >= filters.MinRent.Value);
        }
        if (filters.MaxRent.HasValue)
        {
            query = query.Where(a => a.Rent <= filters.MaxRent.Value);
        }
        if (filters.NumberOfRooms.HasValue)
        {
            query = query.Where(a => a.NumberOfRooms == filters.NumberOfRooms.Value);
        }
        if (filters.ApartmentType.HasValue)
        {
            query = query.Where(a => a.ApartmentType == filters.ApartmentType.Value);
        }
        if (filters.IsFurnished.HasValue)
        {
            query = query.Where(a => a.IsFurnished == filters.IsFurnished.Value);
        }
        if (filters.IsPetFriendly.HasValue)
        {
            query = query.Where(a => a.IsPetFriendly == filters.IsPetFriendly.Value);
        }
        if (filters.IsSmokingAllowed.HasValue)
        {
            query = query.Where(a => a.IsSmokingAllowed == filters.IsSmokingAllowed.Value);
        }
        if (filters.HasParking.HasValue)
        {
            query = query.Where(a => a.HasParking == filters.HasParking.Value);
        }
        if (filters.HasBalcony.HasValue)
        {
            query = query.Where(a => a.HasBalcony == filters.HasBalcony.Value);
        }
        if (filters.IsImmediatelyAvailable.HasValue)
        {
            query = query.Where(a => a.IsImmediatelyAvailable == filters.IsImmediatelyAvailable.Value);
        }
        if (filters.AvailableFrom.HasValue)
        {
            query = query.Where(a => a.AvailableFrom >= filters.AvailableFrom.Value);
        }
        var totalCount = await query.CountAsync();
        
        _logger.LogInformation("Apartment search: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}, Skip={Skip}", 
            filters.Page, filters.PageSize, totalCount, (filters.Page - 1) * filters.PageSize);
            
        var sortBy = filters.SortBy?.ToLower() ?? "date";
        var sortOrder = filters.SortOrder?.ToLower() ?? "desc";
        
        // Modern .NET 10 approach: Use pattern matching with tuple switch expression
        IOrderedQueryable<Apartment> orderedQuery = (sortBy, sortOrder) switch
        {
            // For price sorting: Use Rent if > 0 (rentals), otherwise use Price (sales)
            // This handles both ListingType.Rent (has Rent > 0) and ListingType.Sale (has Price > 0, Rent = 0)
            ("rent" or "price", "asc") => query.OrderBy(a => a.Rent > 0 ? a.Rent : a.Price ?? 0),
            ("rent" or "price", "desc") => query.OrderByDescending(a => a.Rent > 0 ? a.Rent : a.Price ?? 0),
            
            ("size", "asc") => query.OrderBy(a => a.SizeSquareMeters),
            ("size", "desc") => query.OrderByDescending(a => a.SizeSquareMeters),
            
            ("date", "asc") => query.OrderBy(a => a.CreatedDate),
            ("date", "desc") or _ => query.OrderByDescending(a => a.CreatedDate)
        };
        var apartments = await orderedQuery
            .Include(a => a.ApartmentImages)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .AsSplitQuery()
            .Select(a => new
            {
                Apartment = a,
                LandlordId = a.LandlordId
            })
            .ToListAsync();
        var items = apartments.Select(a => new ApartmentDto
        {
            ApartmentId = a.Apartment.ApartmentId,
            Title = a.Apartment.Title,
            Rent = a.Apartment.Rent,
            Price = a.Apartment.Price,
            Address = a.Apartment.Address,
            City = a.Apartment.City ?? string.Empty,
            Latitude = a.Apartment.Latitude,
            Longitude = a.Apartment.Longitude,
            SizeSquareMeters = a.Apartment.SizeSquareMeters,
            ApartmentType = a.Apartment.ApartmentType,
            ListingType = a.Apartment.ListingType,
            IsFurnished = a.Apartment.IsFurnished,
            IsImmediatelyAvailable = a.Apartment.IsImmediatelyAvailable,
            IsLookingForRoommate = a.Apartment.IsLookingForRoommate,
            ApartmentImages = a.Apartment.ApartmentImages
                .Where(img => !img.IsDeleted)
                .OrderBy(img => img.DisplayOrder)
                .Take(5)
                .Select(img => new ApartmentImageDto
                {
                    ImageId = img.ImageId,
                    ApartmentId = img.ApartmentId,
                    ImageUrl = img.ImageUrl,
                    IsPrimary = img.IsPrimary
                }).ToList()
        }).ToList();
        
        var result = new PagedResult<ApartmentDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filters.Page,
            PageSize = filters.PageSize
        };

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(2)) // Keep in cache for 2 mins if accessed
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)); // Remove after 5 mins regardless

        _cache.Set(cacheKey, result, cacheEntryOptions);

        return result;
    }
    public async Task<PagedResult<ApartmentDto>> GetMyApartmentsAsync()
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub") 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int? landlordId = null;
        if (currentUserGuid != null && Guid.TryParse(currentUserGuid, out Guid parsedGuid))
        {
            var user = await _usersContext.Users.FirstOrDefaultAsync(u => u.UserGuid == parsedGuid);
            if (user != null)
            {
                landlordId = user.UserId;
            }
        }
        if (!landlordId.HasValue)
        {
            return new PagedResult<ApartmentDto>
            {
                Items = new List<ApartmentDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            };
        }
        var query = _context.Apartments
            .Where(a => !a.IsDeleted && a.LandlordId == landlordId.Value)
            .AsNoTracking();
        var totalCount = await query.CountAsync();
        var apartments = await query
            .Include(a => a.ApartmentImages.Where(img => !img.IsDeleted))
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();
        var items = apartments.Select(a => new ApartmentDto
        {
            ApartmentId = a.ApartmentId,
            Title = a.Title,
            Rent = a.Rent,
            Price = a.Price,
            Address = a.Address,
            City = a.City ?? string.Empty,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
            SizeSquareMeters = a.SizeSquareMeters,
            ApartmentType = a.ApartmentType,
            ListingType = a.ListingType,
            IsFurnished = a.IsFurnished,
            IsImmediatelyAvailable = a.IsImmediatelyAvailable,
            IsLookingForRoommate = a.IsLookingForRoommate,
            ApartmentImages = a.ApartmentImages
                .Where(img => !img.IsDeleted)
                .OrderBy(img => img.DisplayOrder)
                .Select(img => new ApartmentImageDto
                {
                    ImageId = img.ImageId,
                    ApartmentId = img.ApartmentId,
                    ImageUrl = img.ImageUrl,
                    IsPrimary = img.IsPrimary
                }).ToList()
        }).ToList();
        return new PagedResult<ApartmentDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = 1,
            PageSize = totalCount
        };
    }
    public async Task<GetApartmentDto> GetApartmentByIdAsync(int apartmentId)
    {
        var apartment = await _context.Apartments
            .Include(a => a.ApartmentImages.Where(img => !img.IsDeleted))
            .AsNoTracking()
            .Where(a => a.ApartmentId == apartmentId && !a.IsDeleted)
            .FirstOrDefaultAsync();
        if (apartment == null)
        {
            return null;
        }
        string? landlordName = null;
        string? landlordEmail = null;
        if (apartment.LandlordId.HasValue)
        {
            var landlord = await _usersContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == apartment.LandlordId.Value);
            if (landlord != null)
            {
                landlordName = $"{landlord.FirstName} {landlord.LastName}".Trim();
                landlordEmail = landlord.Email;
            }
        }
        return new GetApartmentDto
        {
            ApartmentId = apartment.ApartmentId,
            Title = apartment.Title,
            Description = apartment.Description,
            Rent = apartment.Rent,
            Price = apartment.Price,
            Address = apartment.Address,
            City = apartment.City,
            PostalCode = apartment.PostalCode,
            AvailableFrom = apartment.AvailableFrom ?? DateOnly.MinValue,
            AvailableUntil = apartment.AvailableUntil ?? DateOnly.MaxValue,
            NumberOfRooms = apartment.NumberOfRooms ?? 0,
            RentIncludeUtilities = apartment.RentIncludeUtilities ?? false,
            Latitude = apartment.Latitude,
            Longitude = apartment.Longitude,
            SizeSquareMeters = apartment.SizeSquareMeters,
            ApartmentType = apartment.ApartmentType,
            ListingType = apartment.ListingType,
            IsFurnished = apartment.IsFurnished,
            HasBalcony = apartment.HasBalcony,
            HasElevator = apartment.HasElevator,
            HasParking = apartment.HasParking,
            HasInternet = apartment.HasInternet,
            HasAirCondition = apartment.HasAirCondition,
            IsPetFriendly = apartment.IsPetFriendly,
            IsSmokingAllowed = apartment.IsSmokingAllowed,
            DepositAmount = apartment.DepositAmount,
            MinimumStayMonths = apartment.MinimumStayMonths,
            MaximumStayMonths = apartment.MaximumStayMonths,
            IsImmediatelyAvailable = apartment.IsImmediatelyAvailable,
            IsLookingForRoommate = apartment.IsLookingForRoommate,
            ContactPhone = apartment.ContactPhone,
            LandlordId = apartment.LandlordId,
            LandlordName = landlordName,
            LandlordEmail = landlordEmail,
            ApartmentImages = apartment.ApartmentImages?
                .Where(img => !img.IsDeleted)
                .OrderBy(img => img.DisplayOrder)
                .Select(img => new ApartmentImageDto
            {
                ImageId = img.ImageId,
                ApartmentId = img.ApartmentId,
                ImageUrl = img.ImageUrl,
                IsPrimary = img.IsPrimary
            }).ToList()
        };
    }

    public async Task<List<ApartmentDto>> GetApartmentsByLandlordIdAsync(int landlordId)
    {
        var apartments = await _context.Apartments
            .Include(a => a.ApartmentImages)
            .AsNoTracking()
            .Where(a => a.LandlordId == landlordId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();

        return apartments.Select(a => new ApartmentDto
        {
            ApartmentId = a.ApartmentId,
            Title = a.Title,
            Rent = a.Rent,
            Price = a.Price,
            Address = a.Address,
            City = a.City ?? string.Empty,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
            SizeSquareMeters = a.SizeSquareMeters,
            ApartmentType = a.ApartmentType,
            ListingType = a.ListingType,
            IsFurnished = a.IsFurnished,
            IsImmediatelyAvailable = a.IsImmediatelyAvailable,
            IsLookingForRoommate = a.IsLookingForRoommate,
            ApartmentImages = a.ApartmentImages
                .Where(img => !img.IsDeleted)
                .OrderBy(img => img.DisplayOrder)
                .Select(img => new ApartmentImageDto
                {
                    ImageId = img.ImageId,
                    ApartmentId = img.ApartmentId,
                    ImageUrl = img.ImageUrl,
                    IsPrimary = img.IsPrimary
                }).ToList()
        }).ToList();
    }

    public async Task DeleteApartmentsByLandlordIdAsync(int landlordId)
    {
        var apartments = await _context.Apartments
            .Where(a => a.LandlordId == landlordId && !a.IsDeleted)
            .ToListAsync();

        if (!apartments.Any()) return;

        foreach (var apartment in apartments)
        {
            apartment.IsDeleted = true;
            apartment.IsActive = false;
            apartment.ModifiedDate = DateTime.UtcNow;
        }

        var transaction = await _context.BeginTransactionAsync();
        try
        {
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
    }
    public async Task<ApartmentDto> UpdateApartmentAsync(int apartmentId, ApartmentUpdateInputDto updateDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub") 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var apartment = await _context.Apartments
            .Include(a => a.ApartmentImages)
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId && !a.IsDeleted);
        if (apartment == null)
        {
            throw new Exception("Apartment not found or has been deleted");
        }
        if (updateDto.Title != null) apartment.Title = updateDto.Title;
        if (updateDto.Description != null) apartment.Description = updateDto.Description;
        if (updateDto.Rent.HasValue) apartment.Rent = updateDto.Rent.Value;
        if (updateDto.Price.HasValue) apartment.Price = updateDto.Price.Value;
        if (updateDto.Address != null) apartment.Address = updateDto.Address;
        if (updateDto.City != null) apartment.City = updateDto.City;
        if (updateDto.PostalCode != null) apartment.PostalCode = updateDto.PostalCode;
        if (updateDto.AvailableFrom.HasValue) apartment.AvailableFrom = updateDto.AvailableFrom.Value;
        if (updateDto.AvailableUntil.HasValue) apartment.AvailableUntil = updateDto.AvailableUntil.Value;
        if (updateDto.NumberOfRooms.HasValue) apartment.NumberOfRooms = updateDto.NumberOfRooms.Value;
        if (updateDto.RentIncludeUtilities.HasValue) apartment.RentIncludeUtilities = updateDto.RentIncludeUtilities.Value;
        if (updateDto.Latitude.HasValue) apartment.Latitude = updateDto.Latitude.Value;
        if (updateDto.Longitude.HasValue) apartment.Longitude = updateDto.Longitude.Value;
        if (updateDto.SizeSquareMeters.HasValue) apartment.SizeSquareMeters = updateDto.SizeSquareMeters.Value;
        if (updateDto.ApartmentType.HasValue) apartment.ApartmentType = updateDto.ApartmentType.Value;
        if (updateDto.ListingType.HasValue) apartment.ListingType = updateDto.ListingType.Value;
        if (updateDto.IsFurnished.HasValue) apartment.IsFurnished = updateDto.IsFurnished.Value;
        if (updateDto.HasBalcony.HasValue) apartment.HasBalcony = updateDto.HasBalcony.Value;
        if (updateDto.HasElevator.HasValue) apartment.HasElevator = updateDto.HasElevator.Value;
        if (updateDto.HasParking.HasValue) apartment.HasParking = updateDto.HasParking.Value;
        if (updateDto.HasInternet.HasValue) apartment.HasInternet = updateDto.HasInternet.Value;
        if (updateDto.HasAirCondition.HasValue) apartment.HasAirCondition = updateDto.HasAirCondition.Value;
        if (updateDto.IsPetFriendly.HasValue) apartment.IsPetFriendly = updateDto.IsPetFriendly.Value;
        if (updateDto.IsSmokingAllowed.HasValue) apartment.IsSmokingAllowed = updateDto.IsSmokingAllowed.Value;
        if (updateDto.DepositAmount.HasValue) apartment.DepositAmount = updateDto.DepositAmount.Value;
        if (updateDto.MinimumStayMonths.HasValue) apartment.MinimumStayMonths = updateDto.MinimumStayMonths.Value;
        if (updateDto.MaximumStayMonths.HasValue) apartment.MaximumStayMonths = updateDto.MaximumStayMonths.Value;
        if (updateDto.IsImmediatelyAvailable.HasValue) apartment.IsImmediatelyAvailable = updateDto.IsImmediatelyAvailable.Value;
        if (updateDto.IsLookingForRoommate.HasValue) apartment.IsLookingForRoommate = updateDto.IsLookingForRoommate.Value;
        if (updateDto.ContactPhone != null) apartment.ContactPhone = updateDto.ContactPhone;
        apartment.ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null;
        apartment.ModifiedDate = DateTime.UtcNow;
        if (updateDto.ImageUrls != null && updateDto.ImageUrls.Any())
        {
            var existingImages = apartment.ApartmentImages.Where(img => !img.IsDeleted).ToList();
            foreach (var img in existingImages)
            {
                img.IsDeleted = true;
                img.ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null;
                img.ModifiedDate = DateTime.UtcNow;
            }
            var newImages = updateDto.ImageUrls.Select((url, index) => new ApartmentImage
            {
                ApartmentId = apartment.ApartmentId,
                ImageUrl = url,
                DisplayOrder = index,
                IsPrimary = index == 0,
                CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
                CreatedDate = DateTime.UtcNow,
                ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
                ModifiedDate = DateTime.UtcNow
            }).ToList();
            _context.ApartmentImages.AddRange(newImages);
        }
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        return new ApartmentDto
        {
            ApartmentId = apartment.ApartmentId,
            Title = apartment.Title,
            Rent = apartment.Rent,
            Price = apartment.Price,
            Address = apartment.Address,
            City = apartment.City,
            Latitude = apartment.Latitude,
            Longitude = apartment.Longitude,
            SizeSquareMeters = apartment.SizeSquareMeters,
            ApartmentType = apartment.ApartmentType,
            ListingType = apartment.ListingType,
            IsFurnished = apartment.IsFurnished,
            IsImmediatelyAvailable = apartment.IsImmediatelyAvailable
        };
    }
    
    // .NET 10 Feature: Vector Search implementation
    public async Task<List<ApartmentDto>> GetAllApartmentsForSemanticSearchAsync()
    {
        var apartments = await _context.Apartments
            .AsNoTracking()
            .Select(a => new ApartmentDto
            {
                ApartmentId = a.ApartmentId,
                Title = a.Title,
                Rent = a.Rent,
                Price = a.Price,
                Address = a.Address,
                City = a.City,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                SizeSquareMeters = a.SizeSquareMeters,
                ApartmentType = a.ApartmentType,
                ListingType = a.ListingType,
                IsFurnished = a.IsFurnished,
                IsImmediatelyAvailable = a.IsImmediatelyAvailable,
                DescriptionEmbedding = a.DescriptionEmbedding
            })
            .ToListAsync();
        
        return apartments;
    }
    
    public async Task<int> GenerateEmbeddingsForAllApartmentsAsync(SimpleEmbeddingService embeddingService)
    {
        var apartments = await _context.Apartments
            .IgnoreQueryFilters()
            .ToListAsync();
        
        int count = 0;
        foreach (var apartment in apartments)
        {
            if (!string.IsNullOrWhiteSpace(apartment.Description))
            {
                var embedding = embeddingService.GenerateEmbedding(apartment.Description);
                apartment.DescriptionEmbedding = System.Text.Json.JsonSerializer.Serialize(embedding);
                count++;
            }
        }
        
        await _context.SaveChangesAsync();
        return count;
    }
}
