using Lander.src.Modules.Appointments.Dtos;
using Lander.src.Modules.Appointments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Appointments.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(
            IAppointmentService appointmentService,
            ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentDto dto)
        {
            _logger.LogInformation("Creating appointment for ApartmentId: {ApartmentId}, Date: {Date}",
                dto.ApartmentId, dto.AppointmentDate);
            try
            {
                var appointment = await _appointmentService.CreateAppointmentAsync(dto);
                return Ok(appointment);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating appointment");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("my-appointments")]
        public async Task<ActionResult<List<AppointmentDto>>> GetMyAppointments()
        {
            try
            {
                var appointments = await _appointmentService.GetMyAppointmentsAsync();
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching appointments");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("landlord-appointments")]
        public async Task<ActionResult<List<AppointmentDto>>> GetLandlordAppointments()
        {
            _logger.LogInformation("GetLandlordAppointments endpoint called");
            try
            {
                var appointments = await _appointmentService.GetLandlordAppointmentsAsync();
                _logger.LogInformation("Returning {Count} landlord appointments", appointments.Count);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching landlord appointments");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("available-slots/{apartmentId}")]
        [AllowAnonymous] // TEMP: k6 testing
        [Microsoft.AspNetCore.RateLimiting.DisableRateLimiting] // TEMP: k6 testing
        public async Task<ActionResult<List<AvailableSlotDto>>> GetAvailableSlots(
            int apartmentId,
            [FromQuery] DateTime date)
        {
            try
            {
                var slots = await _appointmentService.GetAvailableSlotsAsync(apartmentId, date);
                return Ok(slots);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<AppointmentDto>> UpdateAppointmentStatus(
            int id,
            [FromBody] UpdateAppointmentStatusDto dto)
        {
            try
            {
                var appointment = await _appointmentService.UpdateAppointmentStatusAsync(id, dto);
                return Ok(appointment);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> CancelAppointment(int id)
        {
            try
            {
                await _appointmentService.CancelAppointmentAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AppointmentDto>> GetAppointmentById(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found" });
            return Ok(appointment);
        }

        [HttpGet("availability")]
        public async Task<ActionResult<List<LandlordAvailabilityDto>>> GetMyAvailability()
        {
            try
            {
                var availability = await _appointmentService.GetMyAvailabilityAsync();
                return Ok(availability);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        }

        [HttpPut("availability")]
        public async Task<ActionResult<List<LandlordAvailabilityDto>>> SetMyAvailability([FromBody] SetAvailabilityDto dto)
        {
            try
            {
                var availability = await _appointmentService.SetMyAvailabilityAsync(dto);
                return Ok(availability);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        }
    }
}
