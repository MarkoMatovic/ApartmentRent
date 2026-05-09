using Lander.Helpers;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Listings.Controllers;

public partial class ApartmentsController
{
    private INeighbourhoodService? _neighbourhoodService;

    // Setter injection via a secondary constructor is not possible on partial classes;
    // we resolve the service lazily from HttpContext.RequestServices instead to keep
    // the primary constructor signature unchanged.
    private INeighbourhoodService NeighbourhoodService =>
        _neighbourhoodService ??= HttpContext.RequestServices
            .GetRequiredService<INeighbourhoodService>();

    /// <summary>
    /// Returns Walk Score / Transit Score / Bike Score and nearby points of interest
    /// (schools, parks, supermarkets, transit stops…) for the given apartment.
    ///
    /// Results are cached for 24 hours. Returns 404 if the apartment has no GPS coordinates.
    /// </summary>
    [HttpGet(ApiActionsV1.GetNeighbourhood, Name = nameof(ApiActionsV1.GetNeighbourhood))]
    [AllowAnonymous]
    public async Task<ActionResult<NeighbourhoodInsightsDto>> GetNeighbourhood(
        int apartmentId, CancellationToken ct)
    {
        var result = await NeighbourhoodService.GetInsightsAsync(apartmentId, ct);
        if (result is null)
            return NotFound("Apartment not found or has no GPS coordinates.");

        return Ok(result);
    }
}
