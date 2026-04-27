using Lander.src.Modules.MachineLearning.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lander.src.Modules.MachineLearning.Services;

public class PriceModelTrainingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PriceModelTrainingService> _logger;
    private static readonly TimeSpan TrainingInterval = TimeSpan.FromDays(7);

    public PriceModelTrainingService(IServiceScopeFactory scopeFactory, ILogger<PriceModelTrainingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Train on startup if model doesn't exist yet
        await TrainAsync(stoppingToken);

        using var timer = new PeriodicTimer(TrainingInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TrainAsync(stoppingToken);
        }
    }

    private async Task TrainAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IPricePredictionService>();
            _logger.LogInformation("Price model training started.");
            var metrics = await service.TrainModelAsync();
            _logger.LogInformation("Price model training completed. R²={R2:F4}", metrics.RSquared);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Price model training failed.");
        }
    }
}
