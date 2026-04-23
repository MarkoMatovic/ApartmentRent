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
            var appointment = await _appointmentService.CreateAppointmentAsync(dto);
            return Ok(appointment);
        }

        [HttpGet("my-appointments")]
        public async Task<ActionResult<List<AppointmentDto>>> GetMyAppointments()
        {
            var appointments = await _appointmentService.GetMyAppointmentsAsync();
            return Ok(appointments);
        }

        [HttpGet("landlord-appointments")]
        public async Task<ActionResult<List<AppointmentDto>>> GetLandlordAppointments()
        {
            _logger.LogInformation("GetLandlordAppointments endpoint called");
            var appointments = await _appointmentService.GetLandlordAppointmentsAsync();
            _logger.LogInformation("Returning {Count} landlord appointments", appointments.Count);
            return Ok(appointments);
        }

        [HttpGet("available-slots/{apartmentId}")]
        [AllowAnonymous] // TEMP: k6 testing
        [Microsoft.AspNetCore.RateLimiting.DisableRateLimiting] // TEMP: k6 testing
        public async Task<ActionResult<List<AvailableSlotDto>>> GetAvailableSlots(
            int apartmentId,
            [FromQuery] DateTime date)
        {
            var slots = await _appointmentService.GetAvailableSlotsAsync(apartmentId, date);
            return Ok(slots);
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<AppointmentDto>> UpdateAppointmentStatus(
            int id,
            [FromBody] UpdateAppointmentStatusDto dto)
        {
            var appointment = await _appointmentService.UpdateAppointmentStatusAsync(id, dto);
            return Ok(appointment);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> CancelAppointment(int id)
        {
            await _appointmentService.CancelAppointmentAsync(id);
            return NoContent();
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
            var availability = await _appointmentService.GetMyAvailabilityAsync();
            return Ok(availability);
        }

        [HttpPut("availability")]
        public async Task<ActionResult<List<LandlordAvailabilityDto>>> SetMyAvailability([FromBody] SetAvailabilityDto dto)
        {
            var availability = await _appointmentService.SetMyAvailabilityAsync(dto);
            return Ok(availability);
        }
    }
}
