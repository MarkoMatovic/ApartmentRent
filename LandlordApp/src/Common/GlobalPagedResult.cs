namespace Lander.src.Common;

public class KeysetPagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public string? NextPageToken { get; init; } 
    public bool HasMore => NextPageToken is not null;
}

public static class KeysetPagedResultExtensions
{
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
