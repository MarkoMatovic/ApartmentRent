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
    private static readonly object _lock = new object();

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
        
        // Load existing model if available
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

        // Load model if not already loaded
        if (_trainedModel == null)
        {
            lock (_lock)
            {
                if (_trainedModel == null)
                {
                    _trainedModel = _mlContext.Model.Load(_modelPath, out var _);
                }
            }
        }

        // Create prediction engine
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<ApartmentPriceData, ApartmentPricePrediction>(_trainedModel);

        // Convert request to ML input
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

        // Calculate confidence based on model metrics
        var metrics = await GetModelMetricsAsync();
        var confidenceScore = Math.Max(0, Math.Min(100, metrics.RSquared * 100));

        return new PricePredictionResponseDto
        {
            PredictedPrice = Math.Round((decimal)prediction.PredictedPrice, 2),
            ConfidenceScore = (decimal)confidenceScore,
            Message = "Price prediction successful"
        };
    }

    public async Task<ModelMetricsDto> TrainModelAsync()
    {
        // Fetch apartment data from database
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

        // Load data into IDataView
        var dataView = _mlContext.Data.LoadFromEnumerable(apartments);

        // Split data into training and test sets (80/20)
        var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // Define the training pipeline
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

        // Train the model
        var model = pipeline.Fit(trainTestSplit.TrainSet);

        // Evaluate the model
        var predictions = model.Transform(trainTestSplit.TestSet);
        var metrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: "Label");

        // Save the model
        lock (_lock)
        {
            _mlContext.Model.Save(model, dataView.Schema, _modelPath);
            _trainedModel = model;
        }

        // Save metrics
        var metricsDto = new ModelMetricsDto
        {
            RSquared = metrics.RSquared,
            MeanAbsoluteError = metrics.MeanAbsoluteError,
            MeanSquaredError = metrics.MeanSquaredError,
            RootMeanSquaredError = metrics.RootMeanSquaredError,
            TrainingSampleCount = apartments.Count,
            LastTrainedDate = DateTime.UtcNow
        };

        await File.WriteAllTextAsync(_metricsPath, System.Text.Json.JsonSerializer.Serialize(metricsDto));

        return metricsDto;
    }

    public async Task<ModelMetricsDto> GetModelMetricsAsync()
    {
        if (File.Exists(_metricsPath))
        {
            var json = await File.ReadAllTextAsync(_metricsPath);
            return System.Text.Json.JsonSerializer.Deserialize<ModelMetricsDto>(json) 
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

    /// <summary>
    /// Simple city encoding - in production, use proper one-hot encoding or label encoding
    /// </summary>
    private float EncodeCitySimple(string? city)
    {
        if (string.IsNullOrEmpty(city))
            return 0;

        // Simple hash-based encoding
        return Math.Abs(city.ToLower().GetHashCode() % 1000);
    }
}
