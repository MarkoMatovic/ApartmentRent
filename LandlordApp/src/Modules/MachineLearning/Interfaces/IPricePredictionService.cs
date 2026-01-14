using Lander.src.Modules.MachineLearning.Dtos;

namespace Lander.src.Modules.MachineLearning.Interfaces;

public interface IPricePredictionService
{
    /// <summary>
    /// Predict the rental price for an apartment based on its features
    /// </summary>
    Task<PricePredictionResponseDto> PredictPriceAsync(PricePredictionRequestDto request);
    
    /// <summary>
    /// Train or retrain the price prediction model using current apartment data
    /// </summary>
    Task<ModelMetricsDto> TrainModelAsync();
    
    /// <summary>
    /// Get current model performance metrics
    /// </summary>
    Task<ModelMetricsDto> GetModelMetricsAsync();
    
    /// <summary>
    /// Check if a trained model exists
    /// </summary>
    bool IsModelTrained();
}
