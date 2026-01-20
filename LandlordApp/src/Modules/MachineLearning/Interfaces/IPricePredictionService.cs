using Lander.src.Modules.MachineLearning.Dtos;
namespace Lander.src.Modules.MachineLearning.Interfaces;
public interface IPricePredictionService
{
    Task<PricePredictionResponseDto> PredictPriceAsync(PricePredictionRequestDto request);
    Task<ModelMetricsDto> TrainModelAsync();
    Task<ModelMetricsDto> GetModelMetricsAsync();
    bool IsModelTrained();
}
