using System.Security.Claims;
using Lander.src.Common;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Helpers;
using Lander.src.Modules.Listings.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lander.src.Modules.Listings.Implementation;

public partial class ApartmentService
{
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
                var reviewStats = await _reviewStats.GetBatchAsync(apartmentIds, ct);

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

        var reviewStats = await _reviewStats.GetBatchAsync(apartmentIds);

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
            landlordId = await _userLookup.GetUserIdByGuidAsync(parsedGuid);
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
        // IgnoreQueryFilters: landlords must see ALL their own listings including inactive ones
        // (e.g. a temporarily deactivated listing they want to reactivate).
        // The IsDeleted guard is re-applied manually so deleted items stay hidden.
        // The filtered Include keeps its explicit guard because IgnoreQueryFilters() also
        // suppresses the ApartmentImage global query filter on the same query.

        // Hard cap: even a very prolific landlord should not be able to cause a
        // response that serialises thousands of apartments in one shot.
        const int HardCap = 200;

        var query = _context.Apartments
            .IgnoreQueryFilters()
            .Where(a => !a.IsDeleted && a.LandlordId == landlordId.Value)
            .AsNoTracking();
        var totalCount = await query.CountAsync();
        var apartments = await query
            .Include(a => a.ApartmentImages.Where(img => !img.IsDeleted))
            .OrderByDescending(a => a.CreatedDate)
            .AsSplitQuery()   // avoids cartesian join explosion when many images per apartment
            .Take(HardCap)
            .ToListAsync();
        var apartmentIds = apartments.Select(a => a.ApartmentId).ToList();
        var reviewStats = await _reviewStats.GetBatchAsync(apartmentIds);

        var items = apartments
            .Select(a =>
            {
                var s = reviewStats.TryGetValue(a.ApartmentId, out var stats) ? stats : null;
                return a.ToDto(s?.AverageRating, s?.ReviewCount ?? 0, imageLimit: 0);
            })
            .ToList();

        if (totalCount > HardCap)
            _logger.LogWarning(
                "GetMyApartmentsAsync: landlord {LandlordId} has {Total} apartments; response capped at {Cap}.",
                landlordId.Value, totalCount, HardCap);

        return new PagedResult<ApartmentDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = 1,
            PageSize = Math.Min(totalCount, HardCap)
        };
    }

    public async Task<GetApartmentDto> GetApartmentByIdAsync(int apartmentId)
    {
        var ctx = _httpContextAccessor.HttpContext;
        var userIdClaim = ctx?.User?.FindFirstValue("userId");
        int? userId = int.TryParse(userIdClaim, out var parsedId) ? parsedId : null;

        // Global query filters cover: !a.IsDeleted && a.IsActive (Apartment)
        //                             !img.IsDeleted           (ApartmentImage)
        var apartment = await _context.Apartments
            .Include(a => a.ApartmentImages)
            .AsNoTracking()
            .Where(a => a.ApartmentId == apartmentId)
            .FirstOrDefaultAsync();
        if (apartment == null)
            return null;

        LandlordBrief? landlord = null;
        if (apartment.LandlordId.HasValue)
            landlord = await _userLookup.GetLandlordBriefAsync(apartment.LandlordId.Value);

        var reviewStats = await _reviewStats.GetForApartmentAsync(apartmentId);

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
        // Global query filter handles !a.IsDeleted && a.IsActive — no manual check needed.
        var apartments = await _context.Apartments
            .Include(a => a.ApartmentImages)
            .AsNoTracking()
            .AsSplitQuery()
            .Where(a => a.LandlordId == landlordId)
            .OrderByDescending(a => a.CreatedDate)
            .Take(500) // safety cap — covers even very prolific landlords
            .ToListAsync();

        return apartments.Select(a => a.ToDto(imageLimit: 0)).ToList();
    }

    // .NET 10 Feature: Vector Search implementation
    // Only loads active, non-deleted listings and caps at 1 000 rows to avoid
    // materialising the entire table into memory during embedding comparison.
    public async Task<List<ApartmentDto>> GetAllApartmentsForSemanticSearchAsync()
    {
        // Global query filter automatically applies: !a.IsDeleted && a.IsActive
        var apartments = await _context.Apartments
            .AsNoTracking()
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
