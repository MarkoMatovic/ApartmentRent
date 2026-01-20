using System.Security.Claims;
using Lander;
using Lander.src.Common;
using Lander.src.Modules.Roommates.Dtos.Dto;
using Lander.src.Modules.Roommates.Dtos.InputDto;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Roommates.Models;
using Microsoft.EntityFrameworkCore;
namespace Lander.src.Modules.Roommates.Implementation;
public class RoommateService : IRoommateService
{
    private readonly RoommatesContext _context;
    private readonly UsersContext _usersContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public RoommateService(RoommatesContext context, UsersContext usersContext, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _usersContext = usersContext;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<IEnumerable<RoommateDto>> GetAllRoommatesAsync(
        string? location = null, 
        decimal? minBudget = null, 
        decimal? maxBudget = null,
        bool? smokingAllowed = null, 
        bool? petFriendly = null, 
        string? lifestyle = null,
        int? apartmentId = null)
    {
        var query = _context.Roommates
            .Where(r => r.IsActive)
            .AsNoTracking();
        if (!string.IsNullOrEmpty(location))
        {
            query = query.Where(r => r.PreferredLocation != null && r.PreferredLocation.Contains(location));
        }
        if (minBudget.HasValue)
        {
            query = query.Where(r => r.BudgetMax == null || r.BudgetMax >= minBudget.Value);
        }
        if (maxBudget.HasValue)
        {
            query = query.Where(r => r.BudgetMin == null || r.BudgetMin <= maxBudget.Value);
        }
        if (smokingAllowed.HasValue)
        {
            query = query.Where(r => r.SmokingAllowed == smokingAllowed.Value);
        }
        if (petFriendly.HasValue)
        {
            query = query.Where(r => r.PetFriendly == petFriendly.Value);
        }
        if (!string.IsNullOrEmpty(lifestyle))
        {
            query = query.Where(r => r.Lifestyle == lifestyle);
        }
        if (apartmentId.HasValue)
        {
            query = query.Where(r => r.LookingForApartmentId == apartmentId.Value);
        }
        var roommates = await query.ToListAsync();
        var userIds = roommates.Select(r => r.UserId).Distinct().ToList();
        var users = await _usersContext.Users
            .Where(u => userIds.Contains(u.UserId))
            .AsNoTracking()
            .ToListAsync();
        var userDict = users.ToDictionary(u => u.UserId);
        var result = roommates.Select(r =>
        {
            var user = userDict.GetValueOrDefault(r.UserId);
            return new RoommateDto
            {
                RoommateId = r.RoommateId,
                UserId = r.UserId,
                FirstName = user?.FirstName ?? string.Empty,
                LastName = user?.LastName ?? string.Empty,
                ProfilePicture = user?.ProfilePicture,
                DateOfBirth = user?.DateOfBirth,
                PhoneNumber = user?.PhoneNumber,
                Bio = r.Bio,
                Hobbies = r.Hobbies,
                Profession = r.Profession,
                SmokingAllowed = r.SmokingAllowed,
                PetFriendly = r.PetFriendly,
                Lifestyle = r.Lifestyle,
                Cleanliness = r.Cleanliness,
                GuestsAllowed = r.GuestsAllowed,
                BudgetMin = r.BudgetMin,
                BudgetMax = r.BudgetMax,
                BudgetIncludes = r.BudgetIncludes,
                AvailableFrom = r.AvailableFrom,
                AvailableUntil = r.AvailableUntil,
                MinimumStayMonths = r.MinimumStayMonths,
                MaximumStayMonths = r.MaximumStayMonths,
                LookingForRoomType = r.LookingForRoomType,
                LookingForApartmentType = r.LookingForApartmentType,
                PreferredLocation = r.PreferredLocation,
                LookingForApartmentId = r.LookingForApartmentId,
                IsActive = r.IsActive
            };
        }).ToList();
        return result;
    }
    public async Task<PagedResult<RoommateDto>> GetAllRoommatesAsync(
        string? location, 
        decimal? minBudget, 
        decimal? maxBudget,
        bool? smokingAllowed, 
        bool? petFriendly, 
        string? lifestyle,
        int? apartmentId,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.Roommates
            .Where(r => r.IsActive)
            .AsNoTracking();
        if (!string.IsNullOrEmpty(location))
        {
            query = query.Where(r => r.PreferredLocation != null && r.PreferredLocation.Contains(location));
        }
        if (minBudget.HasValue)
        {
            query = query.Where(r => r.BudgetMax == null || r.BudgetMax >= minBudget.Value);
        }
        if (maxBudget.HasValue)
        {
            query = query.Where(r => r.BudgetMin == null || r.BudgetMin <= maxBudget.Value);
        }
        if (smokingAllowed.HasValue)
        {
            query = query.Where(r => r.SmokingAllowed == smokingAllowed.Value);
        }
        if (petFriendly.HasValue)
        {
            query = query.Where(r => r.PetFriendly == petFriendly.Value);
        }
        if (!string.IsNullOrEmpty(lifestyle))
        {
            query = query.Where(r => r.Lifestyle == lifestyle);
        }
        if (apartmentId.HasValue)
        {
            query = query.Where(r => r.LookingForApartmentId == apartmentId.Value);
        }
        var totalCount = await query.CountAsync();
        var roommates = await query
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var userIds = roommates.Select(r => r.UserId).Distinct().ToList();
        var users = await _usersContext.Users
            .Where(u => userIds.Contains(u.UserId))
            .AsNoTracking()
            .ToListAsync();
        var userDict = users.ToDictionary(u => u.UserId);
        var result = roommates.Select(r =>
        {
            var user = userDict.GetValueOrDefault(r.UserId);
            return new RoommateDto
            {
                RoommateId = r.RoommateId,
                UserId = r.UserId,
                FirstName = user?.FirstName ?? string.Empty,
                LastName = user?.LastName ?? string.Empty,
                ProfilePicture = user?.ProfilePicture,
                DateOfBirth = user?.DateOfBirth,
                PhoneNumber = user?.PhoneNumber,
                Bio = r.Bio,
                Hobbies = r.Hobbies,
                Profession = r.Profession,
                SmokingAllowed = r.SmokingAllowed,
                PetFriendly = r.PetFriendly,
                Lifestyle = r.Lifestyle,
                Cleanliness = r.Cleanliness,
                GuestsAllowed = r.GuestsAllowed,
                BudgetMin = r.BudgetMin,
                BudgetMax = r.BudgetMax,
                BudgetIncludes = r.BudgetIncludes,
                AvailableFrom = r.AvailableFrom,
                AvailableUntil = r.AvailableUntil,
                MinimumStayMonths = r.MinimumStayMonths,
                MaximumStayMonths = r.MaximumStayMonths,
                LookingForRoomType = r.LookingForRoomType,
                LookingForApartmentType = r.LookingForApartmentType,
                PreferredLocation = r.PreferredLocation,
                LookingForApartmentId = r.LookingForApartmentId,
                IsActive = r.IsActive
            };
        }).ToList();
        return new PagedResult<RoommateDto>
        {
            Items = result,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
    public async Task<RoommateDto?> GetRoommateByIdAsync(int id)
    {
        var roommate = await _context.Roommates
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoommateId == id && r.IsActive);
        if (roommate == null) return null;
        var user = await _usersContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == roommate.UserId);
        if (user == null) return null;
        return new RoommateDto
        {
            RoommateId = roommate.RoommateId,
            UserId = roommate.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePicture = user.ProfilePicture,
            DateOfBirth = user.DateOfBirth,
            PhoneNumber = user.PhoneNumber,
            Bio = roommate.Bio,
            Hobbies = roommate.Hobbies,
            Profession = roommate.Profession,
            SmokingAllowed = roommate.SmokingAllowed,
            PetFriendly = roommate.PetFriendly,
            Lifestyle = roommate.Lifestyle,
            Cleanliness = roommate.Cleanliness,
            GuestsAllowed = roommate.GuestsAllowed,
            BudgetMin = roommate.BudgetMin,
            BudgetMax = roommate.BudgetMax,
            BudgetIncludes = roommate.BudgetIncludes,
            AvailableFrom = roommate.AvailableFrom,
            AvailableUntil = roommate.AvailableUntil,
            MinimumStayMonths = roommate.MinimumStayMonths,
            MaximumStayMonths = roommate.MaximumStayMonths,
            LookingForRoomType = roommate.LookingForRoomType,
            LookingForApartmentType = roommate.LookingForApartmentType,
            PreferredLocation = roommate.PreferredLocation,
            IsActive = roommate.IsActive
        };
    }
    public async Task<RoommateDto?> GetRoommateByUserIdAsync(int userId)
    {
        var roommate = await _context.Roommates
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive);
        if (roommate == null) return null;
        return await GetRoommateByIdAsync(roommate.RoommateId);
    }
    public async Task<RoommateDto> CreateRoommateAsync(int userId, RoommateInputDto input)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var existingRoommate = await _context.Roommates
            .FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive);
        var user = await _usersContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) throw new Exception("User not found");
        Roommate roommate;
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            if (existingRoommate != null)
            {
                roommate = existingRoommate;
                roommate.Bio = input.Bio;
                roommate.Hobbies = input.Hobbies;
                roommate.Profession = input.Profession;
                roommate.SmokingAllowed = input.SmokingAllowed;
                roommate.PetFriendly = input.PetFriendly;
                roommate.Lifestyle = input.Lifestyle;
                roommate.Cleanliness = input.Cleanliness;
                roommate.GuestsAllowed = input.GuestsAllowed;
                roommate.BudgetMin = input.BudgetMin;
                roommate.BudgetMax = input.BudgetMax;
                roommate.BudgetIncludes = input.BudgetIncludes;
                roommate.AvailableFrom = input.AvailableFrom;
                roommate.AvailableUntil = input.AvailableUntil;
                roommate.MinimumStayMonths = input.MinimumStayMonths;
                roommate.MaximumStayMonths = input.MaximumStayMonths;
                roommate.LookingForRoomType = input.LookingForRoomType;
                roommate.LookingForApartmentType = input.LookingForApartmentType;
                roommate.PreferredLocation = input.PreferredLocation;
                roommate.LookingForApartmentId = input.LookingForApartmentId;
                roommate.IsActive = true;
                roommate.ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null;
                roommate.ModifiedDate = DateTime.UtcNow;
                _context.Roommates.Update(roommate);
            }
            else
            {
                roommate = new Roommate
                {
                    UserId = userId,
                    Bio = input.Bio,
                    Hobbies = input.Hobbies,
                    Profession = input.Profession,
                    SmokingAllowed = input.SmokingAllowed,
                    PetFriendly = input.PetFriendly,
                    Lifestyle = input.Lifestyle,
                    Cleanliness = input.Cleanliness,
                    GuestsAllowed = input.GuestsAllowed,
                    BudgetMin = input.BudgetMin,
                    BudgetMax = input.BudgetMax,
                    BudgetIncludes = input.BudgetIncludes,
                    AvailableFrom = input.AvailableFrom,
                    AvailableUntil = input.AvailableUntil,
                    MinimumStayMonths = input.MinimumStayMonths,
                    MaximumStayMonths = input.MaximumStayMonths,
                    LookingForRoomType = input.LookingForRoomType,
                    LookingForApartmentType = input.LookingForApartmentType,
                    PreferredLocation = input.PreferredLocation,
                    LookingForApartmentId = input.LookingForApartmentId,
                    IsActive = true,
                    CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
                    ModifiedDate = DateTime.UtcNow
                };
                _context.Roommates.Add(roommate);
            }
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        return new RoommateDto
        {
            RoommateId = roommate.RoommateId,
            UserId = roommate.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePicture = user.ProfilePicture,
            DateOfBirth = user.DateOfBirth,
            PhoneNumber = user.PhoneNumber,
            Bio = roommate.Bio,
            Hobbies = roommate.Hobbies,
            Profession = roommate.Profession,
            SmokingAllowed = roommate.SmokingAllowed,
            PetFriendly = roommate.PetFriendly,
            Lifestyle = roommate.Lifestyle,
            Cleanliness = roommate.Cleanliness,
            GuestsAllowed = roommate.GuestsAllowed,
            BudgetMin = roommate.BudgetMin,
            BudgetMax = roommate.BudgetMax,
            BudgetIncludes = roommate.BudgetIncludes,
            AvailableFrom = roommate.AvailableFrom,
            AvailableUntil = roommate.AvailableUntil,
            MinimumStayMonths = roommate.MinimumStayMonths,
            MaximumStayMonths = roommate.MaximumStayMonths,
            LookingForRoomType = roommate.LookingForRoomType,
            LookingForApartmentType = roommate.LookingForApartmentType,
            PreferredLocation = roommate.PreferredLocation,
            LookingForApartmentId = roommate.LookingForApartmentId,
            IsActive = roommate.IsActive
        };
    }
    public async Task<RoommateDto> UpdateRoommateAsync(int id, int userId, RoommateInputDto input)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var roommate = await _context.Roommates
            .FirstOrDefaultAsync(r => r.RoommateId == id && r.UserId == userId);
        if (roommate == null)
            throw new Exception("Roommate not found or you don't have permission to update it");
        roommate.Bio = input.Bio;
        roommate.Hobbies = input.Hobbies;
        roommate.Profession = input.Profession;
        roommate.SmokingAllowed = input.SmokingAllowed;
        roommate.PetFriendly = input.PetFriendly;
        roommate.Lifestyle = input.Lifestyle;
        roommate.Cleanliness = input.Cleanliness;
        roommate.GuestsAllowed = input.GuestsAllowed;
        roommate.BudgetMin = input.BudgetMin;
        roommate.BudgetMax = input.BudgetMax;
        roommate.BudgetIncludes = input.BudgetIncludes;
        roommate.AvailableFrom = input.AvailableFrom;
        roommate.AvailableUntil = input.AvailableUntil;
        roommate.MinimumStayMonths = input.MinimumStayMonths;
        roommate.MaximumStayMonths = input.MaximumStayMonths;
        roommate.LookingForRoomType = input.LookingForRoomType;
        roommate.LookingForApartmentType = input.LookingForApartmentType;
        roommate.PreferredLocation = input.PreferredLocation;
        roommate.ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null;
        roommate.ModifiedDate = DateTime.UtcNow;
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        return await GetRoommateByIdAsync(roommate.RoommateId) ?? throw new Exception("Failed to update roommate");
    }
    public async Task<bool> DeleteRoommateAsync(int id, int userId)
    {
        var roommate = await _context.Roommates
            .FirstOrDefaultAsync(r => r.RoommateId == id && r.UserId == userId);
        if (roommate == null) return false;
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            roommate.IsActive = false;
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        return true;
    }
}
