using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Modules.MachineLearning.Dtos;
using Lander.src.Modules.MachineLearning.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.MachineLearning.Controllers;

[Route(ApiActionsV1.MachineLearning)]
[ApiController]
public class MachineLearningController : ApiControllerBase
{
    private readonly IPricePredictionService _pricePredictionService;
    private readonly IRoommateMatchingService _roommateMatchingService;

    public MachineLearningController(
        IPricePredictionService pricePredictionService,
        IRoommateMatchingService roommateMatchingService,
        IUserInterface userService) : base(userService)
    {
        _pricePredictionService = pricePredictionService;
        _roommateMatchingService = roommateMatchingService;
    }

    [HttpPost(ApiActionsV1.PredictPrice, Name = nameof(ApiActionsV1.PredictPrice))]
    [AllowAnonymous]
    public async Task<ActionResult<PricePredictionResponseDto>> PredictPrice([FromBody] PricePredictionRequestDto request)
    {
        var prediction = await _pricePredictionService.PredictPriceAsync(request);
        return Ok(prediction);
    }

    [HttpPost(ApiActionsV1.TrainPriceModel, Name = nameof(ApiActionsV1.TrainPriceModel))]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<ModelMetricsDto>> TrainModel()
    {
        var metrics = await _pricePredictionService.TrainModelAsync();
        return Ok(metrics);
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
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();
        if (callerId.Value != userId && !User.IsInRole("Admin")) return Forbid();
        var matches = await _roommateMatchingService.GetMatchesForUserAsync(userId, topN);
        return Ok(matches);
    }

    [HttpGet(ApiActionsV1.CalculateMatchScore, Name = nameof(ApiActionsV1.CalculateMatchScore))]
    [Authorize]
    public async Task<ActionResult<float>> CalculateMatchScore(
        [FromQuery] int userId1,
        [FromQuery] int userId2)
    {
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();
        if (callerId.Value != userId1 && callerId.Value != userId2 && !User.IsInRole("Admin")) return Forbid();
        var score = await _roommateMatchingService.CalculateMatchScoreAsync(userId1, userId2);
        return Ok(new { matchScore = score });
    }
}
