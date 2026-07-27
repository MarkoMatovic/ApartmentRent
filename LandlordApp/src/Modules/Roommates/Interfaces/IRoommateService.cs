using Lander.src.Common;
using Lander.src.Modules.Roommates.Dtos.Dto;
using Lander.src.Modules.Roommates.Dtos.InputDto;
using Lander.src.Modules.Roommates.Models;
namespace Lander.src.Modules.Roommates.Interfaces;
public interface IRoommateService
{
    Task<PagedResult<RoommateDto>> GetAllRoommatesAsync(RoommateFilterQuery filter);
    Task<RoommateDto?> GetRoommateByIdAsync(int id);
    Task<RoommateDto?> GetRoommateByUserIdAsync(int userId);
    Task<RoommateDto> CreateRoommateAsync(int userId, RoommateInputDto input);
    Task<RoommateDto> UpdateRoommateAsync(int id, int userId, RoommateInputDto input);
    Task<bool> DeleteRoommateAsync(int id, int userId);
    Task<bool> DeleteRoommateByUserIdAsync(int userId);
}
