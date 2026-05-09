using Lander;
using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Roommates.Models;
using Lander.src.Modules.SavedSearches.Models;
using Lander.src.Modules.SearchRequests.Models;
using Lander.src.Notifications.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LandlordApp.Tests.E2eTests.Infrastructure;

/// <summary>
/// Centralized factory for seeding test data directly into the real SQL database.
/// Tests use this instead of calling API endpoints to set up pre-conditions,
/// keeping test setup fast and independent of the feature being tested.
/// </summary>
public class TestDataFactory
{
    private readonly IServiceProvider _sp;

    public TestDataFactory(IServiceProvider sp) => _sp = sp;

    // ── Users ─────────────────────────────────────────────────────────────────

    /// <summary>Seeds a verified, active user. Returns the saved entity with auto-assigned UserId.</summary>
    public async Task<User> CreateUserAsync(
        string email,
        string? password = null,
        string role = "Tenant",
        bool isActive = true,
        bool chatHistoryConsent = true)
    {
        // Do NOT use 'await using' here — DbContext is scoped and owned by the DI scope.
        // Disposing it manually would make the same instance unusable for the rest of the test.
        var ctx = _sp.GetRequiredService<UsersContext>();

        var roleRow = await ctx.Roles.FirstOrDefaultAsync(r => r.RoleName == role)
                      ?? throw new InvalidOperationException($"Role '{role}' not found — was base data seeded?");

        var user = new User
        {
            FirstName          = "Test",
            LastName           = "User",
            Email              = email,
            Password           = BCrypt.Net.BCrypt.HashPassword(password ?? "Password123!"),
            UserGuid           = Guid.NewGuid(),
            IsActive           = isActive,
            EmailVerifiedAt    = isActive ? DateTime.UtcNow : null,
            ChatHistoryConsent = chatHistoryConsent,
            UserRoleId         = roleRow.RoleId,
            CreatedDate        = DateTime.UtcNow,
        };

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user;
    }

    // ── Apartments ────────────────────────────────────────────────────────────

    /// <summary>Seeds an active apartment owned by the given landlord.</summary>
    public async Task<Apartment> CreateApartmentAsync(
        int landlordId,
        string title = "Test Apartment",
        decimal rent = 500m,
        string city = "Sarajevo",
        int sizeSquareMeters = 50)
    {
        var ctx = _sp.GetRequiredService<ListingsContext>();

        var apartment = new Apartment
        {
            LandlordId       = landlordId,
            Title            = title,
            Rent             = rent,
            Address          = "Testna 1",
            City             = city,
            SizeSquareMeters = sizeSquareMeters,
            IsActive         = true,
            IsDeleted        = false,
        };

        ctx.Apartments.Add(apartment);
        await ctx.SaveChangesAsync();
        return apartment;
    }

    // ── Applications ─────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds an approved application so the tenant passes the approval gate
    /// required by AppointmentService before scheduling a viewing.
    /// </summary>
    public async Task<ApartmentApplication> CreateApprovedApplicationAsync(int tenantId, int apartmentId)
    {
        var ctx = _sp.GetRequiredService<ApplicationsContext>();

        var application = new ApartmentApplication
        {
            UserId          = tenantId,
            ApartmentId     = apartmentId,
            Status          = "Approved",
            ApplicationDate = DateTime.UtcNow,
            CreatedDate     = DateTime.UtcNow,
        };

        ctx.ApartmentApplications.Add(application);
        await ctx.SaveChangesAsync();
        return application;
    }

    // ── Roommates ─────────────────────────────────────────────────────────────

    /// <summary>Seeds a roommate profile for the given user.</summary>
    public async Task<Roommate> CreateRoommateAsync(
        int userId,
        string profession    = "Software Engineer",
        string? location     = null,
        decimal budgetMin    = 300m,
        decimal budgetMax    = 700m)
    {
        var ctx = _sp.GetRequiredService<RoommatesContext>();

        var roommate = new Roommate
        {
            UserId          = userId,
            Profession      = profession,
            PreferredLocation = location ?? "Sarajevo",
            BudgetMin       = budgetMin,
            BudgetMax       = budgetMax,
            IsActive        = true,
            CreatedDate     = DateTime.UtcNow,
        };

        ctx.Roommates.Add(roommate);
        await ctx.SaveChangesAsync();
        return roommate;
    }

