using FluentValidation;
using FluentValidation.AspNetCore;
using Lander;
using Lander.Helpers;
using Lander.Middleware;
using Lander.src.Infrastructure.Extensions;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Notifications.NotificationsHub;
using Lander.src.Modules.Reviews.Implementation;
using Serilog;
using Serilog.Events;

// Bootstrap logger — hvata greske pre ucitavanja konfiguracije
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
});

// TEMP: k6 performance testing — higher connection limits to avoid TCP backlog on Windows loopback
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
    options.Limits.MinRequestBodyDataRate = null;
});

StartupValidation.ValidateSecrets(builder.Configuration, builder.Environment);

builder.Host.UseSerilog((ctx, svc, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(svc)
       .Enrich.FromLogContext()
       .Enrich.WithProperty("Application", "Landlander")
       .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
       .WriteTo.File(
           path: "logs/landlander-.log",
           rollingInterval: RollingInterval.Day,
           retainedFileCountLimit: 14,
           outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

    var aiKey = ctx.Configuration["ApplicationInsights:InstrumentationKey"];
    if (!string.IsNullOrWhiteSpace(aiKey))
        cfg.WriteTo.ApplicationInsights(
            svc.GetRequiredService<Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration>(),
            TelemetryConverter.Traces);
});

builder.Services.AddDatabaseContexts(builder.Configuration);

builder.Services.AddApplicationInsightsTelemetry();

var allowedOrigins = builder.Configuration.GetSection("App:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://127.0.0.1:5173", "https://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithAuth();
builder.Services.AddSignalR();
builder.Services.AddGrpc();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.AddApiRateLimiting();

builder.Services.AddMemoryCache();

var redisConnectionString = builder.Configuration["Redis:Configuration"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(opts =>
    {
        opts.Configuration = redisConnectionString;
        opts.InstanceName = "Landlander:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddSingleton<IdempotencyService>();
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Cache());
    options.AddPolicy("ApartmentsList", builder =>
        builder.Expire(TimeSpan.FromMinutes(5))
               .SetVaryByQuery(new[] { "listingType", "city", "minRent", "maxRent", "page", "pageSize",
                                       "numberOfRooms", "apartmentType", "isFurnished",
                                       "isPetFriendly", "isSmokingAllowed", "hasParking",
                                       "hasBalcony", "isImmediatelyAvailable" })
               .Tag("apartments"));
    options.AddPolicy("ApartmentDetail", builder =>
        builder.Expire(TimeSpan.FromMinutes(10))
               .SetVaryByRouteValue(new[] { "id" })
               .Tag("apartments"));
});

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

var hcBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<Lander.ListingsContext>("db-listings")
    .AddDbContextCheck<Lander.UsersContext>("db-users");

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    hcBuilder.AddRedis(redisConnectionString, name: "redis");
}

var app = builder.Build();

// Global exception handler - must be first in the pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseResponseCompression();
app.UseOutputCache();
app.UseRateLimiter();
app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0}ms)";
    opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
    {
        diagCtx.Set("ClientIP", httpCtx.Connection.RemoteIpAddress?.ToString());
        diagCtx.Set("UserAgent", httpCtx.Request.Headers.UserAgent.ToString());
    };
});

app.UseCors("AllowFrontend");

// ─── Security headers ────────────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.Append("X-Content-Type-Options", "nosniff");
    headers.Append("X-Frame-Options", "DENY");
    headers.Append("X-XSS-Protection", "1; mode=block");
    headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

    if (!app.Environment.IsDevelopment())
    {
        // HSTS — enforce HTTPS for 2 years including subdomains
        headers.Append("Strict-Transport-Security", "max-age=63072000; includeSubDomains; preload");
        headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self'; " +
            "img-src 'self' data: blob: https:; " +
            "connect-src 'self' wss: ws:; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'");
    }
    else
    {
        // Relaxed CSP for development (Vite HMR, SignalR)
        headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src * data: blob:; " +
            "connect-src * ws: wss:; " +
            "frame-ancestors 'none'");
    }

    await next();
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ReviewFavoriteService>();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ChatHub>("/chatHub");
app.MapHealthChecks("/health");

app.Run();
