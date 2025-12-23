using Lander.src.Common;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;

namespace Lander.src.Modules.Listings.Interfaces;

public interface IApartmentService
{
    Task<ApartmentDto> CreateApartmentAsync(ApartmentInputDto apartmentInputDto);
    Task<GetApartmentDto> GetApartmentByIdAsync(int apartmentId);
    Task<PagedResult<ApartmentDto>> GetAllApartmentsAsync();
    Task<PagedResult<ApartmentDto>> GetAllApartmentsAsync(ApartmentFilterDto filters);
    Task<ApartmentDto> UpdateApartmentAsync(int apartmentId, ApartmentUpdateInputDto updateDto);
    Task<bool> DeleteApartmentAsync(int apartmentId);
    Task<bool> ActivateApartmentAsync(int apartmentId);
}
