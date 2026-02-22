using Lander.src.Modules.MachineLearning.Dtos;
using Lander.src.Modules.MachineLearning.Interfaces;
using Lander.src.Modules.MachineLearning.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
namespace Lander.src.Modules.MachineLearning.Implementation;
public class PricePredictionService : IPricePredictionService
{
    private readonly ListingsContext _listingsContext;
    private readonly IWebHostEnvironment _environment;
    private readonly string _modelPath;
    private readonly string _metricsPath;
    private MLContext _mlContext;
    private ITransformer? _trainedModel;
    // .NET 10 Feature: Modern Lock type for thread synchronization
    private static readonly Lock _lock = new();
    public PricePredictionService(ListingsContext listingsContext, IWebHostEnvironment environment)
    {
        _listingsContext = listingsContext;
        _environment = environment;
        _mlContext = new MLContext(seed: 0);
        var mlModelsDir = Path.Combine(_environment.ContentRootPath, "MLModels");
        if (!Directory.Exists(mlModelsDir))
            Directory.CreateDirectory(mlModelsDir);
        _modelPath = Path.Combine(mlModelsDir, "price-prediction.zip");
        _metricsPath = Path.Combine(mlModelsDir, "price-prediction-metrics.json");
        if (File.Exists(_modelPath))
        {
            _trainedModel = _mlContext.Model.Load(_modelPath, out var _);
        }
    }
    public bool IsModelTrained()
    {
        return _trainedModel != null || File.Exists(_modelPath);
    }
    public async Task<PricePredictionResponseDto> PredictPriceAsync(PricePredictionRequestDto request)
    {
        if (!IsModelTrained())
        {
            return new PricePredictionResponseDto
            {
                PredictedPrice = 0,
                ConfidenceScore = 0,
                Message = "Model not trained yet. Please train the model first or wait for sufficient data."
            };
        }
        if (_trainedModel == null)
        {
            using (_lock.EnterScope()) // .NET 10: Automatic lock release
            {
                if (_trainedModel == null)
                {
                    _trainedModel = _mlContext.Model.Load(_modelPath, out var _);
                }
            }
        }
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<ApartmentPriceData, ApartmentPricePrediction>(_trainedModel);
        var input = new ApartmentPriceData
        {
            SizeSquareMeters = request.SizeSquareMeters ?? 50,
            NumberOfRooms = request.NumberOfRooms ?? 1,
            IsFurnished = request.IsFurnished == true ? 1 : 0,
            HasBalcony = request.HasBalcony == true ? 1 : 0,
            HasParking = request.HasParking == true ? 1 : 0,
            HasElevator = request.HasElevator == true ? 1 : 0,
            HasAirCondition = request.HasAirCondition == true ? 1 : 0,
            HasInternet = request.HasInternet == true ? 1 : 0,
            IsPetFriendly = request.IsPetFriendly == true ? 1 : 0,
            IsSmokingAllowed = request.IsSmokingAllowed == true ? 1 : 0,
            CityEncoded = EncodeCitySimple(request.City),
            ApartmentTypeEncoded = request.ApartmentType ?? 0
        };
        var prediction = predictionEngine.Predict(input);
        var metrics = await GetModelMetricsAsync();
        var confidenceScore = Math.Max(0, Math.Min(100, metrics.RSquared * 100));
        var rawPrice = double.IsFinite(prediction.PredictedPrice) ? prediction.PredictedPrice : 0.0;
        var rawConfidence = double.IsFinite(confidenceScore) ? confidenceScore : 0.0;
        return new PricePredictionResponseDto
        {
            PredictedPrice = Math.Round((decimal)rawPrice, 2),
            ConfidenceScore = (decimal)rawConfidence,
            Message = "Price prediction successful"
        };
    }
    public async Task<ModelMetricsDto> TrainModelAsync()
    {
        var apartments = await _listingsContext.Apartments
            .Where(a => a.IsActive && !a.IsDeleted && a.Rent > 0 && a.SizeSquareMeters.HasValue)
            .Select(a => new ApartmentPriceData
            {
                SizeSquareMeters = a.SizeSquareMeters!.Value,
                NumberOfRooms = a.NumberOfRooms ?? 1,
                IsFurnished = a.IsFurnished ? 1 : 0,
                HasBalcony = a.HasBalcony ? 1 : 0,
                HasParking = a.HasParking ? 1 : 0,
                HasElevator = a.HasElevator ? 1 : 0,
                HasAirCondition = a.HasAirCondition ? 1 : 0,
                HasInternet = a.HasInternet ? 1 : 0,
                IsPetFriendly = a.IsPetFriendly ? 1 : 0,
                IsSmokingAllowed = a.IsSmokingAllowed ? 1 : 0,
                CityEncoded = EncodeCitySimple(a.City),
                ApartmentTypeEncoded = (float)a.ApartmentType,
                Rent = (float)a.Rent
            })
            .ToListAsync();
        if (apartments.Count < 10)
        {
            throw new InvalidOperationException($"Insufficient data for training. Need at least 10 apartments, found {apartments.Count}.");
        }
        var dataView = _mlContext.Data.LoadFromEnumerable(apartments);
        var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(ApartmentPriceData.SizeSquareMeters),
                nameof(ApartmentPriceData.NumberOfRooms),
                nameof(ApartmentPriceData.IsFurnished),
                nameof(ApartmentPriceData.HasBalcony),
                nameof(ApartmentPriceData.HasParking),
                nameof(ApartmentPriceData.HasElevator),
                nameof(ApartmentPriceData.HasAirCondition),
                nameof(ApartmentPriceData.HasInternet),
                nameof(ApartmentPriceData.IsPetFriendly),
                nameof(ApartmentPriceData.IsSmokingAllowed),
                nameof(ApartmentPriceData.CityEncoded),
                nameof(ApartmentPriceData.ApartmentTypeEncoded))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 20,
                minimumExampleCountPerLeaf: 10,
                numberOfTrees: 100));
        var model = pipeline.Fit(trainTestSplit.TrainSet);
        var predictions = model.Transform(trainTestSplit.TestSet);
        var metrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: "Label");
        using (_lock.EnterScope()) // .NET 10: Automatic lock release
        {
            _mlContext.Model.Save(model, dataView.Schema, _modelPath);
            _trainedModel = model;
        }
        var metricsDto = new ModelMetricsDto
        {
            RSquared = metrics.RSquared,
            MeanAbsoluteError = metrics.MeanAbsoluteError,
            MeanSquaredError = metrics.MeanSquaredError,
            RootMeanSquaredError = metrics.RootMeanSquaredError,
            TrainingSampleCount = apartments.Count,
            LastTrainedDate = DateTime.UtcNow
        };
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        await File.WriteAllTextAsync(_metricsPath, System.Text.Json.JsonSerializer.Serialize(metricsDto, jsonOptions));
        return metricsDto;
    }
    public async Task<ModelMetricsDto> GetModelMetricsAsync()
    {
        if (File.Exists(_metricsPath))
        {
            var json = await File.ReadAllTextAsync(_metricsPath);
            var jsonOpts = new System.Text.Json.JsonSerializerOptions
            {
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
            };
            return System.Text.Json.JsonSerializer.Deserialize<ModelMetricsDto>(json, jsonOpts) 
                ?? new ModelMetricsDto();
        }
        return new ModelMetricsDto
        {
            RSquared = 0,
            MeanAbsoluteError = 0,
            MeanSquaredError = 0,
            RootMeanSquaredError = 0,
            TrainingSampleCount = 0,
            LastTrainedDate = null
        };
    }
    private float EncodeCitySimple(string? city)
    {
        if (string.IsNullOrEmpty(city))
            return 0;
        return Math.Abs(city.ToLower().GetHashCode() % 1000);
    }
}
