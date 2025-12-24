using System;
using System.Runtime.CompilerServices;
using System.Text;
using Lander;
using Lander.Helpers;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Modules.Communication.Implementation;
using Lander.src.Modules.Communication.Intefaces;
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
using Lander.src.Notifications.Implementation;
using Lander.src.Notifications.Interfaces;
using Lander.src.Notifications.NotificationsHub;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


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

// CORS configuration for frontend access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", 
                "http://127.0.0.1:5173",
                "https://localhost:5173"  // Dodaj HTTPS origin ako frontend koristi HTTPS
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

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Cache());
    options.AddPolicy("ApartmentsList", builder => 
        builder.Expire(TimeSpan.FromMinutes(5))
               .SetVaryByQuery(new[] { "City", "MinRent", "MaxRent", "Page", "PageSize", 
                                       "NumberOfRooms", "ApartmentType", "IsFurnished", 
                                       "IsPetFriendly", "IsSmokingAllowed", "HasParking", 
                                       "HasBalcony", "IsImmediatelyAvailable" })
               .Tag("apartments"));
    options.AddPolicy("ApartmentDetail", builder => 
        builder.Expire(TimeSpan.FromMinutes(10))
               .SetVaryByRouteValue(new[] { "id" })
               .Tag("apartments"));
});

builder.Services.AddScoped<IUserInterface, UserService>();
builder.Services.AddScoped<IApartmentService, ApartmentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRoommateService, RoommateService>();
builder.Services.AddScoped<ISearchRequestService, SearchRequestService>();
builder.Services.AddScoped<ISavedSearchService, SavedSearchService>();
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("Twilio"));
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddSingleton<TokenProvider>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = true;  // OK za HTTPS mode
        o.TokenValidationParameters = new TokenValidationParameters
        {        
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("LandlordPolicy", policy => policy.RequireRole("Landlord"));
    options.AddPolicy("TenantPolicy", policy => policy.RequireRole("Tenant"));
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("BrokerPolicy", policy => policy.RequireRole("Broker"));
    options.AddPolicy("GuestPolicy", policy => policy.RequireRole("Guest"));
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    // Dodavanje Bearer autentifikacije u Swagger
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.UseOutputCache();

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ReviewFavoriteService>();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ChatHub>("/chatHub");

app.Run();
