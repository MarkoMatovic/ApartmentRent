using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.DataProtection;
using FluentValidation.AspNetCore;
using Lander;
using Lander.Helpers;
using Lander.Middleware;
using Lander.src.Infrastructure.Extensions;
using Lander.src.Infrastructure.Hangfire;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Notifications.NotificationsHub;
using Lander.src.Modules.Reviews.Implementation;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
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

builder.WebHost.ConfigureKestrel(options =>
{
    // Hard cap to prevent resource exhaustion under load.
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
    // 10 MB max body — returns 413 before the payload reaches controllers.
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
    // Keep the default MinRequestBodyDataRate so slow-loris-style attacks are mitigated.
});

// Background services ne smiju rušiti cijeli host pri TaskCanceledException (shutdown)
builder.Services.Configure<HostOptions>(o =>
    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

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

    // ─── Centralized log sink: Seq ────────────────────────────────────────────
    // Set Serilog:Seq:ServerUrl in appsettings / env to ship all structured logs
    // to a Seq instance (local dev: http://localhost:5341, prod: your Seq URL).
    // Seq is free for single-developer use; self-host or use Datalust Cloud.
    // For Azure Log Analytics instead, replace this block with Serilog.Sinks.AzureAnalytics.
    var seqUrl = ctx.Configuration["Serilog:Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        var seqApiKey = ctx.Configuration["Serilog:Seq:ApiKey"]; // optional
        cfg.WriteTo.Seq(seqUrl, apiKey: string.IsNullOrWhiteSpace(seqApiKey) ? null : seqApiKey);
    }

    var aiKey = ctx.Configuration["ApplicationInsights:InstrumentationKey"];
    if (!string.IsNullOrWhiteSpace(aiKey))
        cfg.WriteTo.ApplicationInsights(
            svc.GetRequiredService<Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration>(),
            TelemetryConverter.Traces);
// preserveStaticLogger: each host keeps its own logger instead of freezing the
// static bootstrap logger — required when multiple hosts run in one process
// (WebApplicationFactory-based tests) and harmless in production.
}, preserveStaticLogger: true);

builder.Services.AddDatabaseContexts(builder.Configuration);

// Register Application Insights only when actually configured. The AI SDK spawns
// FOREGROUND aggregation threads (DefaultAggregationPeriodCycle) per host, which
// keep the process alive after shutdown — hangs `dotnet test` and local runs.
var appInsightsConfigured =
    !string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]) ||
    !string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:InstrumentationKey"]) ||
    !string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
if (appInsightsConfigured)
    builder.Services.AddApplicationInsightsTelemetry();

// ─── OpenTelemetry distributed tracing ───────────────────────────────────────
// Instruments ASP.NET Core requests, outgoing HTTP calls, and SQL queries so
// every cross-service hop has a shared trace-id and parent-span relationship.
// In production export to OTLP (e.g. Azure Monitor, Jaeger, Seq) by setting
// OpenTelemetry:OtlpEndpoint in appsettings / env.  Console exporter is active
// in Development so traces appear in the local run output immediately.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Landlander")
            .ConfigureResource(r => r.AddService("Landlander", serviceVersion: "1.0"))
            .AddAspNetCoreInstrumentation(o =>
            {
                o.RecordException = true;
                // Exclude noisy health/static endpoints from traces
                o.Filter = ctx =>
                    !ctx.Request.Path.StartsWithSegments("/health") &&
                    !ctx.Request.Path.StartsWithSegments("/metrics") &&
                    !ctx.Request.Path.StartsWithSegments("/favicon");
            })
            .AddHttpClientInstrumentation()
            // SetDbStatementForText includes the full SQL text in traces.
            // Disable in production to avoid leaking query parameters (potential PII).
            .AddSqlClientInstrumentation(o => o.SetDbStatementForText = builder.Environment.IsDevelopment());

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        else if (builder.Environment.IsDevelopment())
            tracing.AddConsoleExporter();
    });

