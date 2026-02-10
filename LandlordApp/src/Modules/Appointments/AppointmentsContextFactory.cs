using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lander.src.Modules.Appointments
{
    public class AppointmentsContextFactory : IDesignTimeDbContextFactory<AppointmentsContext>
    {
        public AppointmentsContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppointmentsContext>();
            optionsBuilder.UseSqlServer("Server=localhost;Database=LandLander;Trusted_Connection=True;TrustServerCertificate=True;");

            return new AppointmentsContext(optionsBuilder.Options);
        }
    }
}
