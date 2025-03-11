using Lander.Helpers;
using Lander.src.Modules.ApartmentApplications.Dtos.Dto;
using Lander.src.Modules.ApartmentApplications.Dtos.InputDto;
using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.ApartmentApplications.Controllers;

[Route(ApiActionsV1.Rent)]
[ApiController]
public class ApartmentsController : ControllerBase
{
    #region Properties
    private readonly IApartmentService _apartmentServie;
    #endregion
    #region Constructors
    public ApartmentsController(IApartmentService apartmentServie)
    {
        _apartmentServie = apartmentServie;
    }
    #endregion

    [HttpPost(ApiActionsV1.CreateApartment, Name = nameof(ApiActionsV1.CreateApartment))]
    public async Task<ActionResult<ApartmentDto>> CreateApartment([FromBody] ApartmentInputDto apartmentInputDto)
    {
        return Ok(await _apartmentServie.CreateApartmentAsync(apartmentInputDto));
    }
    
}
