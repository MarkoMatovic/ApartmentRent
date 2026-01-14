using System;
using System.Security.Claims;
using Lander.src.Common;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Lander.src.Modules.Listings.Implementation;

public class ApartmentService : IApartmentService
{
    private readonly ListingsContext _context;
    private readonly UsersContext _usersContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserInterface _userInterface;

    public ApartmentService(ListingsContext context, UsersContext usersContext, IHttpContextAccessor httpContextAccessor, IUserInterface userInterface)
    {
        _context = context;
        _usersContext = usersContext;
        _httpContextAccessor = httpContextAccessor;
        _userInterface = userInterface;
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

        // Get LandlordId from user Guid
        if (currentUserGuid != null && Guid.TryParse(currentUserGuid, out Guid parsedGuid))
        {
            var user = await _userInterface.GetUserByGuidAsync(parsedGuid);
            if (user != null)
            {
                landlordId = user.UserId;
            }
        }

        var apartment = new Apartment
        {
            LandlordId = landlordId,
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
            Latitude = apartmentInputDto.Latitude,
            Longitude = apartmentInputDto.Longitude,
            SizeSquareMeters = apartmentInputDto.SizeSquareMeters,
            ApartmentType = apartmentInputDto.ApartmentType ?? ApartmentType.Studio,
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

            // Now create images with the correct ApartmentId
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
            // Log the exception for debugging
            System.Diagnostics.Debug.WriteLine($"Error creating apartment: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            throw;
        }

        return new ApartmentDto
        {
            ApartmentId = apartment.ApartmentId,
            Title = apartment.Title,
            Rent = apartment.Rent,
            Address = apartment.Address,
            City = apartment.City,
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
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub") 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        
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
            .Include(a => a.ApartmentImages)
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
        var query = _context.Apartments
            .Where(a => !a.IsDeleted && a.IsActive)
            .AsNoTracking();

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

        var totalCount = await query.CountAsync();

        var apartments = await query
            .Include(a => a.ApartmentImages)
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
            Items = items,
            TotalCount = totalCount,
            Page = filters.Page,
            PageSize = filters.PageSize
        };
    }

    public async Task<PagedResult<ApartmentDto>> GetMyApartmentsAsync()
    {
        // Try both "sub" and ClaimTypes.NameIdentifier to be safe
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub") 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int? landlordId = null;

        // Debug logging
        System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: currentUserGuid = {currentUserGuid}");
        System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: All claims: {string.Join(", ", _httpContextAccessor.HttpContext?.User?.Claims.Select(c => $"{c.Type}={c.Value}") ?? new List<string>())}");

        // Get LandlordId from user Guid
        if (currentUserGuid != null && Guid.TryParse(currentUserGuid, out Guid parsedGuid))
        {
            var user = await _userInterface.GetUserByGuidAsync(parsedGuid);
            if (user != null)
            {
                landlordId = user.UserId;
                System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: Found user with UserId = {landlordId}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: User not found for Guid = {parsedGuid}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: currentUserGuid is null or invalid");
        }

        if (!landlordId.HasValue)
        {
            System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: landlordId is null, returning empty list");
            return new PagedResult<ApartmentDto>
            {
                Items = new List<ApartmentDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            };
        }

        // First, let's check all apartments to see what we have
        var allApartmentsCount = await _context.Apartments.CountAsync();
        var apartmentsWithLandlordId = await _context.Apartments
            .Where(a => a.LandlordId != null)
            .CountAsync();
        var apartmentsWithThisLandlordId = await _context.Apartments
            .Where(a => a.LandlordId == landlordId.Value)
            .CountAsync();
        
        System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: Total apartments in DB: {allApartmentsCount}");
        System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: Apartments with LandlordId set: {apartmentsWithLandlordId}");
        System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: Apartments with LandlordId = {landlordId.Value}: {apartmentsWithThisLandlordId}");

        var query = _context.Apartments
            .Where(a => !a.IsDeleted && a.LandlordId == landlordId.Value)
            .AsNoTracking();

        System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: Querying apartments with LandlordId = {landlordId.Value}");

        var totalCount = await query.CountAsync();
        System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: Found {totalCount} apartments in database (not deleted)");

        var apartments = await query
            .Include(a => a.ApartmentImages.Where(img => !img.IsDeleted))
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();

        System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: Loaded {apartments.Count} apartments with images");

        var items = apartments.Select(a => new ApartmentDto
        {
            ApartmentId = a.ApartmentId,
            Title = a.Title,
            Rent = a.Rent,
            Address = a.Address,
            City = a.City ?? string.Empty,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
            SizeSquareMeters = a.SizeSquareMeters,
            ApartmentType = a.ApartmentType,
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

        System.Diagnostics.Debug.WriteLine($"GetMyApartmentsAsync: Returning {items.Count} items");

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
            IsLookingForRoommate = apartment.IsLookingForRoommate,
            LandlordId = apartment.LandlordId,
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
            Address = apartment.Address,
            City = apartment.City,
            Latitude = apartment.Latitude,
            Longitude = apartment.Longitude,
            SizeSquareMeters = apartment.SizeSquareMeters,
            ApartmentType = apartment.ApartmentType,
            IsFurnished = apartment.IsFurnished,
            IsImmediatelyAvailable = apartment.IsImmediatelyAvailable
        };
    }
}

