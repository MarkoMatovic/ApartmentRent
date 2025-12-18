using System.Security.Claims;
using Lander;
using Lander.src.Modules.SavedSearches.Dtos.Dto;
using Lander.src.Modules.SavedSearches.Dtos.InputDto;
using Lander.src.Modules.SavedSearches.Interfaces;
using Lander.src.Modules.SavedSearches.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.SavedSearches.Implementation;

public class SavedSearchService : ISavedSearchService
{
    private readonly SavedSearchesContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SavedSearchService(SavedSearchesContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<SavedSearchDto>> GetSavedSearchesByUserIdAsync(int userId)
    {
        var savedSearches = await _context.SavedSearches
            .Where(ss => ss.UserId == userId && ss.IsActive)
            .OrderByDescending(ss => ss.CreatedDate)
            .ToListAsync();

        return savedSearches.Select(ss => new SavedSearchDto
        {
            SavedSearchId = ss.SavedSearchId,
            UserId = ss.UserId,
            Name = ss.Name,
            SearchType = ss.SearchType,
            FiltersJson = ss.FiltersJson,
            EmailNotificationsEnabled = ss.EmailNotificationsEnabled,
            LastNotificationSent = ss.LastNotificationSent,
            IsActive = ss.IsActive,
            CreatedDate = ss.CreatedDate
        });
    }

    public async Task<SavedSearchDto?> GetSavedSearchByIdAsync(int id)
    {
        var savedSearch = await _context.SavedSearches
            .FirstOrDefaultAsync(ss => ss.SavedSearchId == id && ss.IsActive);

        if (savedSearch == null) return null;

        return new SavedSearchDto
        {
            SavedSearchId = savedSearch.SavedSearchId,
            UserId = savedSearch.UserId,
            Name = savedSearch.Name,
            SearchType = savedSearch.SearchType,
            FiltersJson = savedSearch.FiltersJson,
            EmailNotificationsEnabled = savedSearch.EmailNotificationsEnabled,
            LastNotificationSent = savedSearch.LastNotificationSent,
            IsActive = savedSearch.IsActive,
            CreatedDate = savedSearch.CreatedDate
        };
    }

    public async Task<SavedSearchDto> CreateSavedSearchAsync(int userId, SavedSearchInputDto input)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

        var savedSearch = new SavedSearch
        {
            UserId = userId,
            Name = input.Name,
            SearchType = input.SearchType,
            FiltersJson = input.FiltersJson,
            EmailNotificationsEnabled = input.EmailNotificationsEnabled,
            IsActive = true,
            CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            ModifiedDate = DateTime.UtcNow
        };

        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.SavedSearches.Add(savedSearch);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }

        return await GetSavedSearchByIdAsync(savedSearch.SavedSearchId) ?? throw new Exception("Failed to create saved search");
    }

    public async Task<SavedSearchDto> UpdateSavedSearchAsync(int id, int userId, SavedSearchInputDto input)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

        var savedSearch = await _context.SavedSearches
            .FirstOrDefaultAsync(ss => ss.SavedSearchId == id && ss.UserId == userId);

        if (savedSearch == null)
            throw new Exception("Saved search not found or you don't have permission to update it");

        savedSearch.Name = input.Name;
        savedSearch.SearchType = input.SearchType;
        savedSearch.FiltersJson = input.FiltersJson;
        savedSearch.EmailNotificationsEnabled = input.EmailNotificationsEnabled;
        savedSearch.ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null;
        savedSearch.ModifiedDate = DateTime.UtcNow;

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

        return await GetSavedSearchByIdAsync(savedSearch.SavedSearchId) ?? throw new Exception("Failed to update saved search");
    }

    public async Task<bool> DeleteSavedSearchAsync(int id, int userId)
    {
        var savedSearch = await _context.SavedSearches
            .FirstOrDefaultAsync(ss => ss.SavedSearchId == id && ss.UserId == userId);

        if (savedSearch == null) return false;

        var transaction = await _context.BeginTransactionAsync();
        try
        {
            savedSearch.IsActive = false;
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

