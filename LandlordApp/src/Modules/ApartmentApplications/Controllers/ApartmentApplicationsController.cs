using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lander.src.Common;
using Lander.src.Modules.ApartmentApplications.Dtos.InputDto;
using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.Helpers;

namespace Lander.src.Modules.ApartmentApplications.Controllers;

[Route("api/applications")]
[ApiController]
public class ApartmentApplicationsController : ApiControllerBase
{
    private readonly IApartmentApplicationService _applicationService;
    private readonly IApplicationApprovalService _approvalService;

    public ApartmentApplicationsController(
        IApartmentApplicationService applicationService,
        IApplicationApprovalService approvalService,
        IUserInterface userService) : base(userService)
    {
        _applicationService = applicationService;
        _approvalService = approvalService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ApplyForApartment([FromBody] CreateApplicationInputDto input)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var result = await _applicationService.ApplyForApartmentAsync(user.UserId, input.ApartmentId, input.IsPriority);

        if (result == null)
            return BadRequest("Application failed. You may have already applied for this apartment.");

        return Ok(result);
    }

    [HttpGet("landlord")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> GetLandlordApplications()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var applications = await _applicationService.GetLandlordApplicationsAsync(user.UserId);
        return Ok(applications);
    }

    [HttpGet("tenant")]
    [Authorize]
    public async Task<IActionResult> GetTenantApplications()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var applications = await _applicationService.GetTenantApplicationsAsync(user.UserId);
        return Ok(applications);
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateApplicationStatusInputDto input)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var result = await _applicationService.UpdateApplicationStatusAsync(id, input.Status, user.UserId);
        if (result == null) return NotFound("Application not found");
        return Ok(result);
    }

    [HttpGet("check-approval/{apartmentId}")]
    [Authorize]
    public async Task<IActionResult> CheckApprovalStatus(int apartmentId)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var status = await _approvalService.GetApprovalStatusAsync(user.UserId, apartmentId);

        return Ok(new
        {
            hasApprovedApplication = status.HasApprovedApplication,
            applicationStatus = status.ApplicationStatus,
            applicationId = status.ApplicationId
        });
    }
}
