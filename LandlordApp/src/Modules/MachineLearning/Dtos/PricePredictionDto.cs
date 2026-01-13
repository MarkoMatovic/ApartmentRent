namespace Lander.src.Modules.MachineLearning.Dtos;

public class PricePredictionRequestDto
{
    public int? SizeSquareMeters { get; set; }
    public int? NumberOfRooms { get; set; }
    public bool? IsFurnished { get; set; }
    public bool? HasBalcony { get; set; }
    public bool? HasParking { get; set; }
    public bool? HasElevator { get; set; }
    public bool? HasAirCondition { get; set; }
    public bool? HasInternet { get; set; }
    public bool? IsPetFriendly { get; set; }
    public bool? IsSmokingAllowed { get; set; }
    public string? City { get; set; }
    public int? ApartmentType { get; set; } // 0=Studio, 1=OneBedroom, etc.
}

public class PricePredictionResponseDto
{
    public decimal PredictedPrice { get; set; }
    public decimal ConfidenceScore { get; set; } // 0-100
    public string Message { get; set; } = string.Empty;
}

public class ModelMetricsDto
{
    public double RSquared { get; set; } // Coefficient of determination
    public double MeanAbsoluteError { get; set; }
    public double MeanSquaredError { get; set; }
    public double RootMeanSquaredError { get; set; }
    public int TrainingSampleCount { get; set; }
    public DateTime? LastTrainedDate { get; set; }
}
