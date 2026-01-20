using Lander.src.Modules.SavedSearches.Dtos.Dto;
using Lander.src.Modules.SavedSearches.Dtos.InputDto;
namespace Lander.src.Modules.SavedSearches.Interfaces;
public interface ISavedSearchService
{
    Task<IEnumerable<SavedSearchDto>> GetSavedSearchesByUserIdAsync(int userId);
    Task<SavedSearchDto?> GetSavedSearchByIdAsync(int id);
    Task<SavedSearchDto> CreateSavedSearchAsync(int userId, SavedSearchInputDto input);
    Task<SavedSearchDto> UpdateSavedSearchAsync(int id, int userId, SavedSearchInputDto input);
    Task<bool> DeleteSavedSearchAsync(int id, int userId);
}
