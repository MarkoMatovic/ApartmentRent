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
    private readonly IApartmentService _apartmentServie;
    private readonly Lander.src.Modules.Analytics.Interfaces.IAnalyticsService _analyticsService;

    public ApartmentsController(IApartmentService apartmentServie, Lander.src.Modules.Analytics.Interfaces.IAnalyticsService analyticsService)
    {
        _apartmentServie = apartmentServie;
        _analyticsService = analyticsService;
    }

    [HttpPost(ApiActionsV1.CreateApartment, Name = nameof(ApiActionsV1.CreateApartment))]
    public async Task<ActionResult<ApartmentDto>> CreateApartment([FromBody] ApartmentInputDto apartmentInputDto)
    {
        try
        {
            // Log the incoming data for debugging
            System.Diagnostics.Debug.WriteLine($"Creating apartment with {apartmentInputDto.ImageUrls?.Count ?? 0} images");
            if (apartmentInputDto.ImageUrls != null && apartmentInputDto.ImageUrls.Any())
            {
                System.Diagnostics.Debug.WriteLine($"Image URLs: {string.Join(", ", apartmentInputDto.ImageUrls)}");
            }

            var result = await _apartmentServie.CreateApartmentAsync(apartmentInputDto);
            
            await HttpContext.RequestServices.GetRequiredService<IOutputCacheStore>()
                .EvictByTagAsync("apartments", default);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CreateApartment controller: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { message = "Error creating apartment", error = ex.Message });
        }
    }

    [HttpGet(ApiActionsV1.GetAllApartments, Name = nameof(ApiActionsV1.GetAllApartments))]
    [AllowAnonymous]
    [OutputCache(PolicyName = "ApartmentsList")]
    public async Task<ActionResult> GetAllApartments([FromQuery] ApartmentFilterDto? filters)
    {
        // Track search event
        if (filters != null && (filters.City != null || filters.MinRent.HasValue || filters.MaxRent.HasValue))
        {
            var searchQuery = $"City:{filters.City},MinRent:{filters.MinRent},MaxRent:{filters.MaxRent}";
            _ = _analyticsService.TrackEventAsync(
                "ApartmentSearch", 
                "Listings", 
                searchQuery: searchQuery,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].ToString()
            );
            return Ok(await _apartmentServie.GetAllApartmentsAsync(filters));
        }
        
        return Ok(await _apartmentServie.GetAllApartmentsAsync());
    }

    [HttpGet(ApiActionsV1.GetMyApartments, Name = nameof(ApiActionsV1.GetMyApartments))]
    public async Task<ActionResult> GetMyApartments()
    {
        System.Diagnostics.Debug.WriteLine("GetMyApartments controller called");
        var result = await _apartmentServie.GetMyApartmentsAsync();
        System.Diagnostics.Debug.WriteLine($"GetMyApartments returning {result.TotalCount} apartments");
        return Ok(result);
    }

    [HttpGet(ApiActionsV1.GetApartment, Name = nameof(ApiActionsV1.GetApartment))]
    [AllowAnonymous]
    [OutputCache(PolicyName = "ApartmentDetail")]
    public async Task<ActionResult<GetApartmentDto>> GetApartment([FromQuery] int id)
    {
        // Track apartment view event
        _ = _analyticsService.TrackEventAsync(
            "ApartmentView", 
            "Listings", 
            entityId: id,
            entityType: "Apartment",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString()
        );
        
        return Ok(await _apartmentServie.GetApartmentByIdAsync(id));
    }

    [HttpPut(ApiActionsV1.UpdateApartment, Name = nameof(ApiActionsV1.UpdateApartment))]
    public async Task<ActionResult<ApartmentDto>> UpdateApartment([FromRoute] int id, [FromBody] ApartmentUpdateInputDto updateDto)
    {
        var result = await _apartmentServie.UpdateApartmentAsync(id, updateDto);
        
        await HttpContext.RequestServices.GetRequiredService<IOutputCacheStore>()
            .EvictByTagAsync("apartments", default);
        
        return Ok(result);
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
