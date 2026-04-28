namespace Lander.src.Common;

/// <summary>
/// Centralised constants for application/payment status strings stored in the database.
/// Avoids magic string literals scattered across the codebase.
/// </summary>
public static class ApplicationStatuses
{
    public const string Pending   = "Pending";
    public const string Approved  = "Approved";
    public const string Rejected  = "Rejected";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
    public const string Confirmed = "Confirmed";
    public const string Success   = "Success";
    public const string Failed    = "Failed";

    // Report-specific statuses (ReportedMessage.Status)
    public const string Reviewed  = "Reviewed";
    public const string Resolved  = "Resolved";
}
