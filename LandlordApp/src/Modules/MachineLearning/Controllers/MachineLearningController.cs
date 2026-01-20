using Lander.Helpers;
using Lander.src.Modules.MachineLearning.Dtos;
using Lander.src.Modules.MachineLearning.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Lander.src.Modules.MachineLearning.Controllers;
[Route(ApiActionsV1.MachineLearning)]
[ApiController]
public class MachineLearningController : ControllerBase
{
    private readonly IPricePredictionService _pricePredictionService;
    private readonly IRoommateMatchingService _roommateMatchingService;
    public MachineLearningController(IPricePredictionService pricePredictionService, IRoommateMatchingService roommateMatchingService)
    {
        _pricePredictionService = pricePredictionService;
        _roommateMatchingService = roommateMatchingService;
    }
    [HttpPost(ApiActionsV1.PredictPrice, Name = nameof(ApiActionsV1.PredictPrice))]
    [AllowAnonymous]
    public async Task<ActionResult<PricePredictionResponseDto>> PredictPrice([FromBody] PricePredictionRequestDto request)
    {
        try
        {
            var prediction = await _pricePredictionService.PredictPriceAsync(request);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPost(ApiActionsV1.TrainPriceModel, Name = nameof(ApiActionsV1.TrainPriceModel))]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<ModelMetricsDto>> TrainModel()
    {
        try
        {
            var metrics = await _pricePredictionService.TrainModelAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet(ApiActionsV1.GetModelMetrics, Name = nameof(ApiActionsV1.GetModelMetrics))]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<ModelMetricsDto>> GetModelMetrics()
    {
        var metrics = await _pricePredictionService.GetModelMetricsAsync();
        return Ok(metrics);
    }
    [HttpGet(ApiActionsV1.IsModelTrained, Name = nameof(ApiActionsV1.IsModelTrained))]
    public ActionResult<bool> IsModelTrained()
    {
        var isTrained = _pricePredictionService.IsModelTrained();
        return Ok(new { isTrained });
    }
    [HttpGet(ApiActionsV1.GetRoommateMatches, Name = nameof(ApiActionsV1.GetRoommateMatches))]
    [Authorize]
    public async Task<ActionResult<List<RoommateMatchScoreDto>>> GetRoommateMatches(
        [FromQuery] int userId,
        [FromQuery] int topN = 10)
    {
        try
        {
            var matches = await _roommateMatchingService.GetMatchesForUserAsync(userId, topN);
            return Ok(matches);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet(ApiActionsV1.CalculateMatchScore, Name = nameof(ApiActionsV1.CalculateMatchScore))]
    [Authorize]
    public async Task<ActionResult<float>> CalculateMatchScore(
        [FromQuery] int userId1,
        [FromQuery] int userId2)
    {
        try
        {
            var score = await _roommateMatchingService.CalculateMatchScoreAsync(userId1, userId2);
            return Ok(new { matchScore = score });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
