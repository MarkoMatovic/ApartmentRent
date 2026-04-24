using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.MachineLearning.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
namespace Lander.src.Modules.Listings.Controllers;

[Route(ApiActionsV1.Rent)]
[ApiController]
public partial class ApartmentsController : ControllerBase
{
    private readonly IApartmentService _apartmentService;
    private readonly SimpleEmbeddingService _embeddingService;

    public ApartmentsController(
        IApartmentService apartmentService,
        SimpleEmbeddingService embeddingService)
    {
        _apartmentService = apartmentService;
        _embeddingService = embeddingService;
    }
    [HttpPost(ApiActionsV1.CreateApartment, Name = nameof(ApiActionsV1.CreateApartment))]
    [Authorize]
    public async Task<ActionResult<ApartmentDto>> CreateApartment([FromBody] ApartmentInputDto apartmentInputDto)
    {
        var result = await _apartmentService.CreateApartmentAsync(apartmentInputDto);
        return Ok(result);
    }

    [HttpGet(ApiActionsV1.GetAllApartments, Name = nameof(ApiActionsV1.GetAllApartments))]
    [AllowAnonymous]
    public async Task<ActionResult> GetAllApartments([FromQuery] ApartmentFilterDto? filters)
    {
        filters ??= new ApartmentFilterDto();
        return Ok(await _apartmentService.GetAllApartmentsAsync(filters));
    }
    [HttpGet(ApiActionsV1.GetAllApartmentsKeyset, Name = nameof(ApiActionsV1.GetAllApartmentsKeyset))]
    [AllowAnonymous]
    public async Task<ActionResult<KeysetPagedResult<ApartmentDto>>> GetAllApartmentsKeyset(
        [FromQuery] ApartmentFilterDto? filters,
        [FromQuery] int? afterId = null,
        [FromQuery] int pageSize = 20)
    {
        filters ??= new ApartmentFilterDto();
        return Ok(await _apartmentService.GetAllApartmentsKeysetAsync(filters, afterId, pageSize));
    }

    [HttpGet(ApiActionsV1.GetMyApartments, Name = nameof(ApiActionsV1.GetMyApartments))]
    [Authorize]
    public async Task<ActionResult> GetMyApartments()
    {
        var result = await _apartmentService.GetMyApartmentsAsync();
        return Ok(result);
    }
    [HttpGet(ApiActionsV1.GetApartment, Name = nameof(ApiActionsV1.GetApartment))]
    [AllowAnonymous]
    [OutputCache(PolicyName = "ApartmentDetail")]
    public async Task<ActionResult<GetApartmentDto>> GetApartment([FromQuery] int id)
    {
        return Ok(await _apartmentService.GetApartmentByIdAsync(id));
    }
    [HttpPut(ApiActionsV1.UpdateApartment, Name = nameof(ApiActionsV1.UpdateApartment))]
    [Authorize]
    public async Task<ActionResult<ApartmentDto>> UpdateApartment([FromRoute] int id, [FromBody] ApartmentUpdateInputDto updateDto)
    {
        var result = await _apartmentService.UpdateApartmentAsync(id, updateDto);
        return Ok(result);
    }
    [HttpDelete(ApiActionsV1.DeleteApartment, Name = nameof(ApiActionsV1.DeleteApartment))]
    [Authorize]
    public async Task<ActionResult<bool>> DeleteApartment([FromRoute] int id)
    {
        var result = await _apartmentService.DeleteApartmentAsync(id);
        return Ok(result);
    }
    [HttpPut(ApiActionsV1.ActivateApartment, Name = nameof(ApiActionsV1.ActivateApartment))]
    [Authorize]
    public async Task<ActionResult<bool>> ActivateApartment(int id)
    {
        var result = await _apartmentService.ActivateApartmentAsync(id);
        return Ok(result);
    }
}
