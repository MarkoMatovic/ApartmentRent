using Lander.src.Modules.Appointments.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Appointments
{
    public class AppointmentsContext : DbContext
    {
        public AppointmentsContext(DbContextOptions<AppointmentsContext> options) : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<LandlordAvailability> LandlordAvailabilities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Appointment configuration
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.ToTable("Appointments", "appointments");
                entity.HasKey(e => e.AppointmentId);
                entity.Property(e => e.AppointmentGuid).HasDefaultValueSql("NEWID()");
                entity.HasIndex(e => e.AppointmentGuid).IsUnique();
                entity.HasIndex(e => e.ApartmentId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.LandlordId);
                entity.HasIndex(e => e.AppointmentDate);
                entity.HasIndex(e => e.Status);
                
                // Note: Foreign keys to Apartment and Users are not enforced at DB level
                // because they exist in different DbContexts (ListingsContext, UsersContext)
                // Referential integrity is maintained at application level
            });

            // LandlordAvailability configuration
            modelBuilder.Entity<LandlordAvailability>(entity =>
            {
                entity.ToTable("LandlordAvailabilities", "appointments");
                entity.HasKey(e => e.AvailabilityId);
                entity.HasIndex(e => e.LandlordId);
                entity.HasIndex(e => new { e.LandlordId, e.DayOfWeek });
                
                // Note: Foreign key to User (Landlord) is not enforced at DB level
            });
        }

        public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            await base.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
