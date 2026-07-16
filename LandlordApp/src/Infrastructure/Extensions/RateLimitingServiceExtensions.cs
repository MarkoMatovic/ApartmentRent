using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Lander.src.Infrastructure.Extensions;

public static class RateLimitingServiceExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // LOGIN: max 5 pokušaja / 15 min po IP-u — nakon 5 zahtjeva lockout traje
            // do isteka prozora (do 15 minuta), što odgovara Security:LockoutMinutes u appsettings.
            options.AddPolicy<string>("login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // REGISTER: max 3 registracije / sat po IP-u — sprječava masovno kreiranje lažnih naloga.
            options.AddPolicy<string>("register", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // CHANGE-PASSWORD: max 3 pokušaja / 10 min po korisniku (userId claim);
            // pada nazad na IP ako korisnik nije autentificiran.
            options.AddPolicy<string>("change-password", httpContext =>
            {
                var userKey = httpContext.User?.FindFirst("userId")?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"cp:{userKey}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(10),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // MESSAGES-SEND: max 30 poruka / minuta po korisniku.
            options.AddPolicy<string>("messages-send", httpContext =>
            {
                var userKey = httpContext.User?.FindFirst("userId")?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"msg:{userKey}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // CREATE-REVIEW: max 5 recenzija / dan po korisniku.
            options.AddPolicy<string>("create-review", httpContext =>
            {
                var userKey = httpContext.User?.FindFirst("userId")?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"rev:{userKey}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromHours(24),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // AUTH (zadržan za ostale auth endpointe: forgot-password, reset-password, send-verification-email)
            options.AddPolicy<string>("auth", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromSeconds(30),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Globalni limit za sve ostale API pozive
            options.AddFixedWindowLimiter("global", policy =>
            {
                policy.PermitLimit = 100;
                policy.Window = TimeSpan.FromMinutes(1);
                policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                policy.QueueLimit = 5;
            });

            options.AddFixedWindowLimiter("mutating", policy =>
            {
                policy.PermitLimit = 30;
                policy.Window = TimeSpan.FromMinutes(1);
                policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                policy.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("readonly", policy =>
            {
                policy.PermitLimit = 100;
                policy.Window = TimeSpan.FromMinutes(1);
                policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                policy.QueueLimit = 5;
            });

            // ANALYTICS-TRACK: anonimni endpoint — max 60 događaja / minuta po IP-u
            // (sprječava botove da pumpaju analitičku tabelu i lažiraju "top viewed").
            options.AddPolicy<string>("analytics-track", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // SEMANTIC-SEARCH: anonimni CPU-teški endpoint — max 20 zahtjeva / minuta po IP-u.
            options.AddPolicy<string>("semantic-search", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Limit za SignalR upgrade konekcije — max 20 novih konekcija po IP-u u minuti (S-13 fix)
            options.AddPolicy<string>("signalr", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
            options.RejectionStatusCode = 429;
        });

        return services;
    }
}
