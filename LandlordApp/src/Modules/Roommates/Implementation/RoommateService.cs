using System.Security.Claims;
using Lander.Helpers;
using Lander;
using Lander.src.Common;
using Lander.src.Common.Exceptions;
using Lander.src.Modules.Roommates.Dtos.Dto;
using Lander.src.Modules.Roommates.Dtos.InputDto;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Roommates.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace Lander.src.Modules.Roommates.Implementation;
public class RoommateService : IRoommateService
{
    private readonly RoommatesContext _context;
    private readonly UsersContext _usersContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;
    public RoommateService(RoommatesContext context, UsersContext usersContext, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
    {
        _context = context;
        _usersContext = usersContext;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }

    // List cache keys include a version number; bumping it on any mutation
    // invalidates all cached pages/filter combinations at once.
    private const string CacheVersionKey = "Roommates_CacheVersion";
    private long GetCacheVersion() => _cache.GetOrCreate(CacheVersionKey, _ => 0L);
    private void InvalidateListCache() => _cache.Set(CacheVersionKey, GetCacheVersion() + 1);
    public async Task<PagedResult<RoommateDto>> GetAllRoommatesAsync(
        string? location, 
        decimal? minBudget, 
        decimal? maxBudget,
        bool? smokingAllowed, 
        bool? petFriendly, 
        string? lifestyle,
        string? profession,
        DateOnly? availableFrom,
        int? stayDuration,
        int? apartmentId,
        RoommateGender? gender = null,
        WorkSchedule? workSchedule = null,
        int page = 1,
        int pageSize = 20)
    {
        // Clamp to prevent DoS via oversized page requests
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;

        var cacheKey = $"Roommates_v{GetCacheVersion()}_{location}_{minBudget}_{maxBudget}_{smokingAllowed}_{petFriendly}_{lifestyle}_{profession}_{availableFrom}_{stayDuration}_{apartmentId}_{gender}_{workSchedule}_{page}_{pageSize}";

        if (_cache.TryGetValue(cacheKey, out PagedResult<RoommateDto>? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

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
        if (!string.IsNullOrEmpty(profession))
        {
            query = query.Where(r => r.Profession != null && r.Profession.Contains(profession));
        }
        if (availableFrom.HasValue)
        {
            query = query.Where(r => r.AvailableFrom >= availableFrom.Value);
        }
        if (stayDuration.HasValue)
        {
            query = query.Where(r => (r.MinimumStayMonths == null || r.MinimumStayMonths <= stayDuration.Value) &&
                                     (r.MaximumStayMonths == null || r.MaximumStayMonths >= stayDuration.Value));
        }
        if (apartmentId.HasValue)
        {
            query = query.Where(r => r.LookingForApartmentId == apartmentId.Value);
        }
        if (gender.HasValue)
        {
            query = query.Where(r => r.Gender == gender.Value);
        }
        if (workSchedule.HasValue)
        {
            query = query.Where(r => r.WorkSchedule == workSchedule.Value);
        }
        var totalCount = await query.CountAsync();
        var now = DateTime.UtcNow;
        var roommates = await query
            // Boosted profiles bubble to the top while BoostedUntil is in the future
            .OrderByDescending(r => r.BoostedUntil != null && r.BoostedUntil > now)
            .ThenByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var userIds = roommates.Select(r => r.UserId).Distinct().ToList();
        var users = await _usersContext.Users
            .Where(u => userIds.Contains(u.UserId))
            .AsNoTracking()
            .ToListAsync();
        var userDict = users.ToDictionary(u => u.UserId);
        var resultList = roommates.Select(r =>
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
                IsActive = r.IsActive,
                Gender = r.Gender,
                Languages = r.Languages,
                WorkSchedule = r.WorkSchedule,
                MusicFriendly = r.MusicFriendly
            };
        }).ToList();

        var result = new PagedResult<RoommateDto>
        {
            Items = resultList,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(2))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

        _cache.Set(cacheKey, result, cacheEntryOptions);

        return result;
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
            IsActive = roommate.IsActive,
            Gender = roommate.Gender,
            Languages = roommate.Languages,
            WorkSchedule = roommate.WorkSchedule,
            MusicFriendly = roommate.MusicFriendly
        };
    }
    public async Task<RoommateDto?> GetRoommateByUserIdAsync(int userId)
    {
        // Fetch roommate + user in 2 queries instead of 3
        // (calling GetRoommateByIdAsync would re-fetch the roommate we already have)
        var roommate = await _context.Roommates
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive);
        if (roommate == null) return null;

