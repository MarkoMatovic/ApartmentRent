using System;
using System.Security.Claims;
using Lander.src.Common;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Lander.src.Modules.Listings.Implementation;

public class ApartmentService : IApartmentService
{
    private readonly ListingsContext _context;
    private readonly UsersContext _usersContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApartmentService(ListingsContext context, UsersContext usersContext, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _usersContext = usersContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> ActivateApartmentAsync(int apartmentId)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

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
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        var apartment = new Apartment
        {
            Title = apartmentInputDto.Title,
            Description = apartmentInputDto.Description,
            Rent = apartmentInputDto.Rent,
            Address = apartmentInputDto.Address,
            City = apartmentInputDto.City,
            PostalCode = apartmentInputDto.PostalCode,
            AvailableFrom = apartmentInputDto.AvailableFrom,
            AvailableUntil = apartmentInputDto.AvailableUntil,
            NumberOfRooms = apartmentInputDto.NumberOfRooms,
            RentIncludeUtilities = apartmentInputDto.RentIncludeUtilities,
            // Location (for maps & search)
            Latitude = apartmentInputDto.Latitude,
            Longitude = apartmentInputDto.Longitude,
            // Apartment characteristics (filters)
            SizeSquareMeters = apartmentInputDto.SizeSquareMeters,
            ApartmentType = apartmentInputDto.ApartmentType ?? ApartmentType.Studio,
            // Furnishing & amenities
            IsFurnished = apartmentInputDto.IsFurnished ?? false,
            HasBalcony = apartmentInputDto.HasBalcony ?? false,
            HasElevator = apartmentInputDto.HasElevator ?? false,
            HasParking = apartmentInputDto.HasParking ?? false,
            HasInternet = apartmentInputDto.HasInternet ?? false,
            HasAirCondition = apartmentInputDto.HasAirCondition ?? false,
            // Rules
            IsPetFriendly = apartmentInputDto.IsPetFriendly ?? false,
            IsSmokingAllowed = apartmentInputDto.IsSmokingAllowed ?? false,
            // Availability & rental terms
            DepositAmount = apartmentInputDto.DepositAmount,
            MinimumStayMonths = apartmentInputDto.MinimumStayMonths,
            MaximumStayMonths = apartmentInputDto.MaximumStayMonths,
            IsImmediatelyAvailable = apartmentInputDto.IsImmediatelyAvailable ?? false,
            CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Apartments.Add(apartment);

        var apartmentImages = apartmentInputDto.ImageUrls.Select(url => new ApartmentImage
        {
            ApartmentId = apartment.ApartmentId,
            ImageUrl = url,
            CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            ModifiedDate = DateTime.UtcNow
        }).ToList();

        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.ApartmentImages.AddRange(apartmentImages);
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
            Address = apartment.Address,
            City = apartment.City,
            // Extended fields (optional for backward compatibility)
            Latitude = apartment.Latitude,
            Longitude = apartment.Longitude,
            SizeSquareMeters = apartment.SizeSquareMeters,
            ApartmentType = apartment.ApartmentType,
            IsFurnished = apartment.IsFurnished,
            IsImmediatelyAvailable = apartment.IsImmediatelyAvailable
        };
    }

