using System.Text.Json;

namespace Lander.src.Notifications;

/// <summary>
/// What the browser needs to show a notification in the user's own language and to open the right page
/// when it is clicked. The server still sends (and stores) a Serbian title and message as a fallback for
/// anything that does not know the key; the frontend prefers its own translation of <c>key</c>, filled
/// with <c>params</c>. It travels as the optional 5th argument of the "ReceiveNotification" push.
/// </summary>
public static class NotificationDescriptor
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>An in-app route is the only kind of link the frontend follows (ids and URLs are ignored).</summary>
    public static bool IsRoute(string? target) =>
        !string.IsNullOrEmpty(target) && target.StartsWith('/') && !target.StartsWith("//");

    /// <summary>
    /// JSON for the push, or null when there is nothing to tell: no id, no translation key and no route.
    /// The id is that of the stored notification, so the browser can mark it read on the server.
    /// </summary>
    public static string? Create(string? key, IReadOnlyDictionary<string, string>? parameters, string? link, int? id = null)
    {
        var route = IsRoute(link) ? link : null;
        var storedId = id is > 0 ? id : null;
        if (string.IsNullOrEmpty(key) && route is null && storedId is null) return null;

        return JsonSerializer.Serialize(new
        {
            id = storedId,
            key = string.IsNullOrEmpty(key) ? null : key,
            @params = parameters is { Count: > 0 } ? parameters : null,
            link = route,
        }, Json);
    }
}
