using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Interfaces;

namespace Lander.src.Modules.Listings.Services;

/// <summary>
/// Pre-populates the apartment cache on app startup so the first real user request
/// is served from cache instead of hitting the database cold.
/// </summary>
public class ApartmentCacheWarmupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApartmentCacheWarmupService> _logger;

    public ApartmentCacheWarmupService(IServiceProvider serviceProvider, ILogger<ApartmentCacheWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Apartment cache warmup starting...");
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var apartmentService = scope.ServiceProvider.GetRequiredService<IApartmentService>();

            // Warm up the most common default views users land on.
            var defaultFilters = new[]
            {
                new ApartmentFilterDto { Page = 1, PageSize = 20 },
                new ApartmentFilterDto { Page = 1, PageSize = 20, SortBy = "date",  SortOrder = "desc" },
                new ApartmentFilterDto { Page = 1, PageSize = 20, SortBy = "price", SortOrder = "asc"  },
            };

            foreach (var filter in defaultFilters)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await apartmentService.GetAllApartmentsAsync(filter);
            }

            _logger.LogInformation("Apartment cache warmup complete ({Count} queries pre-loaded)", defaultFilters.Length);
        }
        catch (Exception ex)
        {
            // Warmup failure must never prevent the app from starting.
            _logger.LogWarning(ex, "Apartment cache warmup failed — first request will hit the database");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
