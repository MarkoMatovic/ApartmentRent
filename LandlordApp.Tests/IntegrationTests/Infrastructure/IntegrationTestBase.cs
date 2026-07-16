using System.Net.Http.Headers;
using Lander;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LandlordApp.Tests.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration test classes. Provides HttpClient helpers and
/// a seeding API for direct DB access. Test classes declare IClassFixture themselves.
/// </summary>
public abstract class IntegrationTestBase
{
    protected readonly LanderWebApplicationFactory Factory;

    protected IntegrationTestBase(LanderWebApplicationFactory factory)
    {
        Factory = factory;
    }

    // ── Client helpers ───────────────────────────────────────────────────────

    protected HttpClient CreateAnonymousClient()
        => Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            // Auth cookies are Secure — the CookieContainer only sends them over https
            BaseAddress = new Uri("https://localhost"),
        });

    protected HttpClient CreateAuthenticatedClient(int userId, Guid userGuid, string role = "Tenant")
    {
        var client = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost"),
        });
        var token = TestJwtGenerator.Generate(userId, userGuid, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── Scope / DB helpers ───────────────────────────────────────────────────

    protected IServiceScope CreateScope() => Factory.Services.CreateScope();

    /// <summary>Seed a verified, active user. Returns the saved user with auto-assigned UserId.</summary>
    protected async Task<User> SeedUserAsync(
        string email,
        string? password = null,
        Guid? userGuid = null,
        bool chatHistoryConsent = true,
        string role = "Tenant")
    {
        using var scope = CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<UsersContext>();

        // Ensure the role row exists (needed by navigation property on user)
        var roleRow = await ctx.Roles.FirstOrDefaultAsync(r => r.RoleName == role);
        if (roleRow == null)
        {
            roleRow = new Role { RoleName = role, Description = $"{role} role" };
            ctx.Roles.Add(roleRow);
            await ctx.SaveChangesAsync();
        }

        var user = new User
        {
            FirstName = "Test",
            LastName  = "User",
            Email     = email,
            Password  = BCrypt.Net.BCrypt.HashPassword(password ?? "Password123!"),
            UserGuid  = userGuid ?? Guid.NewGuid(),
            IsActive  = true,          // IsActive = true means email verified in this app
            EmailVerifiedAt   = DateTime.UtcNow,
            ChatHistoryConsent = chatHistoryConsent,
            UserRoleId = roleRow.RoleId,
        };

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user;
    }

    /// <summary>Seed an apartment owned by the given landlord. Returns the saved entity.</summary>
    protected async Task<Apartment> SeedApartmentAsync(int landlordId, string title = "Test Apartment", string city = "Sarajevo")
    {
        using var scope = CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ListingsContext>();

        var apartment = new Apartment
        {
            LandlordId = landlordId,
            Title      = title,
            Rent       = 500,
            Address    = "Testna 1",
            City       = city,
            IsActive   = true,
            IsDeleted  = false,
        };

        ctx.Apartments.Add(apartment);
        await ctx.SaveChangesAsync();
        return apartment;
    }

    /// <summary>Seed a message between two users.</summary>
    protected async Task<Message> SeedMessageAsync(int senderId, int receiverId, string text = "Hello!")
    {
        using var scope = CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CommunicationsContext>();

        var message = new Message
        {
            SenderId    = senderId,
            ReceiverId  = receiverId,
            MessageText = text,
            SentAt      = DateTime.UtcNow,
            IsRead      = false,
        };

        ctx.Messages.Add(message);
        await ctx.SaveChangesAsync();
        return message;
    }

    /// <summary>Update a user's ChatHistoryConsent directly in the DB.</summary>
    protected async Task SetChatHistoryConsentAsync(int userId, bool consent)
    {
        using var scope = CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<UsersContext>();
        var user = await ctx.Users.FindAsync(userId);
        if (user != null)
        {
            user.ChatHistoryConsent = consent;
            await ctx.SaveChangesAsync();
        }
    }
}
