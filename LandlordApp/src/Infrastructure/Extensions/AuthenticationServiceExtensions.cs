using System.Text;
using Lander.Helpers;
using Lander.src.Infrastructure.Authorization;
using Lander.src.Infrastructure.Services;
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
                // Reject tokens that were explicitly revoked at logout.
                o.Events = new JwtBearerEvents
                {
                    // Browsers cannot set an Authorization header on a WebSocket handshake,
                    // so SignalR sends the JWT via the access_token query string. Pull it
                    // into the request for the hub paths so [Authorize] hubs authenticate
                    // over WebSockets instead of silently degrading to long-polling.
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["access_token"];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/chatHub") ||
                             path.StartsWithSegments("/notificationHub")))
                        {
                            ctx.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async ctx =>
                    {
                        var jti = ctx.Principal?.FindFirst("jti")?.Value;
                        if (string.IsNullOrEmpty(jti)) return;

                        var blacklist = ctx.HttpContext.RequestServices
                            .GetRequiredService<IJwtBlacklistService>();
                        if (await blacklist.IsBlacklistedAsync(jti))
                            ctx.Fail("Token has been revoked.");
                    }
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
