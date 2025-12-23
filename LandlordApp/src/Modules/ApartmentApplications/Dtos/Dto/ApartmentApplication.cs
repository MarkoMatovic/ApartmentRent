namespace Lander.src.Modules.ApartmentApplications.Dtos.Dto
{
    public class ApartmentApplicationDto
    {
        public int ApplicationId { get; set; }
        public int UserId { get; set; }
        public int ApartmentId { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string Status { get; set; }
        public Guid? CreatedByGuid { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? ModifiedByGuid { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
