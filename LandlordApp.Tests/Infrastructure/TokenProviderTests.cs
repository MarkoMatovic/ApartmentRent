using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Lander;
using Lander.Helpers;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace LandlordApp.Tests.Infrastructure;

public class TokenProviderTests : IDisposable
{
    private readonly IConfiguration _config;
    private readonly UsersContext _context;
    private readonly TokenProvider _tokenProvider;

    public TokenProviderTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "SuperSecretTestKey12345678901234",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        var options = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new UsersContext(options);
        _tokenProvider = new TokenProvider(_config, _context);
    }

    public void Dispose() => _context.Dispose();

    private static User BuildUser(int? userRoleId = null, Role? role = null, string? phone = null)
    {
        return new User
        {
            UserId = 1,
            UserGuid = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            UserRoleId = userRoleId,
            UserRole = role,
            PhoneNumber = phone
        };
    }

    private JwtSecurityToken DecodeToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ReadJwtToken(token);
    }

    [Fact]
    public async Task CreateAsync_ValidUser_ReturnsNonEmptyToken()
    {
        var user = BuildUser();

        var token = await _tokenProvider.CreateAsync(user);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateAsync_TokenContainsEmailClaim()
    {
        var user = BuildUser();
        user.Email = "user@domain.com";

        var token = await _tokenProvider.CreateAsync(user);
        var jwt = DecodeToken(token);

        jwt.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Email && c.Value == "user@domain.com");
    }

    [Fact]
    public async Task CreateAsync_TokenContainsRoleClaim()
    {
        var role = new Role { RoleId = 1, RoleName = "Landlord" };
        var user = BuildUser(userRoleId: 1, role: role);

        var token = await _tokenProvider.CreateAsync(user);
        var jwt = DecodeToken(token);

        // ClaimTypes.Role maps to the long URI; check by value
        jwt.Claims.Should().Contain(c => c.Value == "Landlord");
    }

    [Fact]
    public async Task CreateAsync_TokenExpiresIn15Minutes()
    {
        var user = BuildUser();
        var before = DateTime.UtcNow;

        var token = await _tokenProvider.CreateAsync(user);
        var jwt = DecodeToken(token);

        var expiry = jwt.ValidTo;
        expiry.Should().BeCloseTo(before.AddMinutes(15), precision: TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task CreateAsync_UserWithRole_IncludesPermissionClaims()
    {
        var role = new Role { RoleId = 10, RoleName = "Admin" };
        var permission = new Permission { PermissionId = 1, PermissionName = "manage_listings" };
        var rolePermission = new RolePermission
        {
            RoleId = 10,
            PermissionId = 1,
            Role = role,
            Permission = permission
        };

        await _context.Roles.AddAsync(role);
        await _context.Permissions.AddAsync(permission);
        await _context.RolePermissions.AddAsync(rolePermission);
        await _context.SaveChangesAsync();

        var user = BuildUser(userRoleId: 10, role: role);

        var token = await _tokenProvider.CreateAsync(user);
        var jwt = DecodeToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "manage_listings");
    }

    [Fact]
    public async Task CreateAsync_UserWithoutRole_NoPermissionClaims()
    {
        var user = BuildUser(userRoleId: null, role: null);

        var token = await _tokenProvider.CreateAsync(user);
        var jwt = DecodeToken(token);

        jwt.Claims.Should().NotContain(c => c.Type == "permission");
    }

    [Fact]
    public async Task CreateAsync_UserWithPhone_IncludesPhoneClaim()
    {
        var user = BuildUser(phone: "+385911234567");

        var token = await _tokenProvider.CreateAsync(user);
        var jwt = DecodeToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "phone_number" && c.Value == "+385911234567");
    }
}
