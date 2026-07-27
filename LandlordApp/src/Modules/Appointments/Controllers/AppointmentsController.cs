using Lander.Helpers;
using Lander.src.Modules.Appointments.Dtos;
using Lander.src.Modules.Appointments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Appointments.Controllers;

[Authorize]
[ApiController]
[Route(ApiActionsV1.Appointments)]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        try
        {
            var appointment = await _appointmentService.CreateAppointmentAsync(dto);
            return Ok(appointment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet(ApiActionsV1.GetMyAppointments, Name = nameof(ApiActionsV1.GetMyAppointments))]
    public async Task<ActionResult<List<AppointmentDto>>> GetMyAppointments()
    {
        var appointments = await _appointmentService.GetMyAppointmentsAsync();
        return Ok(appointments);
    }

    [HttpGet(ApiActionsV1.GetLandlordAppointments, Name = nameof(ApiActionsV1.GetLandlordAppointments))]
    public async Task<ActionResult<List<AppointmentDto>>> GetLandlordAppointments()
    {
        var appointments = await _appointmentService.GetLandlordAppointmentsAsync();
        return Ok(appointments);
    }

    [HttpGet(ApiActionsV1.GetAvailableSlots, Name = nameof(ApiActionsV1.GetAvailableSlots))]
    [AllowAnonymous]
    public async Task<ActionResult<List<AvailableSlotDto>>> GetAvailableSlots(
        int apartmentId,
        [FromQuery] DateTime date)
    {
        var slots = await _appointmentService.GetAvailableSlotsAsync(apartmentId, date);
        return Ok(slots);
    }

    [HttpPut(ApiActionsV1.UpdateAppointmentStatus, Name = nameof(ApiActionsV1.UpdateAppointmentStatus))]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointmentStatus(
        int id,
        [FromBody] UpdateAppointmentStatusDto dto)
    {
        try
        {
            var appointment = await _appointmentService.UpdateAppointmentStatusAsync(id, dto);
            return Ok(appointment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete(ApiActionsV1.CancelAppointment, Name = nameof(ApiActionsV1.CancelAppointment))]
    public async Task<ActionResult> CancelAppointment(int id)
    {
        try
        {
            await _appointmentService.CancelAppointmentAsync(id);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet(ApiActionsV1.GetAppointmentById, Name = nameof(ApiActionsV1.GetAppointmentById))]
    public async Task<ActionResult<AppointmentDto>> GetAppointmentById(int id)
    {
        try
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found" });
            return Ok(appointment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet(ApiActionsV1.GetMyAvailability, Name = nameof(ApiActionsV1.GetMyAvailability))]
    public async Task<ActionResult<List<LandlordAvailabilityDto>>> GetMyAvailability()
    {
        var availability = await _appointmentService.GetMyAvailabilityAsync();
        return Ok(availability);
    }

    [HttpPut(ApiActionsV1.SetMyAvailability, Name = nameof(ApiActionsV1.SetMyAvailability))]
    public async Task<ActionResult<List<LandlordAvailabilityDto>>> SetMyAvailability([FromBody] SetAvailabilityDto dto)
    {
        var availability = await _appointmentService.SetMyAvailabilityAsync(dto);
        return Ok(availability);
    }
}