    public async Task<bool> DeleteApartmentAsync(int apartmentId)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var apartment = await _context.Apartments
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId && !a.IsDeleted);


        if (apartment == null) return false;

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
            .Where(a => !a.IsDeleted && a.IsActive)
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

        var landlordIds = apartments.Where(a => a.LandlordId.HasValue).Select(a => a.LandlordId!.Value).Distinct().ToList();
        
        var landlordLookingForRoommate = await _usersContext.Users
            .Where(u => landlordIds.Contains(u.UserId) && u.IsLookingForRoommate)
            .Select(u => u.UserId)
            .ToListAsync();

        var apartmentDtos = apartments.Select(a => new ApartmentDto
        {
            ApartmentId = a.Apartment.ApartmentId,
            Title = a.Apartment.Title,
            Rent = a.Apartment.Rent,
            Address = a.Apartment.Address,
            City = a.Apartment.City ?? string.Empty,
            Latitude = a.Apartment.Latitude,
            Longitude = a.Apartment.Longitude,
            SizeSquareMeters = a.Apartment.SizeSquareMeters,
            ApartmentType = a.Apartment.ApartmentType,
            IsFurnished = a.Apartment.IsFurnished,
            IsImmediatelyAvailable = a.Apartment.IsImmediatelyAvailable,
            IsLookingForRoommate = a.LandlordId.HasValue && landlordLookingForRoommate.Contains(a.LandlordId.Value),
            ApartmentImages = a.Apartment.ApartmentImages
                .Where(img => !img.IsDeleted)
                .OrderByDescending(img => img.IsPrimary)
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
        Console.WriteLine($"[ApartmentService] Filters received - City: '{filters.City}', MinRent: {filters.MinRent}, MaxRent: {filters.MaxRent}, Page: {filters.Page}, PageSize: {filters.PageSize}");
        
        var query = _context.Apartments
            .Where(a => !a.IsDeleted && a.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(filters.City))
        {
            Console.WriteLine($"[ApartmentService] Applying City filter: {filters.City}");
            query = query.Where(a => a.City != null && a.City.Contains(filters.City));
        }

        if (filters.MinRent.HasValue)
        {
            Console.WriteLine($"[ApartmentService] Applying MinRent filter: {filters.MinRent.Value}");
            query = query.Where(a => a.Rent >= filters.MinRent.Value);
        }

        if (filters.MaxRent.HasValue)
        {
            Console.WriteLine($"[ApartmentService] Applying MaxRent filter: {filters.MaxRent.Value}");
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

        var totalCount = await query.CountAsync();

        var apartments = await query
            .OrderBy(a => a.Rent)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .AsSplitQuery()
            .Select(a => new
            {
                Apartment = a,
                LandlordId = a.LandlordId
            })
            .ToListAsync();

        var landlordIds = apartments.Where(a => a.LandlordId.HasValue).Select(a => a.LandlordId!.Value).Distinct().ToList();
        
        var landlordLookingForRoommate = await _usersContext.Users
            .Where(u => landlordIds.Contains(u.UserId) && u.IsLookingForRoommate)
            .Select(u => u.UserId)
            .ToListAsync();

        var items = apartments.Select(a => new ApartmentDto
        {
            ApartmentId = a.Apartment.ApartmentId,
            Title = a.Apartment.Title,
            Rent = a.Apartment.Rent,
            Address = a.Apartment.Address,
            City = a.Apartment.City ?? string.Empty,
            Latitude = a.Apartment.Latitude,
            Longitude = a.Apartment.Longitude,
            SizeSquareMeters = a.Apartment.SizeSquareMeters,
            ApartmentType = a.Apartment.ApartmentType,
            IsFurnished = a.Apartment.IsFurnished,
            IsImmediatelyAvailable = a.Apartment.IsImmediatelyAvailable,
            IsLookingForRoommate = a.LandlordId.HasValue && landlordLookingForRoommate.Contains(a.LandlordId.Value),
            ApartmentImages = a.Apartment.ApartmentImages
                .Where(img => !img.IsDeleted)
                .OrderByDescending(img => img.IsPrimary)
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
            Items = items,
            TotalCount = totalCount,
            Page = filters.Page,
            PageSize = filters.PageSize
        };
    }

    public async Task<GetApartmentDto> GetApartmentByIdAsync(int apartmentId)
    {
        var apartment = await _context.Apartments
            .Include(a => a.ApartmentImages)
            .AsNoTracking()
            .Where(a => a.ApartmentId == apartmentId)
            .FirstOrDefaultAsync();


        if (apartment == null)
        {
            return null;
        }

        bool isLookingForRoommate = false;
        if (apartment.LandlordId.HasValue)
        {
            isLookingForRoommate = await _usersContext.Users
                .Where(u => u.UserId == apartment.LandlordId.Value && u.IsLookingForRoommate)
                .AnyAsync();
        }

        return new GetApartmentDto
        {
            ApartmentId = apartment.ApartmentId,
            Title = apartment.Title,
            Description = apartment.Description,
            Rent = apartment.Rent,
            Address = apartment.Address,
            City = apartment.City,
            PostalCode = apartment.PostalCode,
            AvailableFrom = (DateOnly)apartment.AvailableFrom,
            AvailableUntil = (DateOnly)apartment.AvailableUntil,
            NumberOfRooms = (int)apartment.NumberOfRooms,
            RentIncludeUtilities = (bool)apartment.RentIncludeUtilities,
            Latitude = apartment.Latitude,
            Longitude = apartment.Longitude,
            SizeSquareMeters = apartment.SizeSquareMeters,
            ApartmentType = apartment.ApartmentType,
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
            IsLookingForRoommate = isLookingForRoommate,
            ApartmentImages = apartment.ApartmentImages?.Select(img => new ApartmentImageDto
            {
                ImageId = img.ImageId,
                ApartmentId = img.ApartmentId,
                ImageUrl = img.ImageUrl
            }).ToList()
        };

    
    }
}

