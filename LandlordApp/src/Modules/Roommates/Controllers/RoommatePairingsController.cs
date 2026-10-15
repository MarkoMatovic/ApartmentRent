using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Modules.Roommates.Dtos.Dto;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Roommates.Controllers;

/// <summary>
/// Two people agreeing to be roommates: propose / accept / end, and the shortlist of apartments they share.
/// Every call acts as the signed-in user; ids of other people only ever name the counterpart.
/// </summary>
[Route(ApiActionsV1.RoommatePairing)]
[ApiController]
[Authorize]
public class RoommatePairingsController : ApiControllerBase
{
    private readonly IRoommatePairingService _pairings;

    public RoommatePairingsController(IRoommatePairingService pairings, IUserInterface userService) : base(userService)
    {
        _pairings = pairings;
    }

    private int CurrentUserId => TryGetCurrentUserId() ?? throw new UnauthorizedAccessException();

    [HttpGet(ApiActionsV1.GetMyPairing)]
    public async Task<ActionResult<MyPairingStateDto>> GetMine() =>
        Ok(await _pairings.GetMyStateAsync(CurrentUserId));

    [HttpPost(ApiActionsV1.ProposePairing)]
    public async Task<ActionResult<PairingDto>> Propose([FromBody] ProposePairingInputDto input) =>
        Ok(await _pairings.ProposeAsync(CurrentUserId, input.PartnerUserId));

    [HttpPost(ApiActionsV1.AcceptPairing)]
    public async Task<ActionResult<PairingDto>> Accept([FromRoute] int id) =>
        Ok(await _pairings.AcceptAsync(CurrentUserId, id));

    [HttpPost(ApiActionsV1.DeclinePairing)]
    public async Task<IActionResult> Decline([FromRoute] int id)
    {
        await _pairings.DeclineAsync(CurrentUserId, id);
        return NoContent();
    }

    [HttpPost(ApiActionsV1.CancelPairing)]
    public async Task<IActionResult> Cancel([FromRoute] int id)
    {
        await _pairings.CancelAsync(CurrentUserId, id);
        return NoContent();
    }

    [HttpPost(ApiActionsV1.EndPairing)]
    public async Task<IActionResult> End([FromRoute] int id)
    {
        await _pairings.EndAsync(CurrentUserId, id);
        return NoContent();
    }

    [HttpGet(ApiActionsV1.SharedApartments)]
    public async Task<ActionResult<List<SharedApartmentDto>>> GetSharedApartments() =>
        Ok(await _pairings.GetSharedApartmentsAsync(CurrentUserId));

    [HttpPost(ApiActionsV1.SharedApartments)]
    public async Task<ActionResult<SharedApartmentDto>> ShareApartment([FromBody] ShareApartmentInputDto input) =>
        Ok(await _pairings.ShareApartmentAsync(CurrentUserId, input.ApartmentId, input.Note));

    [HttpDelete(ApiActionsV1.SharedApartment)]
    public async Task<IActionResult> RemoveSharedApartment([FromRoute] int apartmentId) =>
        await _pairings.RemoveSharedApartmentAsync(CurrentUserId, apartmentId) ? NoContent() : NotFound();
}