var allowedOrigins = builder.Configuration.GetSection("App:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://127.0.0.1:5173", "https://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              // Explicit method/header allowlist instead of AllowAny* — reduces attack surface.
              // SignalR negotiate + WebSocket require GET/POST and the listed headers.
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
              .WithHeaders(
                  "Content-Type",
                  "Authorization",
                  "X-Requested-With",
                  "X-Idempotency-Key",
                  "Accept",
                  "Origin")
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // Serialize/deserialize enum values as their string names (e.g. "Confirmed"
        // instead of 1). Applies to all enums including AppointmentStatus.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithAuth();
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

// Hard deadline per HTTP request — server returns 503 instead of hanging indefinitely.
// File upload and long-polling endpoints can override this via [RequestTimeout] attribute.
builder.Services.AddRequestTimeouts(o =>
    o.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30),
        TimeoutStatusCode = StatusCodes.Status503ServiceUnavailable
    });

builder.Services.AddMemoryCache();

// ─── Prometheus metrics ───────────────────────────────────────────────────────
// Exposes /metrics for Prometheus scraping (HTTP duration, status code, GC/CLR).
// Middleware wired via app.UseHttpMetrics() below; endpoint via app.MapMetrics().

var redisConnectionString = builder.Configuration["Redis:Configuration"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(opts =>
    {
        opts.Configuration = redisConnectionString;
        opts.InstanceName = "Landlander:";
    });

    // SignalR Redis backplane — all horizontal instances share the same pub/sub bus.
    // Without this every app instance has an isolated in-memory hub and users on
    // different pods cannot receive each other's chat messages or notifications.
    builder.Services.AddSignalR().AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix =
            new StackExchange.Redis.RedisChannel("Landlander:", StackExchange.Redis.RedisChannel.PatternMode.Literal);
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
    // No Redis — single-instance mode (development / local). SignalR runs in-memory only.
    builder.Services.AddSignalR();
}

builder.Services.AddSingleton<IdempotencyService>();
builder.Services.AddOutputCache(options =>
{
    // NOTE: no blanket AddBasePolicy(b => b.Cache()) — that cached EVERY anonymous GET
    // for 60s across the whole API, so deletes/creates looked like they "didn't happen"
    // (stale lists after logout/refresh). Only explicitly tagged endpoints are cached,
    // and their tags are evicted on mutation.
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

builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);
builder.Services.AddJwtAuthentication(builder.Configuration);

// ── Data Protection key persistence (#4) ─────────────────────────────────────
// Without this, each redeploy / new pod generates new keys → antiforgery tokens
// and SignalR auth cookies become invalid (users get logged out on every deploy).
// Keys are stored in a private blob container; no public access.
{
    var dp = builder.Services.AddDataProtection()
        .SetApplicationName("Landlander");

    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        // When Azure Blob connection string is available, persist keys there.
        var dpBlobConn = builder.Configuration["AzureBlobStorage:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(dpBlobConn))
        {
            var blobClient = new Azure.Storage.Blobs.BlobClient(
                dpBlobConn, "data-protection", "keys.xml");
            dp.PersistKeysToAzureBlobStorage(blobClient);
        }
    }
    // In dev / single-instance without Azure, keys persist to the default local filesystem.
}

// ─── Hangfire ─────────────────────────────────────────────────────────────────
// Uses the same SQL Server DB as the app (separate HangFire schema).
// Nightly jobs (listing expiration, email log cleanup, premium expiration, price
// model training) run as recurring jobs instead of long-running IHostedService loops.
var hangfireConnStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConnStr, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout     = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval          = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks           = true
    }));
builder.Services.AddHangfireServer(opts =>
{
    opts.WorkerCount = 2; // small worker pool — jobs are low-frequency nightly tasks
});

var hcBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<Lander.ListingsContext>("db-listings")
    .AddDbContextCheck<Lander.UsersContext>("db-users");

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    hcBuilder.AddRedis(redisConnectionString, name: "redis");
}

var app = builder.Build();

// ─── Forwarded-headers (reverse proxy / load balancer) ───────────────────────
// Must run before UseAuthentication / rate-limiting so that RemoteIpAddress
// and the HTTPS scheme reflect the real client — not the proxy's internal IP.
//
// Behind a real load balancer the proxy IP is NOT loopback, so the default
// KnownProxies/KnownNetworks (loopback only) cause X-Forwarded-For to be ignored —
// every request then shares the proxy's IP and per-IP rate limits misfire (false 429s
// + weakened brute-force protection). Configure the actual proxy IPs / CIDR ranges via
// ForwardedHeaders:KnownProxies and ForwardedHeaders:KnownNetworks at deploy time.
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

var knownProxies = app.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
var knownNetworks = app.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];

