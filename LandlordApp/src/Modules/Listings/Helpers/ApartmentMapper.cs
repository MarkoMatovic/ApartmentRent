using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Models;

namespace Lander.src.Modules.Listings.Helpers;

public static class ApartmentMapper
{
    /// <summary>
    /// Maps an Apartment entity to ApartmentDto.
    /// Pass reviewStats when available; imageLimit = 0 means no limit.
    /// </summary>
    public static ApartmentDto ToDto(
        this Apartment a,
        decimal? averageRating = null,
        int reviewCount = 0,
        int imageLimit = 5)
    {
        IEnumerable<ApartmentImage> images = (a.ApartmentImages ?? Enumerable.Empty<ApartmentImage>())
            .Where(img => !img.IsDeleted)
            .OrderBy(img => img.DisplayOrder);

        if (imageLimit > 0)
            images = images.Take(imageLimit);

        return new ApartmentDto
        {
            ApartmentId = a.ApartmentId,
            Title = a.Title,
            Rent = a.Rent,
            Price = a.Price,
            Address = a.Address,
            City = a.City ?? string.Empty,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
            SizeSquareMeters = a.SizeSquareMeters,
            ApartmentType = a.ApartmentType,
            ListingType = a.ListingType,
            IsFurnished = ApartmentFeaturesHelper.Deserialize(a.Features).IsFurnished,
            IsImmediatelyAvailable = a.IsImmediatelyAvailable,
            IsLookingForRoommate = a.IsLookingForRoommate,
            IsFeatured = a.IsFeatured,
            FeaturedUntil = a.FeaturedUntil,
            AverageRating = averageRating,
            ReviewCount = reviewCount,
            ApartmentImages = images
                .Select(img => new ApartmentImageDto
                {
                    ImageId = img.ImageId,
                    ApartmentId = img.ApartmentId,
                    ImageUrl = img.ImageUrl,
                    IsPrimary = img.IsPrimary
                }).ToList()
        };
    }
}
