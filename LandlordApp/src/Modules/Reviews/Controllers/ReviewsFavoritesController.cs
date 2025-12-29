using Grpc.Core;
using Lander.Helpers;
using Lander.src.Modules.Reviews.Client;
using Lander.src.Modules.Reviews.proto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Reviews.Controllers
{
    [Route(ApiActionsV1.Reviews)]
    [ApiController]
    public class ReviewsFavoritesController : ControllerBase
    {
        private readonly GrpcServiceClient _grpcClient;
        public ReviewsFavoritesController(IConfiguration configuration)
        {
            string grpcServerUrl = configuration["GrpcServerUrl"];
            _grpcClient = new GrpcServiceClient(grpcServerUrl);
        }

        [HttpPost(ApiActionsV1.CreateFavorite, Name = nameof(ApiActionsV1.CreateFavorite))]
        public async Task<IActionResult> CreateFavorite([FromBody] CreateFavoriteRequest request)
        {
            var response = await _grpcClient.CreateFavoriteAsync(request);

            return Ok(response);
        }
        [HttpPost(ApiActionsV1.CreateReview, Name = nameof(ApiActionsV1.CreateReview))]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            var response = await _grpcClient.CreateReviewAsync(request);
            return Ok(response);
        }

        [HttpGet(ApiActionsV1.GetReviewById, Name = nameof(ApiActionsV1.GetReviewById))]
        public async Task<IActionResult> GetReviewById([FromQuery] int reviewId)
        {
            var response = await _grpcClient.GetReviewByIdAsync(reviewId);
            return Ok(response);
        }

        [HttpGet("apartment/{apartmentId}")]
        public async Task<IActionResult> GetReviewsByApartmentId(int apartmentId)
        {
            var response = await _grpcClient.GetReviewsByApartmentIdAsync(apartmentId);
            return Ok(response.Reviews);
        }

        [HttpDelete(ApiActionsV1.DeleteReview, Name = nameof(ApiActionsV1.DeleteReview))]
        public async Task<IActionResult> DeleteReview([FromRoute] int id)
        {
            var response = await _grpcClient.DeleteReviewAsync(id);
            return Ok(response);
        }

        [HttpDelete(ApiActionsV1.DeleteFavorite, Name = nameof(ApiActionsV1.DeleteFavorite))]
        public async Task<IActionResult> DeleteFavorite([FromRoute] int id)
        {
            var response = await _grpcClient.DeleteFavoriteAsync(id);
            return Ok(response);
        }

        [HttpGet(ApiActionsV1.GetUserFavorites, Name = nameof(ApiActionsV1.GetUserFavorites))]
        public async Task<IActionResult> GetUserFavorites([FromRoute] int userId)
        {
            var response = await _grpcClient.GetUserFavoritesAsync(userId);
            return Ok(response.Favorites);
        }
    }
}
