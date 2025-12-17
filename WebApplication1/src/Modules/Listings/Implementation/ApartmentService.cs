using System;
using System.Security.Claims;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Lander.src.Modules.Listings.Implementation;

public class ApartmentService : IApartmentService
{
    private readonly ListingsContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApartmentService(ListingsContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
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

        await _context.SaveChangesAsync();
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

        _context.ApartmentImages.AddRange(apartmentImages);
        await _context.SaveChangesAsync();

        return new ApartmentDto
        {
            ApartmentId = apartment.ApartmentId,
            Title = apartment.Title,
            Rent = apartment.Rent,
            Address = apartment.Address,
            City = apartment.City
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

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ApartmentDto>> GetAllApartmentsAsync()
    {
        var apartments = await _context.Apartments
            .Where(a => !a.IsDeleted && a.IsActive)
            .Select(a => new ApartmentDto
            {
                ApartmentId = a.ApartmentId,
                Title = a.Title,
                Rent = a.Rent,
                Address = a.Address,
                City = a.City ?? string.Empty
            })
            .ToListAsync();

        return apartments;
    }

    public async Task<GetApartmentDto> GetApartmentByIdAsync(int apartmentId)
    {
        var apartment = await _context.Apartments
            .Where(a => a.ApartmentId == apartmentId)
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
            RentIncludeUtilities = (bool)apartment.RentIncludeUtilities
        };

    
    }
}

