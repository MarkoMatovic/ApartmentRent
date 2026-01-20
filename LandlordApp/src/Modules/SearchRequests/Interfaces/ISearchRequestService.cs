using Lander.src.Common;
using Lander.src.Modules.SearchRequests.Dtos.Dto;
using Lander.src.Modules.SearchRequests.Dtos.InputDto;
using Lander.src.Modules.SearchRequests.Models;
namespace Lander.src.Modules.SearchRequests.Interfaces;
public interface ISearchRequestService
{
    Task<IEnumerable<SearchRequestDto>> GetAllSearchRequestsAsync(
        SearchRequestType? requestType = null,
        string? city = null,
        decimal? minBudget = null,
        decimal? maxBudget = null);
    Task<PagedResult<SearchRequestDto>> GetAllSearchRequestsAsync(
        SearchRequestType? requestType,
        string? city,
        decimal? minBudget,
        decimal? maxBudget,
        int page,
        int pageSize);
    Task<SearchRequestDto?> GetSearchRequestByIdAsync(int id);
    Task<IEnumerable<SearchRequestDto>> GetSearchRequestsByUserIdAsync(int userId);
    Task<SearchRequestDto> CreateSearchRequestAsync(int userId, SearchRequestInputDto input);
    Task<SearchRequestDto> UpdateSearchRequestAsync(int id, int userId, SearchRequestInputDto input);
    Task<bool> DeleteSearchRequestAsync(int id, int userId);
}
