using Lander.Helpers;
using Lander.src.Modules.Reviews.Interfaces;
using Lander.src.Modules.Reviews.proto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lander.src.Modules.Reviews.Controllers
{
    [Route(ApiActionsV1.Reviews)]
    [ApiController]
    [Authorize]
    public class ReviewsFavoritesController : ControllerBase
    {
        private readonly IReviewFavoriteService _reviews;

        public ReviewsFavoritesController(IReviewFavoriteService reviews)
        {
            _reviews = reviews;
        }

        private int? TryGetCurrentUserId()
        {
            var claim = User.FindFirstValue("userId");
            return int.TryParse(claim, out var id) ? id : null;
        }

        private string? GetCurrentUserGuid() => User.FindFirstValue("sub");

        [HttpPost(ApiActionsV1.CreateFavorite, Name = nameof(ApiActionsV1.CreateFavorite))]
        public async Task<IActionResult> CreateFavorite([FromBody] CreateFavoriteRequest request)
        {
            var callerId = TryGetCurrentUserId();
            if (callerId is null) return Unauthorized();
            var callerGuid = GetCurrentUserGuid();
            if (string.IsNullOrEmpty(callerGuid)) return Unauthorized();

            // Always use JWT identity — never trust client-supplied UserId/CreatedByGuid
            request.UserId = callerId.Value;
            request.CreatedByGuid = callerGuid;

            var response = await _reviews.CreateFavoriteAsync(request);
            return Ok(response);
        }

        [HttpPost(ApiActionsV1.CreateReview, Name = nameof(ApiActionsV1.CreateReview))]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("create-review")]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            var callerId = TryGetCurrentUserId();
            if (callerId is null) return Unauthorized();
            var callerGuid = GetCurrentUserGuid();
            if (string.IsNullOrEmpty(callerGuid)) return Unauthorized();

            // Always use JWT identity — never trust client-supplied UserId/CreatedByGuid
            request.UserId = callerId.Value;
            request.CreatedByGuid = callerGuid;

            var response = await _reviews.CreateReviewAsync(request);
            return Ok(response);
        }

        [HttpGet(ApiActionsV1.GetReviewById, Name = nameof(ApiActionsV1.GetReviewById))]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewById([FromQuery] int reviewId)
        {
            var response = await _reviews.GetReviewByIdAsync(reviewId);
            return Ok(response);
        }

        [HttpGet(ApiActionsV1.GetReviewsByApartmentId, Name = nameof(ApiActionsV1.GetReviewsByApartmentId))]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsByApartmentId(int apartmentId)
        {
            var response = await _reviews.GetReviewsByApartmentIdAsync(apartmentId);
            return Ok(response.Reviews);
        }

        [HttpDelete(ApiActionsV1.DeleteReview, Name = nameof(ApiActionsV1.DeleteReview))]
        public async Task<IActionResult> DeleteReview([FromRoute] int id)
        {
            var callerGuid = GetCurrentUserGuid();
            if (string.IsNullOrEmpty(callerGuid)) return Unauthorized();

            var response = await _reviews.DeleteReviewAsync(id, callerGuid);
            if (!response.Success && response.Message.Contains("Unauthorized"))
                return Forbid();
            return Ok(response);
        }

        [HttpDelete(ApiActionsV1.DeleteFavorite, Name = nameof(ApiActionsV1.DeleteFavorite))]
        public async Task<IActionResult> DeleteFavorite([FromRoute] int id)
        {
            var callerGuid = GetCurrentUserGuid();
            if (string.IsNullOrEmpty(callerGuid)) return Unauthorized();

            var response = await _reviews.DeleteFavoriteAsync(id, callerGuid);
            if (!response.Success && response.Message.Contains("Unauthorized"))
                return Forbid();
            return Ok(response);
        }

        [HttpGet(ApiActionsV1.GetUserFavorites, Name = nameof(ApiActionsV1.GetUserFavorites))]
        public async Task<IActionResult> GetUserFavorites([FromRoute] int userId)
        {
            var callerId = TryGetCurrentUserId();
            if (callerId is null) return Unauthorized();
            if (callerId.Value != userId && !User.IsInRole("Admin")) return Forbid();

            var response = await _reviews.GetUserFavoritesAsync(userId);
            return Ok(response.Favorites);
        }
    }
}
