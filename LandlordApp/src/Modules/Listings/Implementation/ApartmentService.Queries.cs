using System.Security.Claims;
using Lander.src.Common;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lander.src.Modules.Listings.Implementation;

public partial class ApartmentService
{
    public async Task<PagedResult<ApartmentDto>> GetAllApartmentsAsync()
    {
        var apartments = await _context.Apartments
            .Include(a => a.ApartmentImages)
            // Named query filter automatically applies: !a.IsDeleted && a.IsActive
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(a => a.IsFeatured)
            .ThenBy(a => a.Rent)
            .Take(100)
            .ToListAsync();

        var apartmentDtos = apartments.Select(a => a.ToDto()).ToList();
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
        if (filters.City != null || filters.MinRent.HasValue || filters.MaxRent.HasValue || filters.ListingType.HasValue)
        {
            var ctx = _httpContextAccessor.HttpContext;
            var searchQuery = $"City:{filters.City},MinRent:{filters.MinRent},MaxRent:{filters.MaxRent},ListingType:{filters.ListingType}";
            _ = _analyticsService.TrackEventAsync(
                "ApartmentSearch", "Listings",
                searchQuery: searchQuery,
                ipAddress: ctx?.Connection.RemoteIpAddress?.ToString(),
                userAgent: ctx?.Request.Headers["User-Agent"].ToString());
        }

        // Deterministic cache key — GetHashCode() is not stable across processes/instances.
        var cacheKey = $"Apartments_p{filters.Page}_ps{filters.PageSize}_s{filters.SortBy}_{filters.SortOrder}" +
                       $"_c{filters.City}_mr{filters.MinRent}_{filters.MaxRent}" +
                       $"_at{(int?)filters.ApartmentType}_nr{filters.NumberOfRooms}" +
                       $"_lt{(int?)filters.ListingType}_ia{filters.IsImmediatelyAvailable}";

        // HybridCache: built-in stampede protection + Redis-ready L2 cache.
        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                // Note: Boolean filters (IsFurnished, etc.) temporarily disabled pending JSON column support.
                var query = _context.Apartments.AsNoTracking().ApplyFilters(filters);
                var orderedQuery = query.ApplySort(filters.SortBy, filters.SortOrder);

                var totalCount = await query.CountAsync(ct);
                var apartments = await orderedQuery
                    .Include(a => a.ApartmentImages)
                    .Skip((filters.Page - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .ToListAsync(ct);

                _logger.LogInformation("Apartment search: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
                    filters.Page, filters.PageSize, totalCount);

                var apartmentIds = apartments.Select(a => a.ApartmentId).ToList();
                var reviewStats = await _reviewsContext.Reviews
                    .AsNoTracking()
                    .Where(r => r.ApartmentId.HasValue && apartmentIds.Contains(r.ApartmentId.Value) && r.IsPublic)
                    .GroupBy(r => r.ApartmentId)
                    .Select(g => new
                    {
                        ApartmentId = g.Key,
                        AverageRating = g.Average(r => (decimal?)r.Rating),
                        ReviewCount = g.Count()
                    })
                    .ToDictionaryAsync(x => x.ApartmentId ?? 0, x => x, ct);

                var items = apartments
                    .Select(a =>
                    {
                        var s = reviewStats.TryGetValue(a.ApartmentId, out var stats) ? stats : null;
                        return a.ToDto(s?.AverageRating, s?.ReviewCount ?? 0);
                    })
                    .ToList();

                return new PagedResult<ApartmentDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = filters.Page,
                    PageSize = filters.PageSize
                };
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            });
    }

    public async Task<KeysetPagedResult<ApartmentDto>> GetAllApartmentsKeysetAsync(
        ApartmentFilterDto filters,
        int? afterId,
        int pageSize = 20)
    {
        var query = _context.Apartments.AsNoTracking().ApplyFilters(filters);

        // Keyset: apply cursor BEFORE ordering so the DB can use an index seek.
        if (afterId.HasValue)
            query = query.Where(a => a.ApartmentId > afterId.Value);

        var orderedQuery = query
            .Include(a => a.ApartmentImages)
            .OrderBy(a => a.ApartmentId);

        // Single query: take pageSize+1 (for HasNextPage detection) with images in split query
        var apartments = await orderedQuery
            .AsSplitQuery()
            .Take(pageSize + 1)
            .ToListAsync();

        var apartmentIds = apartments.Select(a => a.ApartmentId).ToList();

        var reviewStats = await _reviewsContext.Reviews
            .AsNoTracking()
            .Where(r => r.ApartmentId.HasValue && apartmentIds.Contains(r.ApartmentId.Value) && r.IsPublic)
            .GroupBy(r => r.ApartmentId)
            .Select(g => new
            {
                ApartmentId = g.Key,
                AverageRating = g.Average(r => (decimal?)r.Rating),
                ReviewCount = g.Count()
            })
            .ToDictionaryAsync(x => x.ApartmentId ?? 0, x => x);

        var dtoQuery = apartments
            .OrderBy(a => a.ApartmentId)
            .Select(a =>
            {
                var s = reviewStats.TryGetValue(a.ApartmentId, out var stats) ? stats : null;
                return a.ToDto(s?.AverageRating, s?.ReviewCount ?? 0);
            })
            .AsQueryable();

        return await dtoQuery.ToKeysetPagedResultAsync(afterId, pageSize, dto => dto.ApartmentId);
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
                landlordId = user.UserId;
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
        var apartmentIds = apartments.Select(a => a.ApartmentId).ToList();
        var reviewStats = await _reviewsContext.Reviews
            .Where(r => r.ApartmentId.HasValue && apartmentIds.Contains(r.ApartmentId.Value) && r.IsPublic)
            .GroupBy(r => r.ApartmentId)
            .Select(g => new
            {
                ApartmentId = g.Key,
                AverageRating = g.Average(r => (decimal?)r.Rating),
                ReviewCount = g.Count()
            })
            .ToDictionaryAsync(x => x.ApartmentId ?? 0, x => x);

        var items = apartments
            .Select(a =>
            {
                var s = reviewStats.TryGetValue(a.ApartmentId, out var stats) ? stats : null;
                return a.ToDto(s?.AverageRating, s?.ReviewCount ?? 0, imageLimit: 0);
            })
            .ToList();

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
        var ctx = _httpContextAccessor.HttpContext;
        var userIdClaim = ctx?.User?.FindFirstValue("userId");
        int? userId = int.TryParse(userIdClaim, out var parsedId) ? parsedId : null;

        var apartment = await _context.Apartments
            .Include(a => a.ApartmentImages.Where(img => !img.IsDeleted))
            .AsNoTracking()
            .Where(a => a.ApartmentId == apartmentId && !a.IsDeleted)
            .FirstOrDefaultAsync();
        if (apartment == null)
            return null;

        Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate.User? landlord = null;
        if (apartment.LandlordId.HasValue)
        {
            landlord = await _usersContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == apartment.LandlordId.Value);
        }
        var reviewStats = await _reviewsContext.Reviews
            .Where(r => r.ApartmentId == apartmentId && r.IsPublic)
            .GroupBy(r => r.ApartmentId)
            .Select(g => new
            {
                AverageRating = g.Average(r => (decimal?)r.Rating),
                ReviewCount = g.Count()
            })
            .FirstOrDefaultAsync();

        // Fire-and-forget AFTER all DB queries — avoids concurrent scoped DbContext access.
        _ = _analyticsService.TrackEventAsync(
            "ApartmentView", "Listings",
            entityId: apartmentId,
            entityType: "Apartment",
            userId: userId,
            ipAddress: ctx?.Connection.RemoteIpAddress?.ToString(),
            userAgent: ctx?.Request.Headers["User-Agent"].ToString());

        var features = ApartmentFeaturesHelper.Deserialize(apartment.Features);
        return new GetApartmentDto
        {
            ApartmentId = apartment.ApartmentId,
            Title = apartment.Title,
            Description = apartment.Description ?? string.Empty,
            Rent = apartment.Rent,
            Price = apartment.Price,
            Address = apartment.Address,
            City = apartment.City ?? string.Empty,
            PostalCode = apartment.PostalCode ?? string.Empty,
            AvailableFrom = apartment.AvailableFrom ?? DateOnly.FromDateTime(DateTime.Now),
            AvailableUntil = apartment.AvailableUntil ?? DateOnly.FromDateTime(DateTime.Now.AddYears(1)),
            NumberOfRooms = apartment.NumberOfRooms ?? 0,
            RentIncludeUtilities = apartment.RentIncludeUtilities ?? false,
            Latitude = apartment.Latitude,
            Longitude = apartment.Longitude,
            SizeSquareMeters = apartment.SizeSquareMeters,
            ApartmentType = apartment.ApartmentType,
            ListingType = apartment.ListingType,
            IsFurnished = features.IsFurnished,
            HasBalcony = features.HasBalcony,
            HasElevator = features.HasElevator,
            HasParking = features.HasParking,
            HasInternet = features.HasInternet,
            HasAirCondition = features.HasAirCondition,
            IsPetFriendly = features.IsPetFriendly,
            IsSmokingAllowed = features.IsSmokingAllowed,
            DepositAmount = apartment.DepositAmount,
            MinimumStayMonths = apartment.MinimumStayMonths,
            MaximumStayMonths = apartment.MaximumStayMonths,
            IsImmediatelyAvailable = apartment.IsImmediatelyAvailable,
            IsLookingForRoommate = apartment.IsLookingForRoommate,
            ContactPhone = apartment.ContactPhone,
            LandlordId = apartment.LandlordId,
            LandlordName = landlord?.FirstName != null ? $"{landlord.FirstName} {landlord.LastName}" : "Unknown",
            LandlordEmail = landlord?.Email,
            AverageRating = reviewStats?.AverageRating ?? 0,
            ReviewCount = reviewStats?.ReviewCount ?? 0,
            ApartmentImages = apartment.ApartmentImages?
                .Where(img => !img.IsDeleted)
                .OrderBy(img => img.DisplayOrder)
                .Select(img => new ApartmentImageDto
                {
                    ImageId = img.ImageId,
                    ApartmentId = img.ApartmentId,
                    ImageUrl = img.ImageUrl,
                    IsPrimary = img.IsPrimary
                }).ToList(),
            IsFeatured = apartment.IsFeatured,
            FeaturedUntil = apartment.FeaturedUntil
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

        return apartments.Select(a => a.ToDto(imageLimit: 0)).ToList();
    }

    // .NET 10 Feature: Vector Search implementation
    // Only loads active, non-deleted listings and caps at 1 000 rows to avoid
    // materialising the entire table into memory during embedding comparison.
    public async Task<List<ApartmentDto>> GetAllApartmentsForSemanticSearchAsync()
    {
        var apartments = await _context.Apartments
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.IsActive)
            .OrderByDescending(a => a.CreatedDate)
            .Take(1000)
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
}
