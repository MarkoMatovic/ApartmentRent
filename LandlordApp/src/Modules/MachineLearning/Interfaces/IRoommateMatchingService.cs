using Lander.src.Modules.MachineLearning.Dtos;
namespace Lander.src.Modules.MachineLearning.Interfaces;
public interface IRoommateMatchingService
{
    Task<List<RoommateMatchScoreDto>> GetMatchesForUserAsync(int userId, int topN = 10);
    Task<float> CalculateMatchScoreAsync(int userId1, int userId2);
}