    // ── Messages ──────────────────────────────────────────────────────────────

    /// <summary>Seeds a direct message from sender to receiver.</summary>
    public async Task<Message> CreateMessageAsync(
        int senderId,
        int receiverId,
        string text = "Test message",
        bool isRead = false)
    {
        var ctx = _sp.GetRequiredService<CommunicationsContext>();

        var message = new Message
        {
            SenderId    = senderId,
            ReceiverId  = receiverId,
            MessageText = text,
            IsRead      = isRead,
            SentAt      = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
        };

        ctx.Messages.Add(message);
        await ctx.SaveChangesAsync();
        return message;
    }

    // ── Notifications ─────────────────────────────────────────────────────────

    /// <summary>Seeds a notification for the given recipient.</summary>
    public async Task<Notification> CreateNotificationAsync(
        int recipientUserId,
        int senderUserId,
        string title   = "Test Notification",
        string message = "This is a test notification.")
    {
        var ctx = _sp.GetRequiredService<NotificationContext>();

        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            SenderUserId    = senderUserId,
            Title           = title,
            Message         = message,
            ActionType      = "Test",
            ActionTarget    = "/",
            IsRead          = false,
            CreatedDate     = DateTime.UtcNow,
            CreatedByGuid   = Guid.NewGuid(),
        };

        ctx.Notifications.Add(notification);
        await ctx.SaveChangesAsync();
        return notification;
    }

    // ── SavedSearches ─────────────────────────────────────────────────────────

    /// <summary>Seeds a saved search for the given user.</summary>
    public async Task<SavedSearch> CreateSavedSearchAsync(
        int userId,
        string name       = "Moja pretraga",
        string searchType = "Apartments")
    {
        var ctx = _sp.GetRequiredService<SavedSearchesContext>();

        var search = new SavedSearch
        {
            UserId                   = userId,
            Name                     = name,
            SearchType               = searchType,
            FiltersJson              = "{\"city\":\"Sarajevo\"}",
            EmailNotificationsEnabled = true,
            IsActive                 = true,
            CreatedDate              = DateTime.UtcNow,
        };

        ctx.SavedSearches.Add(search);
        await ctx.SaveChangesAsync();
        return search;
    }

    // ── SearchRequests ────────────────────────────────────────────────────────

    /// <summary>Seeds a search request for the given user.</summary>
    public async Task<SearchRequest> CreateSearchRequestAsync(
        int userId,
        string title                     = "Tražim stan u centru",
        SearchRequestType requestType    = SearchRequestType.LookingForApartment,
        string? city                     = "Sarajevo")
    {
        var ctx = _sp.GetRequiredService<SearchRequestsContext>();

        var req = new SearchRequest
        {
            UserId      = userId,
            RequestType = requestType,
            Title       = title,
            City        = city,
            BudgetMin   = 300m,
            BudgetMax   = 700m,
            IsActive    = true,
            CreatedDate = DateTime.UtcNow,
        };

        ctx.SearchRequests.Add(req);
        await ctx.SaveChangesAsync();
        return req;
    }

    // ── ReportedMessages ──────────────────────────────────────────────────────

    /// <summary>Seeds a reported-message entry (requires an existing Message seed first).</summary>
    public async Task<ReportedMessage> CreateReportedMessageAsync(
        int messageId,
        int reportedByUserId,
        int reportedUserId,
        string reason = "Spam or harassment")
    {
        var ctx = _sp.GetRequiredService<CommunicationsContext>();

        var report = new ReportedMessage
        {
            MessageId        = messageId,
            ReportedByUserId = reportedByUserId,
            ReportedUserId   = reportedUserId,
            Reason           = reason,
            Status           = "Pending",
            CreatedDate      = DateTime.UtcNow,
            CreatedByGuid    = Guid.NewGuid(),
        };

        ctx.ReportedMessages.Add(report);
        await ctx.SaveChangesAsync();
        return report;
    }

    // ── Resolve services (for advanced seeding in specific tests) ────────────

    public T Resolve<T>() where T : notnull => _sp.GetRequiredService<T>();
}
