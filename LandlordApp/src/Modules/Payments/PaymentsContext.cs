using Lander.src.Modules.Payments.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Payments
{
    public class PaymentsContext : DbContext
    {
        public PaymentsContext(DbContextOptions<PaymentsContext> options) : base(options)
        {
        }

        // Idempotency table for processed payment orders (provider-agnostic).
        public DbSet<ProcessedOrder> ProcessedOrders { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProcessedOrder>(entity =>
            {
                // Table name kept as-is to avoid a migration.
                entity.ToTable("ProcessedMonriOrders", "payments");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.Property(e => e.PlanId).HasMaxLength(100);
                entity.Property(e => e.ReversalReference).HasMaxLength(100);
                entity.HasIndex(e => e.UserId); // payment-history lookup by buyer
            });
        }

        public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            await base.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
