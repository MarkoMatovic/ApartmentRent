using Lander.src.Modules.ApartmentApplications.Dtos.Dto;
using Lander.src.Modules.ApartmentApplications.Dtos.InputDto;

namespace Lander.src.Modules.ApartmentApplications.Interfaces;

public interface IApartmentService
{
    Task<ApartmentDto> CreateApartmentAsync(ApartmentInputDto apartmentInputDto);
}
