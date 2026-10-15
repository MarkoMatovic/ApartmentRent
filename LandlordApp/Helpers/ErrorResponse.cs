namespace Lander.Helpers;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Details { get; set; }
    /// <summary>Stable code for business-rule failures (see BusinessErrorCodes); null otherwise.</summary>
    public string? Code { get; set; }
    public string? TraceId { get; set; }
}
