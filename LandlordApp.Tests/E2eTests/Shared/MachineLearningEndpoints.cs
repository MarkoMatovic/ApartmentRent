using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>GET /api/v1/ml/is-model-trained (no auth)</summary>
public class IsModelTrainedEndpoint
{
    private readonly HttpClient _client;
    public IsModelTrainedEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/v1/ml/is-model-trained");
}

/// <summary>POST /api/v1/ml/predict-price [Authorize]</summary>
public class PredictPriceEndpoint
{
    private readonly HttpClient _client;
    public PredictPriceEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(object? dto = null)
        => _client.PostAsJsonAsync("/api/v1/ml/predict-price", dto ?? new
        {
            SizeSquareMeters = 60,
            NumberOfRooms    = 2,
            IsFurnished      = true,
            City             = "Sarajevo",
        });
}

/// <summary>POST /api/v1/ml/train-price-model [AdminPolicy]</summary>
public class TrainModelEndpoint
{
    private readonly HttpClient _client;
    public TrainModelEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.PostAsJsonAsync("/api/v1/ml/train-price-model", new { });
}

/// <summary>GET /api/v1/ml/model-metrics [AdminPolicy]</summary>
public class GetModelMetricsEndpoint
{
    private readonly HttpClient _client;
    public GetModelMetricsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/v1/ml/model-metrics");
}

/// <summary>GET /api/v1/ml/roommate-matches?userId={userId}</summary>
public class GetRoommateMatchesEndpoint
{
    private readonly HttpClient _client;
    public GetRoommateMatchesEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/ml/roommate-matches?userId={userId}");
}

/// <summary>GET /api/v1/ml/match-score?userId1={u1}&userId2={u2}</summary>
public class CalculateMatchScoreEndpoint
{
    private readonly HttpClient _client;
    public CalculateMatchScoreEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId1, int userId2)
        => _client.GetAsync($"/api/v1/ml/match-score?userId1={userId1}&userId2={userId2}");
}