if (knownProxies.Length > 0 || knownNetworks.Length > 0)
{
    // Trust ONLY the configured proxies/networks — clear the loopback defaults so a
    // client can't spoof X-Forwarded-For by connecting from an untrusted address.
    forwardedOptions.KnownProxies.Clear();
    forwardedOptions.KnownIPNetworks.Clear();

    foreach (var proxy in knownProxies)
        if (System.Net.IPAddress.TryParse(proxy, out var ip))
            forwardedOptions.KnownProxies.Add(ip);

    foreach (var network in knownNetworks)
        if (System.Net.IPNetwork.TryParse(network, out var net))
            forwardedOptions.KnownIPNetworks.Add(net);

    // Allow multiple proxy hops only when proxies are explicitly trusted.
    var forwardLimit = app.Configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit");
    if (forwardLimit.HasValue) forwardedOptions.ForwardLimit = forwardLimit.Value;
}

app.UseForwardedHeaders(forwardedOptions);

// Global exception handler - must be first in the pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseResponseCompression();
app.UseHttpMetrics(); // prometheus-net: captures HTTP request duration / status code metrics

// CORS must run BEFORE OutputCache and the rate limiter. Otherwise a CORS preflight
// (OPTIONS request) can be short-circuited by the rate limiter with a 429 that carries
// no Access-Control-* headers — which the browser then reports as an opaque "CORS error"
// instead of the real status. Running CORS first also guarantees that 429 / cached
// responses still include the CORS headers so the frontend can read them.
app.UseCors("AllowFrontend");

app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0}ms)";
    opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
    {
        diagCtx.Set("ClientIP", httpCtx.Connection.RemoteIpAddress?.ToString());
        diagCtx.Set("UserAgent", httpCtx.Request.Headers.UserAgent.ToString());
    };
});

app.UseOutputCache();

// ─── Security headers ────────────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    // Hangfire dashboard requires inline scripts/styles — skip strict headers for it.
    if (context.Request.Path.StartsWithSegments("/hangfire"))
    {
        await next();
        return;
    }

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

        // img-src: when an Azure Blob CDN base URL is configured, restrict to that
        // origin instead of the open "https:" wildcard.
        var blobPublicBase = app.Configuration["AzureBlobStorage:PublicBaseUrl"];
        var imgSrcHosts = string.IsNullOrWhiteSpace(blobPublicBase)
            ? "https:"
            : $"{new Uri(blobPublicBase).GetLeftPart(UriPartial.Authority)}";

        headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self'; " +
            $"img-src 'self' data: blob: {imgSrcHosts}; " +
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

app.UseRequestTimeouts();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Rate limiter AFTER authentication so per-user policies (change-password, messages-send,
// create-review) partition by the userId claim with IP fallback — before auth the claim
// isn't populated yet, so every authenticated user on one IP shared a single bucket and
// tripped 429 almost immediately.
if (!app.Environment.IsEnvironment("E2eTesting"))
    app.UseRateLimiter();

// ─── Hangfire dashboard + recurring jobs ──────────────────────────────────────
// Dashboard restricted to Admin role.  The Hangfire UI uses inline scripts/styles
// so it is excluded from the strict CSP applied to the rest of the app.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthorizationFilter()]
});

RecurringJob.AddOrUpdate<Lander.src.Modules.Payments.Services.PremiumExpirationService>(
    "premium-expiration",
    j => j.RunAsync(),
    "0 1 * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

RecurringJob.AddOrUpdate<Lander.src.Modules.Listings.Services.ListingExpirationService>(
    "listing-expiration",
    j => j.RunAsync(),
    "0 2 * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

RecurringJob.AddOrUpdate<Lander.src.Modules.Communication.Services.EmailLogCleanupService>(
    "email-log-cleanup",
    j => j.RunAsync(),
    "0 3 * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

RecurringJob.AddOrUpdate<Lander.src.Modules.MachineLearning.Services.PriceModelTrainingService>(
    "price-model-training",
    j => j.RunAsync(),
    "0 4 * * 0",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

app.MapGrpcService<ReviewFavoriteService>();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub").RequireRateLimiting("signalr");
app.MapHub<ChatHub>("/chatHub").RequireRateLimiting("signalr");
app.MapHealthChecks("/health");
// Prometheus scrape endpoint — restrict to internal network at the reverse-proxy level in prod.
app.MapMetrics("/metrics");

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