        var user = await _usersContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == roommate.UserId);
        if (user == null) return null;

        return new RoommateDto
        {
            RoommateId           = roommate.RoommateId,
            UserId               = roommate.UserId,
            FirstName            = user.FirstName,
            LastName             = user.LastName,
            ProfilePicture       = user.ProfilePicture,
            DateOfBirth          = user.DateOfBirth,
            PhoneNumber          = user.PhoneNumber,
            Bio                  = roommate.Bio,
            Hobbies              = roommate.Hobbies,
            Profession           = roommate.Profession,
            SmokingAllowed       = roommate.SmokingAllowed,
            PetFriendly          = roommate.PetFriendly,
            Lifestyle            = roommate.Lifestyle,
            Cleanliness          = roommate.Cleanliness,
            GuestsAllowed        = roommate.GuestsAllowed,
            BudgetMin            = roommate.BudgetMin,
            BudgetMax            = roommate.BudgetMax,
            BudgetIncludes       = roommate.BudgetIncludes,
            AvailableFrom        = roommate.AvailableFrom,
            AvailableUntil       = roommate.AvailableUntil,
            MinimumStayMonths    = roommate.MinimumStayMonths,
            MaximumStayMonths    = roommate.MaximumStayMonths,
            LookingForRoomType   = roommate.LookingForRoomType,
            LookingForApartmentType = roommate.LookingForApartmentType,
            PreferredLocation    = roommate.PreferredLocation,
            IsActive             = roommate.IsActive,
            Gender               = roommate.Gender,
            Languages            = roommate.Languages,
            WorkSchedule         = roommate.WorkSchedule,
            MusicFriendly        = roommate.MusicFriendly
        };
    }
    public async Task<RoommateDto> CreateRoommateAsync(int userId, RoommateInputDto input)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var existingRoommate = await _context.Roommates
            .FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive);
        var user = await _usersContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) throw new NotFoundException("User", userId);
        Roommate roommate = null!;
        await _context.RunInTransactionAsync(async () =>
        {
            if (existingRoommate != null)
            {
                roommate = existingRoommate;
                roommate.Bio = HtmlSanitizationHelper.SanitizePlainText(input.Bio);
                roommate.Hobbies = HtmlSanitizationHelper.SanitizePlainText(input.Hobbies);
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
                roommate.Gender = input.Gender;
                roommate.Languages = HtmlSanitizationHelper.SanitizePlainText(input.Languages);
                roommate.WorkSchedule = input.WorkSchedule;
                roommate.MusicFriendly = input.MusicFriendly;
                roommate.IsActive = true;
                roommate.ModifiedByGuid = Guid.TryParse(currentUserGuid, out var rmGuid) ? rmGuid : null;
                roommate.ModifiedDate = DateTime.UtcNow;
                _context.Roommates.Update(roommate);
            }
            else
            {
                roommate = new Roommate
                {
                    UserId = userId,
                    Bio = HtmlSanitizationHelper.SanitizePlainText(input.Bio),
                    Hobbies = HtmlSanitizationHelper.SanitizePlainText(input.Hobbies),
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
                    Gender = input.Gender,
                    Languages = HtmlSanitizationHelper.SanitizePlainText(input.Languages),
                    WorkSchedule = input.WorkSchedule,
                    MusicFriendly = input.MusicFriendly,
                    IsActive = true,
                    CreatedByGuid = Guid.TryParse(currentUserGuid, out var cGuid) ? cGuid : null,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedByGuid = Guid.TryParse(currentUserGuid, out var mGuid) ? mGuid : null,
                    ModifiedDate = DateTime.UtcNow
                };
                _context.Roommates.Add(roommate);
            }
            await _context.SaveEntitiesAsync();
                    });
        InvalidateListCache();
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
            IsActive = roommate.IsActive,
            Gender = roommate.Gender,
            Languages = roommate.Languages,
            WorkSchedule = roommate.WorkSchedule,
            MusicFriendly = roommate.MusicFriendly
        };
    }
    public async Task<RoommateDto> UpdateRoommateAsync(int id, int userId, RoommateInputDto input)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var roommate = await _context.Roommates
            .FirstOrDefaultAsync(r => r.RoommateId == id);
        if (roommate == null)
            throw new NotFoundException("Roommate", id);
        if (roommate.UserId != userId)
            throw new ForbiddenException("You don't have permission to update this roommate profile.");
        roommate.Bio = HtmlSanitizationHelper.SanitizePlainText(input.Bio);
        roommate.Hobbies = HtmlSanitizationHelper.SanitizePlainText(input.Hobbies);
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
        roommate.Gender = input.Gender;
        roommate.Languages = HtmlSanitizationHelper.SanitizePlainText(input.Languages);
        roommate.WorkSchedule = input.WorkSchedule;
        roommate.MusicFriendly = input.MusicFriendly;
        roommate.ModifiedByGuid = Guid.TryParse(currentUserGuid, out var rmGuid) ? rmGuid : null;
        roommate.ModifiedDate = DateTime.UtcNow;
        await _context.RunInTransactionAsync(async () =>
        {
            await _context.SaveEntitiesAsync();
                    });
        InvalidateListCache();

        // Roommate is already up-to-date in memory — only load the user to build the DTO,
        // avoiding an unnecessary re-fetch of the same roommate entity.
        var user = await _usersContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == roommate.UserId)
            ?? throw new InvalidOperationException("Failed to update roommate");

        return new RoommateDto
        {
            RoommateId           = roommate.RoommateId,
            UserId               = roommate.UserId,
            FirstName            = user.FirstName,
            LastName             = user.LastName,
            ProfilePicture       = user.ProfilePicture,
            DateOfBirth          = user.DateOfBirth,
            PhoneNumber          = user.PhoneNumber,
            Bio                  = roommate.Bio,
            Hobbies              = roommate.Hobbies,
            Profession           = roommate.Profession,
            SmokingAllowed       = roommate.SmokingAllowed,
            PetFriendly          = roommate.PetFriendly,
            Lifestyle            = roommate.Lifestyle,
            Cleanliness          = roommate.Cleanliness,
            GuestsAllowed        = roommate.GuestsAllowed,
            BudgetMin            = roommate.BudgetMin,
            BudgetMax            = roommate.BudgetMax,
            BudgetIncludes       = roommate.BudgetIncludes,
            AvailableFrom        = roommate.AvailableFrom,
            AvailableUntil       = roommate.AvailableUntil,
            MinimumStayMonths    = roommate.MinimumStayMonths,
            MaximumStayMonths    = roommate.MaximumStayMonths,
            LookingForRoomType   = roommate.LookingForRoomType,
            LookingForApartmentType = roommate.LookingForApartmentType,
            PreferredLocation    = roommate.PreferredLocation,
            IsActive             = roommate.IsActive,
            Gender               = roommate.Gender,
            Languages            = roommate.Languages,
            WorkSchedule         = roommate.WorkSchedule,
            MusicFriendly        = roommate.MusicFriendly
        };
    }
    public async Task<bool> DeleteRoommateAsync(int id, int userId)
    {
        var roommate = await _context.Roommates
            .FirstOrDefaultAsync(r => r.RoommateId == id && r.UserId == userId);
        if (roommate == null) return false;
        await _context.RunInTransactionAsync(async () =>
        {
            roommate.IsActive = false;
            await _context.SaveEntitiesAsync();
                    });
        InvalidateListCache();
        return true;
    }
    public async Task<bool> DeleteRoommateByUserIdAsync(int userId)
    {
        var roommate = await _context.Roommates
            .FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive);
        if (roommate == null) return false;
        await _context.RunInTransactionAsync(async () =>
        {
            roommate.IsActive = false;
            await _context.SaveEntitiesAsync();
                    });
        InvalidateListCache();
        return true;
    }
}
