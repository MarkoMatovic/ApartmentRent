namespace Lander.src.Modules.ApartmentApplications.Dtos.Dto
{
    public class ApartmentApplicationDto
    {
        public int ApplicationId { get; set; }
        public int? UserId { get; set; }
        public int? ApartmentId { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public string? Status { get; set; }
        public Guid? CreatedByGuid { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid? ModifiedByGuid { get; set; }
        public DateTime? ModifiedDate { get; set; }
        
        // Nested apartment details
        public ApartmentDetailsDto? Apartment { get; set; }
        
        // Nested user (tenant) details
        public UserDetailsDto? User { get; set; }
    }

    public class ApartmentDetailsDto
    {
        public int ApartmentId { get; set; }
        public string? Title { get; set; }
        public string? City { get; set; }
        public decimal? Rent { get; set; }
    }

    public class UserDetailsDto
    {
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }
}
