using Lander.src.Modules.Roommates.Dtos.Dto;

namespace Lander.src.Modules.MachineLearning.Dtos;

public class RoommateMatchScoreDto
{
    public int RoommateId { get; set; }
    public RoommateDto? Roommate { get; set; }
    public float MatchPercentage { get; set; } // 0-100
    public Dictionary<string, float> FeatureScores { get; set; } = new();
    public string MatchQuality { get; set; } = string.Empty; // "Excellent", "Good", "Fair", "Poor"
}

public class RoommateMatchRequestDto
{
    public int UserId { get; set; }
    public int TopN { get; set; } = 10;
}

public class MatchScoreRequestDto
{
    public int UserId1 { get; set; }
    public int UserId2 { get; set; }
}
