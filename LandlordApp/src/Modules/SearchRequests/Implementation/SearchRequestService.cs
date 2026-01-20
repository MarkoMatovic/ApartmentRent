using System.Security.Claims;
using Lander;
using Lander.src.Common;
using Lander.src.Modules.SearchRequests.Dtos.Dto;
using Lander.src.Modules.SearchRequests.Dtos.InputDto;
using Lander.src.Modules.SearchRequests.Interfaces;
using Lander.src.Modules.SearchRequests.Models;
using Microsoft.EntityFrameworkCore;
namespace Lander.src.Modules.SearchRequests.Implementation;
public class SearchRequestService : ISearchRequestService
{
    private readonly SearchRequestsContext _context;
    private readonly UsersContext _usersContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public SearchRequestService(SearchRequestsContext context, UsersContext usersContext, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _usersContext = usersContext;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<IEnumerable<SearchRequestDto>> GetAllSearchRequestsAsync(
        SearchRequestType? requestType = null,
        string? city = null,
        decimal? minBudget = null,
        decimal? maxBudget = null)
    {
        var query = _context.SearchRequests
            .Where(sr => sr.IsActive)
            .AsNoTracking();
        if (requestType.HasValue)
        {
            query = query.Where(sr => sr.RequestType == requestType.Value);
        }
        if (!string.IsNullOrEmpty(city))
        {
            query = query.Where(sr => sr.City != null && sr.City.Contains(city));
        }
        if (minBudget.HasValue)
        {
            query = query.Where(sr => sr.BudgetMax == null || sr.BudgetMax >= minBudget.Value);
        }
        if (maxBudget.HasValue)
        {
            query = query.Where(sr => sr.BudgetMin == null || sr.BudgetMin <= maxBudget.Value);
        }
        var result = await (from sr in query
                           join u in _usersContext.Users on sr.UserId equals u.UserId
                           orderby sr.CreatedDate descending
                           select new SearchRequestDto
                           {
                               SearchRequestId = sr.SearchRequestId,
                               UserId = sr.UserId,
                               FirstName = u.FirstName,
                               LastName = u.LastName,
                               ProfilePicture = u.ProfilePicture,
                               RequestType = sr.RequestType,
                               Title = sr.Title,
                               Description = sr.Description,
                               City = sr.City,
                               PostalCode = sr.PostalCode,
                               PreferredLocation = sr.PreferredLocation,
                               BudgetMin = sr.BudgetMin,
                               BudgetMax = sr.BudgetMax,
                               NumberOfRooms = sr.NumberOfRooms,
                               SizeSquareMeters = sr.SizeSquareMeters,
                               IsFurnished = sr.IsFurnished,
                               HasParking = sr.HasParking,
                               HasBalcony = sr.HasBalcony,
                               PetFriendly = sr.PetFriendly,
                               SmokingAllowed = sr.SmokingAllowed,
                               AvailableFrom = sr.AvailableFrom,
                               AvailableUntil = sr.AvailableUntil,
                               LookingForSmokingAllowed = sr.LookingForSmokingAllowed,
                               LookingForPetFriendly = sr.LookingForPetFriendly,
                               PreferredLifestyle = sr.PreferredLifestyle,
                               IsActive = sr.IsActive,
                               CreatedDate = sr.CreatedDate
                           }).ToListAsync();
        return result;
    }
    public async Task<PagedResult<SearchRequestDto>> GetAllSearchRequestsAsync(
        SearchRequestType? requestType,
        string? city,
        decimal? minBudget,
        decimal? maxBudget,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.SearchRequests
            .Where(sr => sr.IsActive)
            .AsNoTracking();
        if (requestType.HasValue)
        {
            query = query.Where(sr => sr.RequestType == requestType.Value);
        }
        if (!string.IsNullOrEmpty(city))
        {
            query = query.Where(sr => sr.City != null && sr.City.Contains(city));
        }
        if (minBudget.HasValue)
        {
            query = query.Where(sr => sr.BudgetMax == null || sr.BudgetMax >= minBudget.Value);
        }
        if (maxBudget.HasValue)
        {
            query = query.Where(sr => sr.BudgetMin == null || sr.BudgetMin <= maxBudget.Value);
        }
        var totalCount = await query.CountAsync();
        var result = await (from sr in query
                           join u in _usersContext.Users on sr.UserId equals u.UserId
                           orderby sr.CreatedDate descending
                           select new SearchRequestDto
                           {
                               SearchRequestId = sr.SearchRequestId,
                               UserId = sr.UserId,
                               FirstName = u.FirstName,
                               LastName = u.LastName,
                               ProfilePicture = u.ProfilePicture,
                               RequestType = sr.RequestType,
                               Title = sr.Title,
                               Description = sr.Description,
                               City = sr.City,
                               PostalCode = sr.PostalCode,
                               PreferredLocation = sr.PreferredLocation,
                               BudgetMin = sr.BudgetMin,
                               BudgetMax = sr.BudgetMax,
                               NumberOfRooms = sr.NumberOfRooms,
                               SizeSquareMeters = sr.SizeSquareMeters,
                               IsFurnished = sr.IsFurnished,
                               HasParking = sr.HasParking,
                               HasBalcony = sr.HasBalcony,
                               PetFriendly = sr.PetFriendly,
                               SmokingAllowed = sr.SmokingAllowed,
                               AvailableFrom = sr.AvailableFrom,
                               AvailableUntil = sr.AvailableUntil,
                               LookingForSmokingAllowed = sr.LookingForSmokingAllowed,
                               LookingForPetFriendly = sr.LookingForPetFriendly,
                               PreferredLifestyle = sr.PreferredLifestyle,
                               IsActive = sr.IsActive,
                               CreatedDate = sr.CreatedDate
                           })
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync();
        return new PagedResult<SearchRequestDto>
        {
            Items = result,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
    public async Task<SearchRequestDto?> GetSearchRequestByIdAsync(int id)
    {
        var searchRequest = await _context.SearchRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(sr => sr.SearchRequestId == id && sr.IsActive);
        if (searchRequest == null) return null;
        var user = await _usersContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == searchRequest.UserId);
        if (user == null) return null;
        return new SearchRequestDto
        {
            SearchRequestId = searchRequest.SearchRequestId,
            UserId = searchRequest.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePicture = user.ProfilePicture,
            RequestType = searchRequest.RequestType,
            Title = searchRequest.Title,
            Description = searchRequest.Description,
            City = searchRequest.City,
            PostalCode = searchRequest.PostalCode,
            PreferredLocation = searchRequest.PreferredLocation,
            BudgetMin = searchRequest.BudgetMin,
            BudgetMax = searchRequest.BudgetMax,
            NumberOfRooms = searchRequest.NumberOfRooms,
            SizeSquareMeters = searchRequest.SizeSquareMeters,
            IsFurnished = searchRequest.IsFurnished,
            HasParking = searchRequest.HasParking,
            HasBalcony = searchRequest.HasBalcony,
            PetFriendly = searchRequest.PetFriendly,
            SmokingAllowed = searchRequest.SmokingAllowed,
            AvailableFrom = searchRequest.AvailableFrom,
            AvailableUntil = searchRequest.AvailableUntil,
            LookingForSmokingAllowed = searchRequest.LookingForSmokingAllowed,
            LookingForPetFriendly = searchRequest.LookingForPetFriendly,
            PreferredLifestyle = searchRequest.PreferredLifestyle,
            IsActive = searchRequest.IsActive,
            CreatedDate = searchRequest.CreatedDate
        };
    }
    public async Task<IEnumerable<SearchRequestDto>> GetSearchRequestsByUserIdAsync(int userId)
    {
        var result = await (from sr in _context.SearchRequests
                           where sr.UserId == userId && sr.IsActive
                           join u in _usersContext.Users on sr.UserId equals u.UserId
                           orderby sr.CreatedDate descending
                           select new SearchRequestDto
                           {
                               SearchRequestId = sr.SearchRequestId,
                               UserId = sr.UserId,
                               FirstName = u.FirstName,
                               LastName = u.LastName,
                               ProfilePicture = u.ProfilePicture,
                               RequestType = sr.RequestType,
                               Title = sr.Title,
                               Description = sr.Description,
                               City = sr.City,
                               PostalCode = sr.PostalCode,
                               PreferredLocation = sr.PreferredLocation,
                               BudgetMin = sr.BudgetMin,
                               BudgetMax = sr.BudgetMax,
                               NumberOfRooms = sr.NumberOfRooms,
                               SizeSquareMeters = sr.SizeSquareMeters,
                               IsFurnished = sr.IsFurnished,
                               HasParking = sr.HasParking,
                               HasBalcony = sr.HasBalcony,
                               PetFriendly = sr.PetFriendly,
                               SmokingAllowed = sr.SmokingAllowed,
                               AvailableFrom = sr.AvailableFrom,
                               AvailableUntil = sr.AvailableUntil,
                               LookingForSmokingAllowed = sr.LookingForSmokingAllowed,
                               LookingForPetFriendly = sr.LookingForPetFriendly,
                               PreferredLifestyle = sr.PreferredLifestyle,
                               IsActive = sr.IsActive,
                               CreatedDate = sr.CreatedDate
                           }).AsNoTracking().ToListAsync();
        return result;
    }
    public async Task<SearchRequestDto> CreateSearchRequestAsync(int userId, SearchRequestInputDto input)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var searchRequest = new SearchRequest
        {
            UserId = userId,
            RequestType = input.RequestType,
            Title = input.Title,
            Description = input.Description,
            City = input.City,
            PostalCode = input.PostalCode,
            PreferredLocation = input.PreferredLocation,
            BudgetMin = input.BudgetMin,
            BudgetMax = input.BudgetMax,
            NumberOfRooms = input.NumberOfRooms,
            SizeSquareMeters = input.SizeSquareMeters,
            IsFurnished = input.IsFurnished,
            HasParking = input.HasParking,
            HasBalcony = input.HasBalcony,
            PetFriendly = input.PetFriendly,
            SmokingAllowed = input.SmokingAllowed,
            AvailableFrom = input.AvailableFrom,
            AvailableUntil = input.AvailableUntil,
            LookingForSmokingAllowed = input.LookingForSmokingAllowed,
            LookingForPetFriendly = input.LookingForPetFriendly,
            PreferredLifestyle = input.PreferredLifestyle,
            IsActive = true,
            CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            ModifiedDate = DateTime.UtcNow
        };
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.SearchRequests.Add(searchRequest);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        return await GetSearchRequestByIdAsync(searchRequest.SearchRequestId) ?? throw new Exception("Failed to create search request");
    }
    public async Task<SearchRequestDto> UpdateSearchRequestAsync(int id, int userId, SearchRequestInputDto input)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var searchRequest = await _context.SearchRequests
            .FirstOrDefaultAsync(sr => sr.SearchRequestId == id && sr.UserId == userId);
        if (searchRequest == null)
            throw new Exception("Search request not found or you don't have permission to update it");
        searchRequest.RequestType = input.RequestType;
        searchRequest.Title = input.Title;
        searchRequest.Description = input.Description;
        searchRequest.City = input.City;
        searchRequest.PostalCode = input.PostalCode;
        searchRequest.PreferredLocation = input.PreferredLocation;
        searchRequest.BudgetMin = input.BudgetMin;
        searchRequest.BudgetMax = input.BudgetMax;
        searchRequest.NumberOfRooms = input.NumberOfRooms;
        searchRequest.SizeSquareMeters = input.SizeSquareMeters;
        searchRequest.IsFurnished = input.IsFurnished;
        searchRequest.HasParking = input.HasParking;
        searchRequest.HasBalcony = input.HasBalcony;
        searchRequest.PetFriendly = input.PetFriendly;
        searchRequest.SmokingAllowed = input.SmokingAllowed;
        searchRequest.AvailableFrom = input.AvailableFrom;
        searchRequest.AvailableUntil = input.AvailableUntil;
        searchRequest.LookingForSmokingAllowed = input.LookingForSmokingAllowed;
        searchRequest.LookingForPetFriendly = input.LookingForPetFriendly;
        searchRequest.PreferredLifestyle = input.PreferredLifestyle;
        searchRequest.ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null;
        searchRequest.ModifiedDate = DateTime.UtcNow;
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
        return await GetSearchRequestByIdAsync(searchRequest.SearchRequestId) ?? throw new Exception("Failed to update search request");
    }
    public async Task<bool> DeleteSearchRequestAsync(int id, int userId)
    {
        var searchRequest = await _context.SearchRequests
            .FirstOrDefaultAsync(sr => sr.SearchRequestId == id && sr.UserId == userId);
        if (searchRequest == null) return false;
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            searchRequest.IsActive = false;
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
