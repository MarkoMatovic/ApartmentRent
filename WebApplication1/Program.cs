using System;
using System.Runtime.CompilerServices;
using System.Text;
using Lander;
using Lander.Helpers;
using Lander.src.Modules.Listings.Implementation;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Reviews.Implementation;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.src.Notifications.Implementation;
using Lander.src.Notifications.Interfaces;
using Lander.src.Notifications.NotificationsHub;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<UsersContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ListingsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<NotificationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<ReviewsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddGrpc();

builder.Services.AddScoped<IUserInterface, UserService>();
builder.Services.AddScoped<IApartmentService, ApartmentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddSingleton<TokenProvider>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = true;
        o.TokenValidationParameters = new TokenValidationParameters
        {        
            ValidIssuer = builder.Configuration["Jwt:Issuers"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ClockSkew = TimeSpan.Zero
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
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapGrpcService<ReviewFavoriteService>();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");
app.UseAuthentication();
app.UseAuthorization();

app.Run();
