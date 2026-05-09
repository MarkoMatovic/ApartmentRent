using Lander.src.Common;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.MachineLearning.Services;

namespace Lander.src.Modules.Listings.Interfaces;

/// <summary>
/// Read-only apartment operations.  Separated from write operations so that
/// read-heavy consumers (public search pages, admin dashboards) can depend only
/// on this narrower interface — and so both sides can evolve independently.
/// </summary>
public interface IApartmentQueryService
{
    Task<PagedResult<ApartmentDto>> GetAllApartmentsAsync(ApartmentFilterDto filters);
    Task<KeysetPagedResult<ApartmentDto>> GetAllApartmentsKeysetAsync(
        ApartmentFilterDto filters, int? afterId, int pageSize = 20);
    Task<PagedResult<ApartmentDto>> GetMyApartmentsAsync();
    Task<GetApartmentDto> GetApartmentByIdAsync(int apartmentId);
    Task<List<ApartmentDto>> GetApartmentsByLandlordIdAsync(int landlordId);

    // Used by the ML vector-search pipeline
    Task<List<ApartmentDto>> GetAllApartmentsForSemanticSearchAsync();
}
