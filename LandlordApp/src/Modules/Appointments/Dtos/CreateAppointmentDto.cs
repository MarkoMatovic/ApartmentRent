using System.ComponentModel.DataAnnotations;

namespace Lander.src.Modules.Appointments.Dtos
{
    public class CreateAppointmentDto
    {
        [Required]
        public int ApartmentId { get; set; }
        
        [Required]
        public DateTime AppointmentDate { get; set; }
        
        [MaxLength(500)]
        public string? TenantNotes { get; set; }
    }
}
