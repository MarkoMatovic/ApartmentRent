using System.ComponentModel.DataAnnotations;
using Lander.src.Modules.Appointments.Models;

namespace Lander.src.Modules.Appointments.Dtos
{
    public class UpdateAppointmentStatusDto
    {
        [Required]
        public AppointmentStatus Status { get; set; }
        
        [MaxLength(500)]
        public string? LandlordNotes { get; set; }
    }
}
