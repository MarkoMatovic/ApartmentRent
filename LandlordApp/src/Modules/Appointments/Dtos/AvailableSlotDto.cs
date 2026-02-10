namespace Lander.src.Modules.Appointments.Dtos
{
    public class AvailableSlotDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAvailable { get; set; }
    }
}
