using Microsoft.ML.Data;

namespace Lander.src.Modules.MachineLearning.Models;

/// <summary>
/// Input data for training the apartment price prediction model
/// </summary>
public class ApartmentPriceData
{
    [LoadColumn(0)]
    public float SizeSquareMeters { get; set; }
    
    [LoadColumn(1)]
    public float NumberOfRooms { get; set; }
    
    [LoadColumn(2)]
    public float IsFurnished { get; set; } // 0 or 1
    
    [LoadColumn(3)]
    public float HasBalcony { get; set; } // 0 or 1
    
    [LoadColumn(4)]
    public float HasParking { get; set; } // 0 or 1
    
    [LoadColumn(5)]
    public float HasElevator { get; set; } // 0 or 1
    
    [LoadColumn(6)]
    public float HasAirCondition { get; set; } // 0 or 1
    
    [LoadColumn(7)]
    public float HasInternet { get; set; } // 0 or 1
    
    [LoadColumn(8)]
    public float IsPetFriendly { get; set; } // 0 or 1
    
    [LoadColumn(9)]
    public float IsSmokingAllowed { get; set; } // 0 or 1
    
    [LoadColumn(10)]
    public float CityEncoded { get; set; } // Encoded city value
    
    [LoadColumn(11)]
    public float ApartmentTypeEncoded { get; set; } // Encoded apartment type
    
    [LoadColumn(12)]
    [ColumnName("Label")]
    public float Rent { get; set; } // Target variable - actual rent price
}

/// <summary>
/// Output prediction from the price prediction model
/// </summary>
public class ApartmentPricePrediction
{
    [ColumnName("Score")]
    public float PredictedPrice { get; set; }
}
