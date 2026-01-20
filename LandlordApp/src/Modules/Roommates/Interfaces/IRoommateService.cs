using Lander.src.Common;
using Lander.src.Modules.Roommates.Dtos.Dto;
using Lander.src.Modules.Roommates.Dtos.InputDto;
namespace Lander.src.Modules.Roommates.Interfaces;
public interface IRoommateService
{
    Task<IEnumerable<RoommateDto>> GetAllRoommatesAsync(string? location = null, decimal? minBudget = null, decimal? maxBudget = null, 
        bool? smokingAllowed = null, bool? petFriendly = null, string? lifestyle = null, int? apartmentId = null);
    Task<PagedResult<RoommateDto>> GetAllRoommatesAsync(string? location, decimal? minBudget, decimal? maxBudget, 
        bool? smokingAllowed, bool? petFriendly, string? lifestyle, int? apartmentId, int page, int pageSize);
    Task<RoommateDto?> GetRoommateByIdAsync(int id);
    Task<RoommateDto?> GetRoommateByUserIdAsync(int userId);
    Task<RoommateDto> CreateRoommateAsync(int userId, RoommateInputDto input);
    Task<RoommateDto> UpdateRoommateAsync(int id, int userId, RoommateInputDto input);
    Task<bool> DeleteRoommateAsync(int id, int userId);
}
