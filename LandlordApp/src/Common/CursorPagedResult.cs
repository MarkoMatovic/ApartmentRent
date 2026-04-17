namespace Lander.src.Common;

/// <summary>
/// Keyset/ID-based pagination result — O(1) regardless of page depth.
/// Use instead of offset pagination for large datasets.
/// </summary>
public class KeysetPagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public string? NextPageToken { get; init; }   // null = no more pages
    public bool HasMore => NextPageToken is not null;
}

public static class KeysetPagedResultExtensions
{
    /// <summary>
    /// Execute keyset pagination on an ordered IQueryable using int ID as the page token.
    /// The query MUST be ordered by Id ascending before calling this.
    /// </summary>
    public static async Task<KeysetPagedResult<T>> ToKeysetPagedResultAsync<T>(
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

        string? nextPageToken = null;
        if (items.Count > pageSize)
        {
            items.RemoveAt(items.Count - 1);
            nextPageToken = idSelector(items[^1]).ToString();
        }

        return new KeysetPagedResult<T> { Items = items, NextPageToken = nextPageToken };
    }
}
