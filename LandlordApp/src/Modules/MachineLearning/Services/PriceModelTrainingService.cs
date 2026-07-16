using Lander.src.Modules.MachineLearning.Interfaces;

namespace Lander.src.Modules.MachineLearning.Services;

/// <summary>
/// Hangfire recurring job (weekly, Sunday 04:00 UTC) that retrains the price prediction model
/// on the latest listing data.
/// </summary>
public sealed class PriceModelTrainingService
{
    private readonly IPricePredictionService _predictionService;
    private readonly ILogger<PriceModelTrainingService> _logger;

    public PriceModelTrainingService(
        IPricePredictionService predictionService,
        ILogger<PriceModelTrainingService> logger)
    {
        _predictionService = predictionService;
        _logger            = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Price model training started.");
        var metrics = await _predictionService.TrainModelAsync();
        _logger.LogInformation("Price model training completed. R²={R2:F4}", metrics.RSquared);
    }
}
