using Lander.src.Modules.Appointments.Models;

namespace Lander.src.Modules.Appointments.Dtos
{
    public class AppointmentDto
    {
        public int AppointmentId { get; set; }
        public Guid AppointmentGuid { get; set; }
        public int ApartmentId { get; set; }
        public string? ApartmentTitle { get; set; }
        public string? ApartmentAddress { get; set; }
        public int TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? TenantEmail { get; set; }
        public int LandlordId { get; set; }
        public string? LandlordName { get; set; }
        public string? LandlordEmail { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan Duration { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? TenantNotes { get; set; }
        public string? LandlordNotes { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
