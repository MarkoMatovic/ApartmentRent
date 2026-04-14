using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lander.src.Modules.MachineLearning.Controllers;
using Lander.src.Modules.MachineLearning.Dtos;
using Lander.src.Modules.MachineLearning.Interfaces;

namespace LandlordApp.Tests.Controllers;

public class MachineLearningControllerTests
{
    private readonly Mock<IPricePredictionService> _mockPricePrediction;
    private readonly Mock<IRoommateMatchingService> _mockRoommateMatching;
    private readonly MachineLearningController _controller;
    private const int CurrentUserId = 1;

    public MachineLearningControllerTests()
    {
        _mockPricePrediction = new Mock<IPricePredictionService>();
        _mockRoommateMatching = new Mock<IRoommateMatchingService>();

        _controller = new MachineLearningController(
            _mockPricePrediction.Object,
            _mockRoommateMatching.Object);
        _controller.ControllerContext = MakeAuthContext(CurrentUserId);
    }

    // ─── PredictPrice ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PredictPrice_ValidRequest_ReturnsOkWithPrediction()
    {
        var request = new PricePredictionRequestDto
        {
            SizeSquareMeters = 60,
            NumberOfRooms = 2,
            City = "Beograd"
        };
        var response = new PricePredictionResponseDto
        {
            PredictedPrice = 500m,
            ConfidenceScore = 85m,
            Message = "Prediction successful"
        };
        _mockPricePrediction.Setup(s => s.PredictPriceAsync(request))
            .ReturnsAsync(response);

        var result = await _controller.PredictPrice(request);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task PredictPrice_ServiceThrows_ReturnsBadRequest()
    {
        var request = new PricePredictionRequestDto { City = "Beograd" };
        _mockPricePrediction.Setup(s => s.PredictPriceAsync(It.IsAny<PricePredictionRequestDto>()))
            .ThrowsAsync(new Exception("Model not trained"));

        var result = await _controller.PredictPrice(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PredictPrice_ModelNotTrained_ReturnsBadRequest()
    {
        var request = new PricePredictionRequestDto { NumberOfRooms = 1 };
        _mockPricePrediction.Setup(s => s.PredictPriceAsync(It.IsAny<PricePredictionRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("Model is not trained yet"));

        var result = await _controller.PredictPrice(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── TrainModel ───────────────────────────────────────────────────────────

    [Fact]
    public async Task TrainModel_Success_ReturnsOkWithMetrics()
    {
        var metrics = new ModelMetricsDto
        {
            RSquared = 0.95,
            MeanAbsoluteError = 20.0,
            TrainingSampleCount = 500
        };
        _mockPricePrediction.Setup(s => s.TrainModelAsync())
            .ReturnsAsync(metrics);

        var result = await _controller.TrainModel();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(metrics);
    }

    [Fact]
    public async Task TrainModel_ServiceThrows_ReturnsBadRequest()
    {
        _mockPricePrediction.Setup(s => s.TrainModelAsync())
            .ThrowsAsync(new Exception("Insufficient training data"));

        var result = await _controller.TrainModel();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── GetModelMetrics ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetModelMetrics_ReturnsOkWithMetrics()
    {
        var metrics = new ModelMetricsDto
        {
            RSquared = 0.92,
            RootMeanSquaredError = 35.5,
            LastTrainedDate = new DateTime(2025, 1, 10)
        };
        _mockPricePrediction.Setup(s => s.GetModelMetricsAsync())
            .ReturnsAsync(metrics);

        var result = await _controller.GetModelMetrics();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(metrics);
    }

    [Fact]
    public async Task GetModelMetrics_ServiceThrows_PropagatesException()
    {
        _mockPricePrediction.Setup(s => s.GetModelMetricsAsync())
            .ThrowsAsync(new Exception("Metrics unavailable"));

        Func<Task> act = async () => await _controller.GetModelMetrics();

        await act.Should().ThrowAsync<Exception>().WithMessage("Metrics unavailable");
    }

    // ─── IsModelTrained ───────────────────────────────────────────────────────

    [Fact]
    public void IsModelTrained_WhenTrained_ReturnsOkWithTrue()
    {
        _mockPricePrediction.Setup(s => s.IsModelTrained()).Returns(true);

        var result = _controller.IsModelTrained();

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.Value.Should().BeEquivalentTo(new { isTrained = true });
    }

    [Fact]
    public void IsModelTrained_WhenNotTrained_ReturnsOkWithFalse()
    {
        _mockPricePrediction.Setup(s => s.IsModelTrained()).Returns(false);

        var result = _controller.IsModelTrained();

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.Value.Should().BeEquivalentTo(new { isTrained = false });
    }

    // ─── GetRoommateMatches ───────────────────────────────────────────────────

    [Fact]
    public async Task GetRoommateMatches_ValidRequest_ReturnsOkWithMatches()
    {
        var matches = new List<RoommateMatchScoreDto>
        {
            new() { RoommateId = 5, MatchPercentage = 88.5f, MatchQuality = "Excellent" },
            new() { RoommateId = 7, MatchPercentage = 72.0f, MatchQuality = "Good" }
        };
        _mockRoommateMatching.Setup(s => s.GetMatchesForUserAsync(CurrentUserId, 10))
            .ReturnsAsync(matches);

        var result = await _controller.GetRoommateMatches(CurrentUserId, 10);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(matches);
    }

    [Fact]
    public async Task GetRoommateMatches_EmptyResult_ReturnsOkWithEmptyList()
    {
        _mockRoommateMatching.Setup(s => s.GetMatchesForUserAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<RoommateMatchScoreDto>());

        var result = await _controller.GetRoommateMatches(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<RoommateMatchScoreDto>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoommateMatches_ServiceThrows_ReturnsBadRequest()
    {
        _mockRoommateMatching.Setup(s => s.GetMatchesForUserAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Matching service error"));

        var result = await _controller.GetRoommateMatches(CurrentUserId, 10);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── CalculateMatchScore ──────────────────────────────────────────────────

    [Fact]
    public async Task CalculateMatchScore_ValidUsers_ReturnsOkWithScore()
    {
        _mockRoommateMatching.Setup(s => s.CalculateMatchScoreAsync(1, 2))
            .ReturnsAsync(0.85f);

        var result = await _controller.CalculateMatchScore(1, 2);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.Value.Should().BeEquivalentTo(new { matchScore = 0.85f });
    }

    [Fact]
    public async Task CalculateMatchScore_ServiceThrows_ReturnsBadRequest()
    {
        _mockRoommateMatching.Setup(s => s.CalculateMatchScoreAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("User not found"));

        var result = await _controller.CalculateMatchScore(1, 999);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CalculateMatchScore_SameUsers_ReturnsOkWithPerfectScore()
    {
        _mockRoommateMatching.Setup(s => s.CalculateMatchScoreAsync(1, 1))
            .ReturnsAsync(1.0f);

        var result = await _controller.CalculateMatchScore(1, 1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ControllerContext MakeAuthContext(int userId = 1, Guid? userGuid = null)
    {
        userGuid ??= Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new("sub", userGuid.ToString())
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        return new ControllerContext { HttpContext = httpContext };
    }
}
