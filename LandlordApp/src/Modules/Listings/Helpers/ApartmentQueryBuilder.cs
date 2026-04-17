using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Listings.Helpers;

public static class ApartmentQueryBuilder
{
    public static IQueryable<Apartment> ApplyFilters(
        this IQueryable<Apartment> query, ApartmentFilterDto filters)
    {
        if (filters.ListingType.HasValue)
            query = query.Where(a => a.ListingType == filters.ListingType.Value);

        if (!string.IsNullOrEmpty(filters.City))
            // Suffix wildcard → SQL LIKE 'value%' uses the City index (vs. Contains → '%value%')
            query = query.Where(a => EF.Functions.Like(a.City!, filters.City + "%"));

        if (filters.MinRent.HasValue)
            query = query.Where(a => a.Rent >= filters.MinRent.Value);

        if (filters.MaxRent.HasValue)
            query = query.Where(a => a.Rent <= filters.MaxRent.Value);

        if (filters.NumberOfRooms.HasValue)
            query = query.Where(a => a.NumberOfRooms == filters.NumberOfRooms.Value);

        if (filters.ApartmentType.HasValue)
            query = query.Where(a => a.ApartmentType == filters.ApartmentType.Value);

        if (filters.IsImmediatelyAvailable.HasValue)
            query = query.Where(a => a.IsImmediatelyAvailable == filters.IsImmediatelyAvailable.Value);

        if (filters.AvailableFrom.HasValue)
            query = query.Where(a => a.AvailableFrom >= filters.AvailableFrom.Value);

        return query;
    }

    public static IOrderedQueryable<Apartment> ApplySort(
        this IQueryable<Apartment> query, string? sortBy, string? sortOrder)
    {
        var baseOrdered = query.OrderByDescending(a => a.IsFeatured);
        var by = sortBy?.ToLower() ?? "date";
        var order = sortOrder?.ToLower() ?? "desc";

        return (by, order) switch
        {
            ("rent" or "price", "asc")  => baseOrdered.ThenBy(a => a.Rent > 0 ? a.Rent : a.Price ?? 0),
            ("rent" or "price", "desc") => baseOrdered.ThenByDescending(a => a.Rent > 0 ? a.Rent : a.Price ?? 0),
            ("size", "asc")             => baseOrdered.ThenBy(a => a.SizeSquareMeters),
            ("size", "desc")            => baseOrdered.ThenByDescending(a => a.SizeSquareMeters),
            ("date", "asc")             => baseOrdered.ThenBy(a => a.CreatedDate),
            _                           => baseOrdered.ThenByDescending(a => a.CreatedDate)
        };
    }
}
