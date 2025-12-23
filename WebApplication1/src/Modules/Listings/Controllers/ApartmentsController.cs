using Lander.Helpers;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Lander.src.Modules.Listings.Controllers;

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
        var result = await _apartmentServie.CreateApartmentAsync(apartmentInputDto);
        
        await HttpContext.RequestServices.GetRequiredService<IOutputCacheStore>()
            .EvictByTagAsync("apartments", default);
        
        return Ok(result);
    }

    [HttpGet(ApiActionsV1.GetAllApartments, Name = nameof(ApiActionsV1.GetAllApartments))]
    [AllowAnonymous]
    [OutputCache(PolicyName = "ApartmentsList")]
    public async Task<ActionResult> GetAllApartments([FromQuery] ApartmentFilterDto? filters)
    {
        Console.WriteLine($"Received filters: City={filters?.City}, MinRent={filters?.MinRent}, MaxRent={filters?.MaxRent}, Page={filters?.Page}, PageSize={filters?.PageSize}");
        
        if (filters != null && (filters.City != null || filters.MinRent.HasValue || filters.MaxRent.HasValue))
        {
            return Ok(await _apartmentServie.GetAllApartmentsAsync(filters));
        }
        
        return Ok(await _apartmentServie.GetAllApartmentsAsync());
    }

    [HttpGet(ApiActionsV1.GetApartment, Name = nameof(ApiActionsV1.GetApartment))]
    [AllowAnonymous]
    [OutputCache(PolicyName = "ApartmentDetail")]
    public async Task<ActionResult<GetApartmentDto>> GetApartment([FromQuery] int id)
    {
        return Ok(await _apartmentServie.GetApartmentByIdAsync(id));
    }
    
    [HttpDelete(ApiActionsV1.DeleteApartment, Name = nameof(ApiActionsV1.DeleteApartment))]
    public async Task<ActionResult<bool>> DeleteApartment([FromRoute] int id)
    {
        var result = await _apartmentServie.DeleteApartmentAsync(id);
        
        await HttpContext.RequestServices.GetRequiredService<IOutputCacheStore>()
            .EvictByTagAsync("apartments", default);
        
        return Ok(result);
    }

    [HttpPut(ApiActionsV1.ActivateApartment, Name = nameof(ApiActionsV1.ActivateApartment))]
    public async Task<ActionResult<bool>> ActivateApartment(int id)
    {
        var result = await _apartmentServie.ActivateApartmentAsync(id);
        
        await HttpContext.RequestServices.GetRequiredService<IOutputCacheStore>()
            .EvictByTagAsync("apartments", default);
        
        return Ok(result);
    }
}
