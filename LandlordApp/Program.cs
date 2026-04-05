using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Events;
using FluentValidation;
using FluentValidation.AspNetCore;
using Lander;
using Lander.Helpers;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Modules.Communication.Implementation;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Listings.Implementation;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Reviews.Implementation;
using Lander.src.Modules.Roommates.Implementation;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.SearchRequests.Implementation;
using Lander.src.Modules.SearchRequests.Interfaces;
using Lander.src.Modules.SavedSearches.Implementation;
using Lander.src.Modules.SavedSearches.Interfaces;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.src.Modules.Users.Domain.IRepository;
using Lander.src.Modules.Users.Infrastructure.Repository;
using Lander.src.Modules.Users.Domain.IService;
using Lander.src.Modules.Users.Implementation.PermissionImplementation;
using Lander.src.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Lander.src.Notifications.Implementation;
using Lander.src.Notifications.Interfaces;
using Lander.src.Notifications.NotificationsHub;
using Lander.src.Notifications.Services; // .NET 10: SSE Support
using Lander.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// Bootstrap logger — hvata greske pre ucitavanja konfiguracije
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

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


builder.Services.AddDbContext<UsersContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ApplicationsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ListingsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<NotificationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<ReviewsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<CommunicationsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<RoommatesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<SearchRequestsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<SavedSearchesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<AnalyticsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<Lander.src.Modules.Appointments.AppointmentsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<Lander.src.Modules.Payments.PaymentsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", 
                "http://127.0.0.1:5173",
                "https://localhost:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

builder.Services.AddRateLimiter(options =>
{
    // Strogi limit za auth endpointe (login, register, change-password)
    options.AddFixedWindowLimiter("auth", policy =>
    {
        policy.PermitLimit = 5;
        policy.Window = TimeSpan.FromSeconds(30);
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 0;
    });

    // Globalni limit za sve ostale API pozive
    options.AddFixedWindowLimiter("global", policy =>
    {
        policy.PermitLimit = 100;
        policy.Window = TimeSpan.FromMinutes(1);
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 5;
    });

    options.RejectionStatusCode = 429;
});

builder.Services.AddMemoryCache();
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

builder.Services.AddScoped<IUserInterface, UserService>();

// RBAC: Permission Repository and Service
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

builder.Services.AddScoped<IApartmentService, ApartmentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRoommateService, RoommateService>();
builder.Services.AddScoped<ISearchRequestService, SearchRequestService>();
builder.Services.AddScoped<ISavedSearchService, SavedSearchService>();
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("Twilio"));
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.Configure<BrevoSettings>(builder.Configuration.GetSection("Brevo"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<Lander.src.Modules.Analytics.Interfaces.IAnalyticsService, Lander.src.Modules.Analytics.Implementation.AnalyticsService>();
builder.Services.AddScoped<Lander.src.Modules.MachineLearning.Interfaces.IPricePredictionService, Lander.src.Modules.MachineLearning.Implementation.PricePredictionService>();
builder.Services.AddScoped<Lander.src.Modules.MachineLearning.Interfaces.IRoommateMatchingService, Lander.src.Modules.MachineLearning.Implementation.RoommateMatchingService>();

// .NET 10 Feature: Server-Sent Events for real-time notifications
builder.Services.AddSingleton<NotificationStreamService>();

builder.Services.AddScoped<Lander.src.Modules.ApartmentApplications.Interfaces.IApartmentApplicationService, Lander.src.Modules.ApartmentApplications.Implementation.ApartmentApplicationService>();
builder.Services.AddScoped<Lander.src.Modules.ApartmentApplications.Interfaces.IApplicationApprovalService, Lander.src.Modules.ApartmentApplications.Implementation.ApplicationApprovalService>();

// Payment Integration (Monri)
builder.Services.AddScoped<Lander.src.Modules.Payments.Interfaces.IMonriService, Lander.src.Modules.Payments.Implementation.MonriService>();

// .NET 10 Feature: Vector Search for semantic apartment search
builder.Services.AddSingleton<Lander.src.Modules.MachineLearning.Services.SimpleEmbeddingService>();

// Appointment Booking System
builder.Services.AddScoped<Lander.src.Modules.Appointments.Interfaces.IAppointmentService, Lander.src.Modules.Appointments.Implementation.AppointmentService>();

// Payments
builder.Services.AddScoped<Lander.src.Modules.Payments.Interfaces.IPaymentService, Lander.src.Modules.Payments.Implementation.PaytenPaymentService>();

builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddHttpContextAccessor();

// RBAC: Authorization Infrastructure
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("LandlordPolicy",  policy => policy.RequireRole(RoleConstants.Landlord));
    options.AddPolicy("TenantPolicy",    policy => policy.RequireRole(RoleConstants.Tenant));
    options.AddPolicy("AdminPolicy",     policy => policy.RequireRole(RoleConstants.Admin));
    options.AddPolicy("BrokerPolicy",    policy => policy.RequireRole(RoleConstants.Broker));
    options.AddPolicy("GuestPolicy",     policy => policy.RequireRole(RoleConstants.Guest));
    options.AddPolicy("PremiumPolicy",   policy => policy.RequireRole(RoleConstants.PremiumTenant, RoleConstants.PremiumLandlord));
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field (e.g. 'Bearer {token}')",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
var app = builder.Build();

// Global exception handler - must be first in the pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

app.UseStaticFiles(); // Enable static file serving for uploaded images

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
            "style-src 'self' 'unsafe-inline'; " +
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

app.Run();
