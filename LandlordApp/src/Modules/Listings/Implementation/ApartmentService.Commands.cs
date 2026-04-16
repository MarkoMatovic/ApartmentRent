using System.Security.Claims;
using Lander.src.Common;
using Lander.src.Common.Exceptions;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Helpers;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.MachineLearning.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Listings.Implementation;

public partial class ApartmentService
{
    public async Task<bool> ActivateApartmentAsync(int apartmentId)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var apartment = await _context.Apartments
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId);
        if (apartment == null) return false;

        // Ownership check
        if (currentUserGuid != null && Guid.TryParse(currentUserGuid, out Guid parsedGuid))
        {
            var user = await _usersContext.Users.FirstOrDefaultAsync(u => u.UserGuid == parsedGuid);
            if (user == null || apartment.LandlordId != user.UserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to activate this apartment.");
            }
        }
        else
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        apartment.IsDeleted = false;
        apartment.IsActive = true;
        apartment.ModifiedByGuid = Guid.TryParse(currentUserGuid, out Guid parsedGuidMod) ? parsedGuidMod : null;
        apartment.ModifiedDate = DateTime.UtcNow;
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
        _cacheVersion.Invalidate();
        return true;
    }

    public async Task<ApartmentDto> CreateApartmentAsync(ApartmentInputDto apartmentInputDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int? landlordId = null;
        if (currentUserGuid != null && Guid.TryParse(currentUserGuid, out Guid parsedGuid))
        {
            var user = await _usersContext.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.UserGuid == parsedGuid);

            if (user != null)
            {
                landlordId = user.UserId;

                // Auto-upgrade: If user is Tenant, upgrade to TenantLandlord when creating first apartment
                if (user.UserRole?.RoleName == "Tenant")
                {
                    var tenantLandlordRole = await _usersContext.Roles
                        .FirstOrDefaultAsync(r => r.RoleName == "TenantLandlord");

                    if (tenantLandlordRole != null)
                    {
                        user.UserRoleId = tenantLandlordRole.RoleId;
                        await _usersContext.SaveEntitiesAsync();
                    }
                }
            }
        }
        var apartment = new Apartment
        {
            LandlordId = landlordId,
            Title = HtmlSanitizationHelper.SanitizePlainText(apartmentInputDto.Title)!,
            Description = HtmlSanitizationHelper.SanitizeRichText(apartmentInputDto.Description),
            Rent = apartmentInputDto.Rent,
            Price = apartmentInputDto.Price,
            Address = apartmentInputDto.Address,
            City = apartmentInputDto.City,
            PostalCode = apartmentInputDto.PostalCode,
            AvailableFrom = apartmentInputDto.AvailableFrom,
            AvailableUntil = apartmentInputDto.AvailableUntil,
            NumberOfRooms = apartmentInputDto.NumberOfRooms,
            RentIncludeUtilities = apartmentInputDto.RentIncludeUtilities ?? false,
            Latitude = apartmentInputDto.Latitude,
            Longitude = apartmentInputDto.Longitude,
            SizeSquareMeters = apartmentInputDto.SizeSquareMeters,
            ApartmentType = apartmentInputDto.ApartmentType ?? ApartmentType.Studio,
            ListingType = apartmentInputDto.ListingType ?? ListingType.Rent,
            IsFurnished = apartmentInputDto.IsFurnished ?? false,
            HasBalcony = apartmentInputDto.HasBalcony ?? false,
            HasElevator = apartmentInputDto.HasElevator ?? false,
            HasParking = apartmentInputDto.HasParking ?? false,
            HasInternet = apartmentInputDto.HasInternet ?? false,
            HasAirCondition = apartmentInputDto.HasAirCondition ?? false,
            IsPetFriendly = apartmentInputDto.IsPetFriendly ?? false,
            IsSmokingAllowed = apartmentInputDto.IsSmokingAllowed ?? false,
            DepositAmount = apartmentInputDto.DepositAmount,
            MinimumStayMonths = apartmentInputDto.MinimumStayMonths,
            MaximumStayMonths = apartmentInputDto.MaximumStayMonths,
            IsImmediatelyAvailable = apartmentInputDto.IsImmediatelyAvailable ?? false,
            IsLookingForRoommate = apartmentInputDto.IsLookingForRoommate ?? false,
            ContactPhone = apartmentInputDto.ContactPhone,
            IsActive = true,
            CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            ModifiedDate = DateTime.UtcNow
        };
        // Serijalizacija features u JSON kolonu (NotMapped properties se ne čuvaju direktno)
        apartment.Features = ApartmentFeaturesHelper.Serialize(
            apartment.IsFurnished, apartment.HasBalcony, apartment.HasElevator,
            apartment.HasParking, apartment.HasInternet, apartment.HasAirCondition,
            apartment.IsPetFriendly, apartment.IsSmokingAllowed);
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.Apartments.Add(apartment);
            await _context.SaveEntitiesAsync(); // Save apartment first to get ApartmentId
            if (apartmentInputDto.ImageUrls != null && apartmentInputDto.ImageUrls.Any())
            {
                var apartmentImages = apartmentInputDto.ImageUrls.Select((url, index) => new ApartmentImage
                {
                    ApartmentId = apartment.ApartmentId,
                    ImageUrl = url,
                    DisplayOrder = index,
                    IsPrimary = index == 0,
                    IsDeleted = false,
                    CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
                    ModifiedDate = DateTime.UtcNow
                }).ToList();
                _context.ApartmentImages.AddRange(apartmentImages);
                await _context.SaveEntitiesAsync(); // Save images
            }
            await _context.CommitTransactionAsync(transaction);
        }
        catch (Exception ex)
        {
            _context.RollBackTransaction();
            throw;
        }
        _cacheVersion.Invalidate();

        // Broadcast new listing notification
        if (apartment.IsActive)
        {
             await _notificationHubContext.Clients.All.SendAsync("ReceiveNotification",
                "New Apartment Listed!",
                $"A new apartment '{apartment.Title}' is now available in {apartment.City}.",
                "success");
        }

        return new ApartmentDto
        {
            ApartmentId = apartment.ApartmentId,
            Title = apartment.Title,
            Rent = apartment.Rent,
            Price = apartment.Price,
            Address = apartment.Address,
            City = apartment.City,
            Latitude = apartment.Latitude,
            Longitude = apartment.Longitude,
            SizeSquareMeters = apartment.SizeSquareMeters,
            ApartmentType = apartment.ApartmentType,
            ListingType = apartment.ListingType,
            IsFurnished = apartment.IsFurnished,
            IsImmediatelyAvailable = apartment.IsImmediatelyAvailable
        };
    }

    public async Task<bool> DeleteApartmentAsync(int apartmentId)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var apartment = await _context.Apartments
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId && !a.IsDeleted);
        if (apartment == null) return false;

        // Ownership check
        if (currentUserGuid != null && Guid.TryParse(currentUserGuid, out Guid parsedGuid))
        {
            var user = await _usersContext.Users.FirstOrDefaultAsync(u => u.UserGuid == parsedGuid);
            if (user == null || apartment.LandlordId != user.UserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this apartment.");
            }
        }
        else
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        // Notify saved search users BEFORE deactivating
        try
        {
            // Find all active saved searches that reference this apartment ID in their filters
            var allSavedSearches = await _savedSearchesContext.SavedSearches
                .Where(ss => ss.IsActive && ss.EmailNotificationsEnabled)
                .ToListAsync();

            var affectedUserIds = allSavedSearches
                .Where(ss => ss.FiltersJson != null &&
                             ss.FiltersJson.Contains($"\"apartmentId\":{apartmentId}",
                                 StringComparison.OrdinalIgnoreCase))
                .Select(ss => ss.UserId)
                .Distinct()
                .ToList();

            if (affectedUserIds.Any())
            {
                var usersToNotify = await _usersContext.Users
                    .Where(u => affectedUserIds.Contains(u.UserId) && u.IsActive)
                    .ToListAsync();

                foreach (var user in usersToNotify)
                {
                    _ = _emailService.SendListingUnavailableEmailAsync(
                        user.Email,
                        user.FirstName,
                        apartment.Title,
                        "This listing has been removed by the landlord.");
                }

                _logger.LogInformation("Sent listing unavailable notifications to {Count} user(s) for apartment {ApartmentId}",
                    usersToNotify.Count, apartmentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying saved search users for deleted apartment {ApartmentId}", apartmentId);
            // Do not block deletion on notification failure
        }

        apartment.IsDeleted = true;
        apartment.IsActive = false;
        apartment.ModifiedByGuid = Guid.TryParse(currentUserGuid, out Guid tempGuid)? tempGuid : null;
        apartment.ModifiedDate = DateTime.UtcNow;
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
        _cacheVersion.Invalidate();
        _auditLog.Log("DeleteApartment", "Apartment", apartmentId, currentUserGuid);
        return true;
    }

    public async Task DeleteApartmentsByLandlordIdAsync(int landlordId)
    {
        var apartments = await _context.Apartments
            .Where(a => a.LandlordId == landlordId && !a.IsDeleted)
            .ToListAsync();

        if (!apartments.Any()) return;

        foreach (var apartment in apartments)
        {
            apartment.IsDeleted = true;
            apartment.IsActive = false;
            apartment.ModifiedDate = DateTime.UtcNow;
        }

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
    }

    public async Task<ApartmentDto> UpdateApartmentAsync(int apartmentId, ApartmentUpdateInputDto updateDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var apartment = await _context.Apartments
            .Include(a => a.ApartmentImages)
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId && !a.IsDeleted);
        if (apartment == null)
        {
            throw new NotFoundException("Apartment not found or has been deleted");
        }

        // Ownership check
        if (currentUserGuid != null && Guid.TryParse(currentUserGuid, out Guid parsedGuid))
        {
            var user = await _usersContext.Users.FirstOrDefaultAsync(u => u.UserGuid == parsedGuid);
            if (user == null || apartment.LandlordId != user.UserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to update this apartment.");
            }
        }
        else
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }
        if (updateDto.Title != null) apartment.Title = HtmlSanitizationHelper.SanitizePlainText(updateDto.Title)!;
        if (updateDto.Description != null) apartment.Description = HtmlSanitizationHelper.SanitizeRichText(updateDto.Description);
        if (updateDto.Rent.HasValue) apartment.Rent = updateDto.Rent.Value;
        if (updateDto.Price.HasValue) apartment.Price = updateDto.Price.Value;
        if (updateDto.Address != null) apartment.Address = updateDto.Address;
        if (updateDto.City != null) apartment.City = updateDto.City;
        if (updateDto.PostalCode != null) apartment.PostalCode = updateDto.PostalCode;
        if (updateDto.AvailableFrom.HasValue) apartment.AvailableFrom = updateDto.AvailableFrom.Value;
        if (updateDto.AvailableUntil.HasValue) apartment.AvailableUntil = updateDto.AvailableUntil.Value;
        if (updateDto.NumberOfRooms.HasValue) apartment.NumberOfRooms = updateDto.NumberOfRooms.Value;
        if (updateDto.RentIncludeUtilities.HasValue) apartment.RentIncludeUtilities = updateDto.RentIncludeUtilities.Value;
        if (updateDto.Latitude.HasValue) apartment.Latitude = updateDto.Latitude.Value;
        if (updateDto.Longitude.HasValue) apartment.Longitude = updateDto.Longitude.Value;
        if (updateDto.SizeSquareMeters.HasValue) apartment.SizeSquareMeters = updateDto.SizeSquareMeters.Value;
        if (updateDto.ApartmentType.HasValue) apartment.ApartmentType = updateDto.ApartmentType.Value;
        if (updateDto.ListingType.HasValue) apartment.ListingType = updateDto.ListingType.Value;
        if (updateDto.IsFurnished.HasValue) apartment.IsFurnished = updateDto.IsFurnished.Value;
        if (updateDto.HasBalcony.HasValue) apartment.HasBalcony = updateDto.HasBalcony.Value;
        if (updateDto.HasElevator.HasValue) apartment.HasElevator = updateDto.HasElevator.Value;
        if (updateDto.HasParking.HasValue) apartment.HasParking = updateDto.HasParking.Value;
        if (updateDto.HasInternet.HasValue) apartment.HasInternet = updateDto.HasInternet.Value;
        if (updateDto.HasAirCondition.HasValue) apartment.HasAirCondition = updateDto.HasAirCondition.Value;
        if (updateDto.IsPetFriendly.HasValue) apartment.IsPetFriendly = updateDto.IsPetFriendly.Value;
        if (updateDto.IsSmokingAllowed.HasValue) apartment.IsSmokingAllowed = updateDto.IsSmokingAllowed.Value;
        // Re-serijalizacija features u JSON kolonu nakon update-a
        apartment.Features = ApartmentFeaturesHelper.Serialize(
            apartment.IsFurnished, apartment.HasBalcony, apartment.HasElevator,
            apartment.HasParking, apartment.HasInternet, apartment.HasAirCondition,
            apartment.IsPetFriendly, apartment.IsSmokingAllowed);
        if (updateDto.DepositAmount.HasValue) apartment.DepositAmount = updateDto.DepositAmount.Value;
        if (updateDto.MinimumStayMonths.HasValue) apartment.MinimumStayMonths = updateDto.MinimumStayMonths.Value;
        if (updateDto.MaximumStayMonths.HasValue) apartment.MaximumStayMonths = updateDto.MaximumStayMonths.Value;
        if (updateDto.IsImmediatelyAvailable.HasValue) apartment.IsImmediatelyAvailable = updateDto.IsImmediatelyAvailable.Value;
        if (updateDto.IsLookingForRoommate.HasValue) apartment.IsLookingForRoommate = updateDto.IsLookingForRoommate.Value;
        if (updateDto.ContactPhone != null) apartment.ContactPhone = updateDto.ContactPhone;
        apartment.ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null;
        apartment.ModifiedDate = DateTime.UtcNow;
        if (updateDto.ImageUrls != null && updateDto.ImageUrls.Any())
        {
            var existingImages = apartment.ApartmentImages.Where(img => !img.IsDeleted).ToList();
            foreach (var img in existingImages)
            {
                img.IsDeleted = true;
                img.ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null;
                img.ModifiedDate = DateTime.UtcNow;
            }
            var newImages = updateDto.ImageUrls.Select((url, index) => new ApartmentImage
            {
                ApartmentId = apartment.ApartmentId,
                ImageUrl = url,
                DisplayOrder = index,
                IsPrimary = index == 0,
                CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
                CreatedDate = DateTime.UtcNow,
                ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
                ModifiedDate = DateTime.UtcNow
            }).ToList();
            _context.ApartmentImages.AddRange(newImages);
        }
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
        _cacheVersion.Invalidate();
        return new ApartmentDto
        {
            ApartmentId = apartment.ApartmentId,
            Title = apartment.Title,
            Rent = apartment.Rent,
            Price = apartment.Price,
            Address = apartment.Address,
            City = apartment.City,
            Latitude = apartment.Latitude,
            Longitude = apartment.Longitude,
            SizeSquareMeters = apartment.SizeSquareMeters,
            ApartmentType = apartment.ApartmentType,
            ListingType = apartment.ListingType,
            IsFurnished = apartment.IsFurnished,
            IsImmediatelyAvailable = apartment.IsImmediatelyAvailable
        };
    }

    // .NET 10 Feature: Vector Search implementation
    public async Task<int> GenerateEmbeddingsForAllApartmentsAsync(SimpleEmbeddingService embeddingService)
    {
        var apartments = await _context.Apartments
            .IgnoreQueryFilters()
            .ToListAsync();

        int count = 0;
        foreach (var apartment in apartments)
        {
            if (!string.IsNullOrWhiteSpace(apartment.Description))
            {
                var embedding = embeddingService.GenerateEmbedding(apartment.Description);
                apartment.DescriptionEmbedding = System.Text.Json.JsonSerializer.Serialize(embedding);
                count++;
            }
        }

        await _context.SaveChangesAsync();
        return count;
    }
}
