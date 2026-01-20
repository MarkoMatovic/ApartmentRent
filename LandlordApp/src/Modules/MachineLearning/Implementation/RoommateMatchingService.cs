using Lander.src.Modules.MachineLearning.Dtos;
using Lander.src.Modules.MachineLearning.Interfaces;
using Lander.src.Modules.Roommates.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Lander.src.Modules.MachineLearning.Implementation;
public class RoommateMatchingService : IRoommateMatchingService
{
    private readonly RoommatesContext _roommatesContext;
    private readonly IRoommateService _roommateService;
    private const float BUDGET_WEIGHT = 0.30f;
    private const float LIFESTYLE_WEIGHT = 0.25f;
    private const float PREFERENCES_WEIGHT = 0.25f;
    private const float LOCATION_WEIGHT = 0.20f;
    public RoommateMatchingService(RoommatesContext roommatesContext, IRoommateService roommateService)
    {
        _roommatesContext = roommatesContext;
        _roommateService = roommateService;
    }
    public async Task<List<RoommateMatchScoreDto>> GetMatchesForUserAsync(int userId, int topN = 10)
    {
        var userRoommate = await _roommatesContext.Roommates
            .FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive);
        if (userRoommate == null)
        {
            return new List<RoommateMatchScoreDto>();
        }
        var otherRoommates = await _roommatesContext.Roommates
            .Where(r => r.UserId != userId && r.IsActive)
            .ToListAsync();
        var matchScores = new List<RoommateMatchScoreDto>();
        foreach (var candidate in otherRoommates)
        {
            var score = CalculateMatchScore(userRoommate, candidate);
            var featureScores = CalculateFeatureScores(userRoommate, candidate);
            var matchQuality = score >= 80 ? "Excellent" :
                              score >= 60 ? "Good" :
                              score >= 40 ? "Fair" : "Poor";
            matchScores.Add(new RoommateMatchScoreDto
            {
                RoommateId = candidate.RoommateId,
                MatchPercentage = score,
                FeatureScores = featureScores,
                MatchQuality = matchQuality
            });
        }
        var topMatches = matchScores
            .OrderByDescending(m => m.MatchPercentage)
            .Take(topN)
            .ToList();
        foreach (var match in topMatches)
        {
            match.Roommate = await _roommateService.GetRoommateByIdAsync(match.RoommateId);
        }
        return topMatches;
    }
    public async Task<float> CalculateMatchScoreAsync(int userId1, int userId2)
    {
        var roommate1 = await _roommatesContext.Roommates
            .FirstOrDefaultAsync(r => r.UserId == userId1 && r.IsActive);
        var roommate2 = await _roommatesContext.Roommates
            .FirstOrDefaultAsync(r => r.UserId == userId2 && r.IsActive);
        if (roommate1 == null || roommate2 == null)
        {
            return 0;
        }
        return CalculateMatchScore(roommate1, roommate2);
    }
    private float CalculateMatchScore(Lander.src.Modules.Roommates.Models.Roommate user, Lander.src.Modules.Roommates.Models.Roommate candidate)
    {
        var budgetScore = CalculateBudgetCompatibility(user, candidate);
        var lifestyleScore = CalculateLifestyleCompatibility(user, candidate);
        var preferencesScore = CalculatePreferencesCompatibility(user, candidate);
        var locationScore = CalculateLocationCompatibility(user, candidate);
        var totalScore = (budgetScore * BUDGET_WEIGHT) +
                        (lifestyleScore * LIFESTYLE_WEIGHT) +
                        (preferencesScore * PREFERENCES_WEIGHT) +
                        (locationScore * LOCATION_WEIGHT);
        return totalScore * 100; // Convert to percentage
    }
    private Dictionary<string, float> CalculateFeatureScores(Lander.src.Modules.Roommates.Models.Roommate user, Lander.src.Modules.Roommates.Models.Roommate candidate)
    {
        return new Dictionary<string, float>
        {
            { "Budget", CalculateBudgetCompatibility(user, candidate) * 100 },
            { "Lifestyle", CalculateLifestyleCompatibility(user, candidate) * 100 },
            { "Preferences", CalculatePreferencesCompatibility(user, candidate) * 100 },
            { "Location", CalculateLocationCompatibility(user, candidate) * 100 }
        };
    }
    private float CalculateBudgetCompatibility(Lander.src.Modules.Roommates.Models.Roommate user, Lander.src.Modules.Roommates.Models.Roommate candidate)
    {
        if (!user.BudgetMin.HasValue || !user.BudgetMax.HasValue ||
            !candidate.BudgetMin.HasValue || !candidate.BudgetMax.HasValue)
        {
            return 0.5f; // Neutral score if budget not specified
        }
        var userMin = (float)user.BudgetMin.Value;
        var userMax = (float)user.BudgetMax.Value;
        var candMin = (float)candidate.BudgetMin.Value;
        var candMax = (float)candidate.BudgetMax.Value;
        var overlapMin = Math.Max(userMin, candMin);
        var overlapMax = Math.Min(userMax, candMax);
        if (overlapMax < overlapMin)
        {
            var distance = Math.Min(Math.Abs(userMax - candMin), Math.Abs(candMax - userMin));
            var maxBudget = Math.Max(userMax, candMax);
            return Math.Max(0, 1 - (distance / maxBudget));
        }
        var overlapRange = overlapMax - overlapMin;
        var userRange = userMax - userMin;
        var candRange = candMax - candMin;
        var avgRange = (userRange + candRange) / 2;
        return Math.Min(1.0f, overlapRange / avgRange);
    }
    private float CalculateLifestyleCompatibility(Lander.src.Modules.Roommates.Models.Roommate user, Lander.src.Modules.Roommates.Models.Roommate candidate)
    {
        float score = 0;
        int factors = 0;
        if (!string.IsNullOrEmpty(user.Lifestyle) && !string.IsNullOrEmpty(candidate.Lifestyle))
        {
            score += StringSimilarity(user.Lifestyle, candidate.Lifestyle);
            factors++;
        }
        if (!string.IsNullOrEmpty(user.Cleanliness) && !string.IsNullOrEmpty(candidate.Cleanliness))
        {
            score += StringSimilarity(user.Cleanliness, candidate.Cleanliness);
            factors++;
        }
        if (!string.IsNullOrEmpty(user.Profession) && !string.IsNullOrEmpty(candidate.Profession))
        {
            score += StringSimilarity(user.Profession, candidate.Profession) * 0.5f;
            factors++;
        }
        return factors > 0 ? score / factors : 0.5f;
    }
    private float CalculatePreferencesCompatibility(Lander.src.Modules.Roommates.Models.Roommate user, Lander.src.Modules.Roommates.Models.Roommate candidate)
    {
        float score = 0;
        int factors = 0;
        if (user.SmokingAllowed.HasValue && candidate.SmokingAllowed.HasValue)
        {
            score += user.SmokingAllowed == candidate.SmokingAllowed ? 1.0f : 0.0f;
            factors++;
        }
        if (user.PetFriendly.HasValue && candidate.PetFriendly.HasValue)
        {
            score += user.PetFriendly == candidate.PetFriendly ? 1.0f : 0.0f;
            factors++;
        }
        if (user.GuestsAllowed.HasValue && candidate.GuestsAllowed.HasValue)
        {
            score += user.GuestsAllowed == candidate.GuestsAllowed ? 1.0f : 0.0f;
            factors++;
        }
        return factors > 0 ? score / factors : 0.5f;
    }
    private float CalculateLocationCompatibility(Lander.src.Modules.Roommates.Models.Roommate user, Lander.src.Modules.Roommates.Models.Roommate candidate)
    {
        if (string.IsNullOrEmpty(user.PreferredLocation) || string.IsNullOrEmpty(candidate.PreferredLocation))
        {
            return 0.5f; // Neutral if location not specified
        }
        return StringSimilarity(user.PreferredLocation, candidate.PreferredLocation);
    }
    private float StringSimilarity(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            return 0;
        var s1 = str1.ToLower().Trim();
        var s2 = str2.ToLower().Trim();
        if (s1 == s2)
            return 1.0f;
        if (s1.Contains(s2) || s2.Contains(s1))
            return 0.7f;
        var commonChars = s1.Intersect(s2).Count();
        var totalChars = Math.Max(s1.Length, s2.Length);
        return (float)commonChars / totalChars;
    }
}
