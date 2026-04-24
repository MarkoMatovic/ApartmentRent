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

    // ─── PredictPrice — full request & response fields ────────────────────────

    [Fact]
    public async Task PredictPrice_FullRequest_ReturnsAllResponseFields()
    {
        var request = new PricePredictionRequestDto
        {
            SizeSquareMeters = 75,
            NumberOfRooms = 3,
            City = "Sarajevo",
            IsFurnished = true,
            HasBalcony = true,
            HasParking = false,
            HasElevator = true,
            HasAirCondition = true,
            HasInternet = true,
            IsPetFriendly = false,
            IsSmokingAllowed = false,
            ApartmentType = 1
        };
        var response = new PricePredictionResponseDto
        {
            PredictedPrice = 750m,
            ConfidenceScore = 92m,
            Message = "High confidence prediction"
        };
        _mockPricePrediction.Setup(s => s.PredictPriceAsync(request))
            .ReturnsAsync(response);

        var result = await _controller.PredictPrice(request);

        var value = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PricePredictionResponseDto>().Subject;

        value.PredictedPrice.Should().Be(750m);
        value.ConfidenceScore.Should().BeInRange(0m, 100m);
        value.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PredictPrice_MessageInResponseOnSuccess_IsNotEmpty()
    {
        var response = new PricePredictionResponseDto
        {
            PredictedPrice = 400m,
            ConfidenceScore = 70m,
            Message = "Prediction based on 200 similar apartments"
        };
        _mockPricePrediction.Setup(s => s.PredictPriceAsync(It.IsAny<PricePredictionRequestDto>()))
            .ReturnsAsync(response);

        var result = await _controller.PredictPrice(new PricePredictionRequestDto { City = "Mostar" });

        var value = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PricePredictionResponseDto>().Subject;
        value.Message.Should().NotBeNullOrEmpty();
    }

    // ─── TrainModel — verifies metrics values ─────────────────────────────────

    [Fact]
    public async Task TrainModel_ReturnsAllMetricsFields()
    {
        var metrics = new ModelMetricsDto
        {
            RSquared = 0.91,
            MeanAbsoluteError = 25.5,
            MeanSquaredError = 1050.2,
            RootMeanSquaredError = 32.4,
            TrainingSampleCount = 1200,
            LastTrainedDate = DateTime.UtcNow
        };
        _mockPricePrediction.Setup(s => s.TrainModelAsync())
            .ReturnsAsync(metrics);

        var result = await _controller.TrainModel();

        var value = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<ModelMetricsDto>().Subject;

        value.RSquared.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(1);
        value.TrainingSampleCount.Should().BePositive();
        value.LastTrainedDate.Should().NotBeNull();
        value.MeanAbsoluteError.Should().BeGreaterThanOrEqualTo(0);
        value.RootMeanSquaredError.Should().BeGreaterThanOrEqualTo(0);
    }

    // ─── GetModelMetrics — all fields present ─────────────────────────────────

    [Fact]
    public async Task GetModelMetrics_AllFieldsPresent_ReturnsCorrectly()
    {
        var metrics = new ModelMetricsDto
        {
            RSquared = 0.88,
            MeanAbsoluteError = 30.0,
            MeanSquaredError = 1500.0,
            RootMeanSquaredError = 38.7,
            TrainingSampleCount = 800,
            LastTrainedDate = new DateTime(2025, 3, 15)
        };
        _mockPricePrediction.Setup(s => s.GetModelMetricsAsync())
            .ReturnsAsync(metrics);

        var result = await _controller.GetModelMetrics();

        var value = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<ModelMetricsDto>().Subject;

        value.RSquared.Should().Be(0.88);
        value.MeanAbsoluteError.Should().Be(30.0);
        value.TrainingSampleCount.Should().Be(800);
        value.LastTrainedDate.Should().Be(new DateTime(2025, 3, 15));
    }

    // ─── GetRoommateMatches — custom topN & feature scores ───────────────────

    [Fact]
    public async Task GetRoommateMatches_CustomTopN_PassesTopNToService()
    {
        _mockRoommateMatching.Setup(s => s.GetMatchesForUserAsync(CurrentUserId, 5))
            .ReturnsAsync(new List<RoommateMatchScoreDto>());

        var result = await _controller.GetRoommateMatches(CurrentUserId, 5);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mockRoommateMatching.Verify(s => s.GetMatchesForUserAsync(CurrentUserId, 5), Times.Once);
    }

    [Fact]
    public async Task GetRoommateMatches_WithFeatureScores_ReturnsFeatureBreakdown()
    {
        var matches = new List<RoommateMatchScoreDto>
        {
            new()
            {
                RoommateId = 3,
                MatchPercentage = 91.5f,
                MatchQuality = "Excellent",
                FeatureScores = new Dictionary<string, float>
                {
                    { "budget", 0.95f },
                    { "city", 1.0f },
                    { "lifestyle", 0.85f }
                }
            }
        };
        _mockRoommateMatching.Setup(s => s.GetMatchesForUserAsync(CurrentUserId, 10))
            .ReturnsAsync(matches);

        var result = await _controller.GetRoommateMatches(CurrentUserId, 10);

        var value = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<List<RoommateMatchScoreDto>>().Subject;

        value.Should().HaveCount(1);
        value[0].FeatureScores.Should().ContainKey("budget").And.ContainKey("city");
        value[0].MatchPercentage.Should().BeInRange(0f, 100f);
        value[0].MatchQuality.Should().Be("Excellent");
    }

    // ─── CalculateMatchScore — zero score & score range ──────────────────────

    [Fact]
    public async Task CalculateMatchScore_ZeroScore_ReturnsOkWithZero()
    {
        _mockRoommateMatching.Setup(s => s.CalculateMatchScoreAsync(1, 2))
            .ReturnsAsync(0.0f);

        var result = await _controller.CalculateMatchScore(1, 2);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { matchScore = 0.0f });
    }

    [Fact]
    public async Task CalculateMatchScore_ScoreInValidRange_ReturnsOk()
    {
        _mockRoommateMatching.Setup(s => s.CalculateMatchScoreAsync(1, 2))
            .ReturnsAsync(0.67f);

        var result = await _controller.CalculateMatchScore(1, 2);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var score = ok.Value.Should().BeEquivalentTo(new { matchScore = 0.67f }).And.Subject;
        // matchScore field name is verified above; score value is [0,1]
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
