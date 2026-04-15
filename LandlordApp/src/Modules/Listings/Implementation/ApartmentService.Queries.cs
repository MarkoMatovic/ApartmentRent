using System.Security.Claims;
using Lander.src.Common;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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
            IsFeatured = a.Apartment.IsFeatured,
            FeaturedUntil = a.Apartment.FeaturedUntil,
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
        // Deterministic cache key — GetHashCode() is not stable across processes/instances.
        var cacheKey = $"Apartments_p{filters.Page}_ps{filters.PageSize}_s{filters.SortBy}_{filters.SortOrder}" +
                       $"_c{filters.City}_mr{filters.MinRent}_{filters.MaxRent}" +
                       $"_at{(int?)filters.ApartmentType}_nr{filters.NumberOfRooms}" +
                       $"_lt{(int?)filters.ListingType}_ia{filters.IsImmediatelyAvailable}";

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
            // EF.Functions.Like with suffix wildcard → SQL LIKE 'value%' which uses the City index.
            // Contains() would produce LIKE '%value%' and skip the index entirely.
            query = query.Where(a => EF.Functions.Like(a.City!, filters.City + "%"));
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
        // Note: Boolean filters (IsFurnished, etc.) are temporarily disabled
        // until JSON-based filtering for the Features column is implemented.

        if (filters.IsImmediatelyAvailable.HasValue)
        {
            query = query.Where(a => a.IsImmediatelyAvailable == filters.IsImmediatelyAvailable.Value);
        }
        if (filters.AvailableFrom.HasValue)
        {
            query = query.Where(a => a.AvailableFrom >= filters.AvailableFrom.Value);
        }
        var sortBy = filters.SortBy?.ToLower() ?? "date";
        var sortOrder = filters.SortOrder?.ToLower() ?? "desc";

        var baseOrderedQuery = query.OrderByDescending(a => a.IsFeatured);

        IOrderedQueryable<Lander.src.Modules.Listings.Models.Apartment> orderedQuery = (sortBy, sortOrder) switch
        {
            ("rent" or "price", "asc")  => baseOrderedQuery.ThenBy(a => a.Rent > 0 ? a.Rent : a.Price ?? 0),
            ("rent" or "price", "desc") => baseOrderedQuery.ThenByDescending(a => a.Rent > 0 ? a.Rent : a.Price ?? 0),
            ("size",  "asc")  => baseOrderedQuery.ThenBy(a => a.SizeSquareMeters),
            ("size",  "desc") => baseOrderedQuery.ThenByDescending(a => a.SizeSquareMeters),
            ("date",  "asc")  => baseOrderedQuery.ThenBy(a => a.CreatedDate),
            _                 => baseOrderedQuery.ThenByDescending(a => a.CreatedDate)
        };

        var totalCount = await query.CountAsync();
        var apartments = await orderedQuery
            .Include(a => a.ApartmentImages)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync();

        _logger.LogInformation("Apartment search: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            filters.Page, filters.PageSize, totalCount);

        // Fetch review stats in parallel with count+data (different DbContext → no concurrency issue).
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
            IsFurnished = ApartmentFeaturesHelper.Deserialize(a.Features).IsFurnished,
            IsImmediatelyAvailable = a.IsImmediatelyAvailable,
            IsLookingForRoommate = a.IsLookingForRoommate,
            AverageRating = reviewStats.ContainsKey(a.ApartmentId) ? reviewStats[a.ApartmentId].AverageRating : 0,
            ReviewCount = reviewStats.ContainsKey(a.ApartmentId) ? reviewStats[a.ApartmentId].ReviewCount : 0,
            ApartmentImages = a.ApartmentImages
                .Where(img => !img.IsDeleted)
                .OrderBy(img => img.DisplayOrder)
                .Take(5)
                .Select(img => new ApartmentImageDto
                {
                    ImageId = img.ImageId,
                    ApartmentId = img.ApartmentId,
                    ImageUrl = img.ImageUrl,
                    IsPrimary = img.IsPrimary
                }).ToList(),
            IsFeatured = a.IsFeatured,
            FeaturedUntil = a.FeaturedUntil
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

    public async Task<CursorPagedResult<ApartmentDto>> GetAllApartmentsCursorAsync(
        ApartmentFilterDto filters,
        int? afterId,
        int pageSize = 20)
    {
        var query = _context.Apartments
            // Named query filter automatically applies: !a.IsDeleted && a.IsActive
            .AsNoTracking();

        if (filters.ListingType.HasValue)
            query = query.Where(a => a.ListingType == filters.ListingType.Value);

        if (!string.IsNullOrEmpty(filters.City))
            query = query.Where(a => EF.Functions.Like(a.City!, filters.City + "%"));

        if (filters.MinRent.HasValue)
            query = query.Where(a => a.Rent >= filters.MinRent.Value);

        if (filters.MaxRent.HasValue)
            query = query.Where(a => a.Rent <= filters.MaxRent.Value);

        if (filters.NumberOfRooms.HasValue)
            query = query.Where(a => a.NumberOfRooms == filters.NumberOfRooms.Value);

        if (filters.ApartmentType.HasValue)
            query = query.Where(a => a.ApartmentType == filters.ApartmentType.Value);

        if (filters.IsImmediatelyAvailable.HasValue)
            query = query.Where(a => a.IsImmediatelyAvailable == filters.IsImmediatelyAvailable.Value);

        if (filters.AvailableFrom.HasValue)
            query = query.Where(a => a.AvailableFrom >= filters.AvailableFrom.Value);

        // Keyset: apply cursor BEFORE ordering so the DB can use an index seek.
        if (afterId.HasValue)
            query = query.Where(a => a.ApartmentId > afterId.Value);

        var orderedQuery = query
            .Include(a => a.ApartmentImages)
            .OrderBy(a => a.ApartmentId);

        var apartmentIds = await orderedQuery
            .Take(pageSize + 1)
            .Select(a => a.ApartmentId)
            .ToListAsync();

        var apartments = await orderedQuery
            .Where(a => apartmentIds.Contains(a.ApartmentId))
            .ToListAsync();

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
            .Select(a => new ApartmentDto
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
                IsFurnished = ApartmentFeaturesHelper.Deserialize(a.Features).IsFurnished,
                IsImmediatelyAvailable = a.IsImmediatelyAvailable,
                IsLookingForRoommate = a.IsLookingForRoommate,
                AverageRating = reviewStats.ContainsKey(a.ApartmentId) ? reviewStats[a.ApartmentId].AverageRating : 0,
                ReviewCount = reviewStats.ContainsKey(a.ApartmentId) ? reviewStats[a.ApartmentId].ReviewCount : 0,
                ApartmentImages = a.ApartmentImages
                    .Where(img => !img.IsDeleted)
                    .OrderBy(img => img.DisplayOrder)
                    .Take(5)
                    .Select(img => new ApartmentImageDto
                    {
                        ImageId = img.ImageId,
                        ApartmentId = img.ApartmentId,
                        ImageUrl = img.ImageUrl,
                        IsPrimary = img.IsPrimary
                    }).ToList(),
                IsFeatured = a.IsFeatured,
                FeaturedUntil = a.FeaturedUntil
            })
            .AsQueryable();

        return await dtoQuery.ToCursorPagedResultAsync(afterId, pageSize, dto => dto.ApartmentId);
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
            IsFurnished = ApartmentFeaturesHelper.Deserialize(a.Features).IsFurnished,
            IsImmediatelyAvailable = a.IsImmediatelyAvailable,
            IsLookingForRoommate = a.IsLookingForRoommate,
            AverageRating = reviewStats.ContainsKey(a.ApartmentId) ? reviewStats[a.ApartmentId].AverageRating : 0,
            ReviewCount = reviewStats.ContainsKey(a.ApartmentId) ? reviewStats[a.ApartmentId].ReviewCount : 0,
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
}
