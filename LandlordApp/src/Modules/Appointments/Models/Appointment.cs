using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lander.src.Common;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;

namespace Lander.src.Modules.Appointments.Models
{
    public class Appointment : IAggregateRoot
    {
        [Key]
        public int AppointmentId { get; set; }
        
        public Guid AppointmentGuid { get; set; }
        
        [Required]
        public int ApartmentId { get; set; }
        
        [Required]
        public int TenantId { get; set; }
        
        [Required]
        public int LandlordId { get; set; }
        
        [Required]
        public DateTime AppointmentDate { get; set; }
        
        [Required]
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30); // Default: 30 minutes
        
        [Required]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        
        [MaxLength(500)]
        public string? TenantNotes { get; set; }
        
        [MaxLength(500)]
        public string? LandlordNotes { get; set; }
        
        public Guid? CreatedByGuid { get; set; }
        public DateTime CreatedDate { get; set; }
        
        public Guid? ModifiedByGuid { get; set; }
        public DateTime? ModifiedDate { get; set; }
        
        // Note: Navigation properties removed because Apartment and User entities
        // exist in different DbContexts (ListingsContext, UsersContext)
        // Use ApartmentId, TenantId, and LandlordId for relationships
    }
    
    public enum AppointmentStatus
    {
        Pending = 0,
        Confirmed = 1,
        Cancelled = 2,
        Completed = 3,
        Rejected = 4
    }
}
