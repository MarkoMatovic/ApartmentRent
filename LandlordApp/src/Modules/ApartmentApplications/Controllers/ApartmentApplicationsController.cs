using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lander.src.Modules.ApartmentApplications.Dtos.InputDto;
using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.Helpers;

namespace Lander.src.Modules.ApartmentApplications.Controllers;

[Route("api/applications")] // Hardcoded route for now or use ApiActionsV1 constant if available? Using hardcoded to be safe based on context.
[ApiController]
public class ApartmentApplicationsController : ControllerBase
{
    private readonly IApartmentApplicationService _applicationService;
    private readonly IUserInterface _userService;

    public ApartmentApplicationsController(IApartmentApplicationService applicationService, IUserInterface userService)
    {
        _applicationService = applicationService;
        _userService = userService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ApplyForApartment([FromBody] CreateApplicationInputDto input)
    {
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid)) return Unauthorized();

        var user = await _userService.GetUserByGuidAsync(Guid.Parse(userGuid));
        if (user == null) return Unauthorized();

        var result = await _applicationService.ApplyForApartmentAsync(user.UserId, input.ApartmentId);
        
        if (result == null)
            return BadRequest("Application failed. You may have already applied for this apartment.");

        return Ok(result);
    }

    [HttpGet("landlord")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> GetLandlordApplications()
    {
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid)) return Unauthorized();

        var user = await _userService.GetUserByGuidAsync(Guid.Parse(userGuid));
        if (user == null) return Unauthorized();

        var applications = await _applicationService.GetLandlordApplicationsAsync(user.UserId);
        return Ok(applications);
    }

    [HttpGet("tenant")]
    [Authorize]
    public async Task<IActionResult> GetTenantApplications()
    {
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid)) return Unauthorized();

        var user = await _userService.GetUserByGuidAsync(Guid.Parse(userGuid));
        if (user == null) return Unauthorized();

        var applications = await _applicationService.GetTenantApplicationsAsync(user.UserId);
        return Ok(applications);
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateApplicationStatusInputDto input)
    {
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid)) return Unauthorized();

        var user = await _userService.GetUserByGuidAsync(Guid.Parse(userGuid));
        if (user == null) return Unauthorized();

        try 
        {
            var result = await _applicationService.UpdateApplicationStatusAsync(id, input.Status, user.UserId);
            if (result == null) return NotFound("Application not found");
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
