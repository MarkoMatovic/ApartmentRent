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

    // hibrid, only for testing
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
    }
}
