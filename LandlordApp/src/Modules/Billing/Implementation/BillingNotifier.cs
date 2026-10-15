using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Billing.Implementation;

/// <summary>
/// The bell (and, for reminders, an e-mail) about tenancies and bills. Title and message are the Serbian
/// fallback; the key and its values let the browser show it in the user's language. A failed
/// notification never undoes the payment step that caused it.
/// </summary>
public sealed class BillingNotifier
{
    public const string BillsPage = "/bills";
    public static string TenancyPage(int tenancyId) => $"/bills/tenancy/{tenancyId}";

    private readonly INotificationService _notifications;
    private readonly IEmailService _email;
    private readonly UsersContext _users;
    private readonly ILogger<BillingNotifier> _logger;

    public BillingNotifier(INotificationService notifications, IEmailService email, UsersContext users, ILogger<BillingNotifier> logger)
    {
        _notifications = notifications;
        _email = email;
        _users = users;
        _logger = logger;
    }

    public async Task NotifyAsync(
        int recipientUserId, int senderUserId, string key, string title, string message,
        Dictionary<string, string> values, string link, string type = "info")
    {
        try
        {
            await _notifications.SendNotificationAsync(new CreateNotificationInputDto
            {
                Title = title,
                Message = message,
                ActionType = key,
                ActionTarget = link,
                SenderUserId = senderUserId,
                RecipientUserId = recipientUserId,
                Key = key,
                Params = values,
                Type = type,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send billing notification {Key} to user {UserId}", key, recipientUserId);
        }
    }

    /// <summary>A new month's rent: every tenant is told their part and the due date.</summary>
    public async Task NotifyRentBillAsync(Models.Bill bill)
    {
        var landlord = await NameOfAsync(bill.Tenancy.LandlordUserId);
        foreach (var share in bill.Shares)
            await NotifyAsync(share.PayerUserId, bill.Tenancy.LandlordUserId, "bill_new", $"Novi račun: {bill.Title}",
                $"Vaš deo: {share.Amount:N2} RSD, rok {bill.DueDate:dd.MM.yyyy.}",
                new Dictionary<string, string>
                {
                    ["name"] = landlord, ["title"] = bill.Title,
                    ["amount"] = BillingMath.Plain(share.Amount), ["due"] = BillingMath.Plain(bill.DueDate),
                },
                BillsPage);
    }

    /// <summary>Reminders matter most to people who are not on the site, so they go by mail as well.</summary>
    public async Task EmailReminderAsync(int recipientUserId, string billTitle, decimal amount, DateOnly dueDate, bool overdue)
    {
        try
        {
            var recipient = await _users.Users.AsNoTracking()
                .Where(u => u.UserId == recipientUserId)
                .Select(u => new { u.Email, u.FirstName })
                .FirstOrDefaultAsync();
            if (recipient is null || string.IsNullOrEmpty(recipient.Email)) return;
            await _email.SendBillReminderEmailAsync(recipient.Email, recipient.FirstName, billTitle, amount, dueDate, overdue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not e-mail a bill reminder to user {UserId}", recipientUserId);
        }
    }

    public async Task<string> NameOfAsync(int userId)
    {
        var user = await _users.Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstOrDefaultAsync();
        return user is null ? "Neko" : $"{user.FirstName} {user.LastName}".Trim();
    }
}
