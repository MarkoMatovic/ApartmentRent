using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;

namespace Lander.src.Modules.Listings.Interfaces;

public interface IApartmentService
{
    Task<ApartmentDto> CreateApartmentAsync(ApartmentInputDto apartmentInputDto);
    Task<GetApartmentDto> GetApartmentByIdAsync(int apartmentId);
    Task<bool> DeleteApartmentAsync(int apartmentId);
}
