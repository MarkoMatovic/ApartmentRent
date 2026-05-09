using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.MachineLearning.Services;

namespace Lander.src.Modules.Listings.Interfaces;

/// <summary>
/// Write / mutation operations on apartments.  Kept separate from
/// <see cref="IApartmentQueryService"/> so the write path can be deployed,
/// scaled, or replaced independently of the read path in the future.
/// </summary>
public interface IApartmentCommandService
{
    Task<ApartmentDto> CreateApartmentAsync(ApartmentInputDto apartmentInputDto);
    Task<ApartmentDto> UpdateApartmentAsync(int apartmentId, ApartmentUpdateInputDto updateDto);
    Task<bool> DeleteApartmentAsync(int apartmentId);
    Task DeleteApartmentsByLandlordIdAsync(int landlordId);
    Task<bool> ActivateApartmentAsync(int apartmentId);

    // Embedding generation is a write side concern (updates DescriptionEmbedding column)
    Task<int> GenerateEmbeddingsForAllApartmentsAsync(SimpleEmbeddingService embeddingService);
}
