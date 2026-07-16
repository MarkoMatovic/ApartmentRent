using Lander.src.Infrastructure.FileStorage;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Models;

namespace Lander.src.Modules.Listings.Helpers;

public static class ApartmentMapper
{
    /// <summary>
    /// Maps an Apartment entity to ApartmentDto.
    /// Pass reviewStats when available; imageLimit = 0 means no limit.
    /// Pass <paramref name="urlBuilder"/> to resolve blob keys to public URLs (falls back to legacy ImageUrl).
    /// </summary>
    public static ApartmentDto ToDto(
        this Apartment a,
        decimal? averageRating = null,
        int reviewCount = 0,
        int imageLimit = 5,
        IImageUrlBuilder? urlBuilder = null)
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
                .Select(img => img.ToImageDto(urlBuilder))
                .ToList()
        };
    }

    /// <summary>
    /// Maps an <see cref="ApartmentImage"/> to its DTO, resolving the full-size and thumbnail
    /// URLs from stored blob keys via <paramref name="urlBuilder"/>. Falls back to the legacy
    /// absolute <see cref="ApartmentImage.ImageUrl"/> for rows created before the blob-path migration.
    /// </summary>
    public static ApartmentImageDto ToImageDto(this ApartmentImage img, IImageUrlBuilder? urlBuilder)
    {
        // Full-size: prefer the blob key; fall back to the legacy absolute URL.
        var imageUrl = urlBuilder?.BuildUrl(FileStorageContainers.ApartmentImages, img.BlobPath) ?? img.ImageUrl;

        // Thumbnail: only when a thumbnail key exists. Null => clients fall back to the full image.
        var thumbnailUrl = img.ThumbnailPath is not null
            ? urlBuilder?.BuildUrl(FileStorageContainers.ApartmentImages, img.ThumbnailPath)
            : null;

        return new ApartmentImageDto
        {
            ImageId = img.ImageId,
            ApartmentId = img.ApartmentId,
            ImageUrl = imageUrl,
            ThumbnailUrl = thumbnailUrl,
            IsPrimary = img.IsPrimary
        };
    }
}
