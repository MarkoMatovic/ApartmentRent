using System.Security.Claims;
using Lander.src.Modules.ApartmentApplications.Dtos.Dto;
using Lander.src.Modules.ApartmentApplications.Dtos.InputDto;
using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.Listings.Models;

namespace Lander.src.Modules.ApartmentApplications.Implementation;

public class ApartmentService : IApartmentService
{
    private readonly ListingsContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApartmentService(ListingsContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<ApartmentDto> CreateApartmentAsync(ApartmentInputDto apartmentInputDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

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
            CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : (Guid?)null,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : (Guid?)null,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Apartments.Add(apartment);
       
        var apartmentImages = apartmentInputDto.ImageUrls.Select(url => new ApartmentImage
        {
            ApartmentId = apartment.ApartmentId,
            ImageUrl = url,
            CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : (Guid?)null,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : (Guid?)null,
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
}

