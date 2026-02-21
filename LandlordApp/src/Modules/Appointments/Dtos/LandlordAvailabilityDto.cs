namespace Lander.src.Modules.Appointments.Dtos
{
    public class LandlordAvailabilityDto
    {
        public int AvailabilityId { get; set; }
        public int LandlordId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class AvailabilitySlotInput
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class SetAvailabilityDto
    {
        public List<AvailabilitySlotInput> Slots { get; set; } = new();
    }
}
