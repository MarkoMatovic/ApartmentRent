using System.Text;
using Lander.Helpers;
using Lander.src.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Lander.src.Infrastructure.Extensions;

public static class AuthenticationServiceExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = true;
                o.SaveToken = true;
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub",
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("LandlordPolicy",  policy => policy.RequireRole(RoleConstants.Landlord));
            options.AddPolicy("TenantPolicy",    policy => policy.RequireRole(RoleConstants.Tenant));
            options.AddPolicy("AdminPolicy",     policy => policy.RequireRole(RoleConstants.Admin));
            options.AddPolicy("BrokerPolicy",    policy => policy.RequireRole(RoleConstants.Broker));
            options.AddPolicy("GuestPolicy",     policy => policy.RequireRole(RoleConstants.Guest));
            options.AddPolicy("PremiumPolicy",   policy => policy.RequireRole(RoleConstants.PremiumTenant, RoleConstants.PremiumLandlord));
        });

        return services;
    }
}
