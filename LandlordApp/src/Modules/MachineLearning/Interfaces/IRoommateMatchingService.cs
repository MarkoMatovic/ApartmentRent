using Lander.src.Modules.MachineLearning.Dtos;

namespace Lander.src.Modules.MachineLearning.Interfaces;

public interface IRoommateMatchingService
{
    /// <summary>
    /// Get top N best matches for a user's roommate profile
    /// </summary>
    Task<List<RoommateMatchScoreDto>> GetMatchesForUserAsync(int userId, int topN = 10);
    
    /// <summary>
    /// Calculate compatibility score between two users
    /// </summary>
    Task<float> CalculateMatchScoreAsync(int userId1, int userId2);
}
