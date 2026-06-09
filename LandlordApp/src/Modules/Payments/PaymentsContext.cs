using Lander.src.Modules.Payments.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Payments
{
    public class PaymentsContext : DbContext
    {
        public PaymentsContext(DbContextOptions<PaymentsContext> options) : base(options)
        {
        }

        // Monri payment provider — idempotency table for processed callbacks
        public DbSet<ProcessedMonriOrder> ProcessedMonriOrders { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProcessedMonriOrder>(entity =>
            {
                entity.ToTable("ProcessedMonriOrders", "payments");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderNumber).IsUnique();
            });
        }

        public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            await base.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
