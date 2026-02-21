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
            try
            {
                _logger.LogInformation("Creating appointment for ApartmentId: {ApartmentId}, Date: {Date}", 
                    dto.ApartmentId, dto.AppointmentDate);
                    
                var appointment = await _appointmentService.CreateAppointmentAsync(dto);
                return Ok(appointment);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating appointment: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Conflict error creating appointment: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating appointment for ApartmentId: {ApartmentId}, Date: {Date}. Error: {ErrorMessage}", 
                    dto.ApartmentId, dto.AppointmentDate, ex.Message);
                return StatusCode(500, new { message = "An error occurred while creating the appointment", details = ex.Message });
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
                _logger.LogError(ex, "Error getting my appointments");
                return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
            }
        }

        [HttpGet("landlord-appointments")]
        public async Task<ActionResult<List<AppointmentDto>>> GetLandlordAppointments()
        {
            try
            {
                _logger.LogInformation("GetLandlordAppointments endpoint called");
                var appointments = await _appointmentService.GetLandlordAppointmentsAsync();
                _logger.LogInformation("Returning {Count} landlord appointments", appointments.Count);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting landlord appointments");
                return StatusCode(500, new { message = "An error occurred while retrieving landlord appointments" });
            }
        }

        [HttpGet("available-slots/{apartmentId}")]
        public async Task<ActionResult<List<AvailableSlotDto>>> GetAvailableSlots(
            int apartmentId,
            [FromQuery] string date)
        {
            try
            {
                if (string.IsNullOrEmpty(date))
                {
                    return BadRequest(new { message = "Date parameter is required" });
                }

                if (!DateTime.TryParse(date, out var parsedDate))
                {
                    return BadRequest(new { message = "Invalid date format. Use yyyy-MM-dd" });
                }

                var slots = await _appointmentService.GetAvailableSlotsAsync(apartmentId, parsedDate);
                return Ok(slots);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available slots for apartment {ApartmentId}, date: {Date}", apartmentId, date);
                return StatusCode(500, new { message = "An error occurred while retrieving available slots" });
            }
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment status for appointment {AppointmentId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the appointment" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> CancelAppointment(int id)
        {
            try
            {
                await _appointmentService.CancelAppointmentAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", id);
                return StatusCode(500, new { message = "An error occurred while cancelling the appointment" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AppointmentDto>> GetAppointmentById(int id)
        {
            try
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
                if (appointment == null)
                {
                    return NotFound(new { message = "Appointment not found" });
                }
                return Ok(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointment {AppointmentId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the appointment" });
            }
        }

        [HttpGet("availability")]
        public async Task<ActionResult<List<LandlordAvailabilityDto>>> GetMyAvailability()
        {
            try
            {
                var availability = await _appointmentService.GetMyAvailabilityAsync();
                return Ok(availability);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting landlord availability");
                return StatusCode(500, new { message = "An error occurred while retrieving availability" });
            }
        }

        [HttpPut("availability")]
        public async Task<ActionResult<List<LandlordAvailabilityDto>>> SetMyAvailability([FromBody] SetAvailabilityDto dto)
        {
            try
            {
                var availability = await _appointmentService.SetMyAvailabilityAsync(dto);
                return Ok(availability);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting landlord availability");
                return StatusCode(500, new { message = "An error occurred while saving availability" });
            }
        }
    }
}
