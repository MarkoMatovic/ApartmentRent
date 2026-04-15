namespace Lander.src.Common;

/// <summary>
/// Keyset/cursor-based pagination result — O(1) regardless of page depth.
/// Use instead of offset pagination for large datasets.
/// </summary>
public class CursorPagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public string? NextCursor { get; init; }   // null = no more pages
    public bool HasMore => NextCursor is not null;
}

public static class CursorPagedResultExtensions
{
    /// <summary>
    /// Execute keyset pagination on an ordered IQueryable using int ID as cursor.
    /// The query MUST be ordered by Id ascending before calling this.
    /// </summary>
    public static async Task<CursorPagedResult<T>> ToCursorPagedResultAsync<T>(
        this IQueryable<T> source,
        int? afterId,
        int pageSize,
        Func<T, int> idSelector,
        CancellationToken ct = default)
        where T : class
    {
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Take one extra to know if there's a next page
        var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(source.Take(pageSize + 1), ct);

        string? nextCursor = null;
        if (items.Count > pageSize)
        {
            items.RemoveAt(items.Count - 1);
            nextCursor = idSelector(items[^1]).ToString();
        }

        return new CursorPagedResult<T> { Items = items, NextCursor = nextCursor };
    }
}
