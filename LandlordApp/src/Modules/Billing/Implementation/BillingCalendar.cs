namespace Lander.src.Modules.Billing.Implementation;

/// <summary>Dates of bills are calendar days in Serbia, whatever the server's own time zone is.</summary>
public static class BillingCalendar
{
    private static readonly TimeZoneInfo Belgrade = FindBelgrade();

    /// <summary>How long before the due date the month's rent bill appears.</summary>
    public const int RentLeadDays = 10;

    /// <summary>How many days before the due date the reminder goes out.</summary>
    public const int ReminderDaysBefore = 3;

    public static DateOnly Today(TimeProvider time) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), Belgrade).DateTime);

    /// <summary>"2026-10".</summary>
    public static string Period(int year, int month) => $"{year:D4}-{month:D2}";

    /// <summary>The rent due date of a month (the due day is at most 28, so every month has it).</summary>
    public static DateOnly RentDueDate(int year, int month, int dueDay) => new(year, month, dueDay);

    private static TimeZoneInfo FindBelgrade()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Belgrade");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        }
    }
}
