namespace Lander.src.Modules.Analytics.Dtos.InputDto;

/// <summary>
/// Common optional date-range filter shared by analytics endpoints.
/// Bound from the query string (?from=...&amp;to=...). Both bounds are optional.
/// </summary>
public class DateRangeQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

/// <summary>
/// Date-range filter plus a result-count cap for "top N" analytics endpoints
/// (?count=...&amp;from=...&amp;to=...). Count defaults to 10 when omitted.
/// </summary>
public class TopEntityQuery : DateRangeQuery
{
    public int Count { get; set; } = 10;
}
