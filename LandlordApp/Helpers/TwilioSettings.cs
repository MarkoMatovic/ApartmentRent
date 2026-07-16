namespace Lander.Helpers;

// All properties nullable — SMS is an optional feature; missing config disables it gracefully.
public class TwilioSettings
{
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string? PhoneNumber { get; set; }
}
