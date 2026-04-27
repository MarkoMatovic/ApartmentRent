using Lander.src.Modules.Payments.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Payments
{
    public class PaymentsContext : DbContext
    {
        public PaymentsContext(DbContextOptions<PaymentsContext> options) : base(options)
        {
        }

        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<ProcessedMonriOrder> ProcessedMonriOrders { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.ToTable("Subscriptions", "payments");
                entity.HasKey(e => e.SubscriptionId);
                entity.Property(e => e.SubscriptionGuid).HasDefaultValueSql("NEWID()");
                entity.HasIndex(e => e.SubscriptionGuid).IsUnique();
                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("Transactions", "payments");
                entity.HasKey(e => e.TransactionId);
                entity.Property(e => e.TransactionGuid).HasDefaultValueSql("NEWID()");
                entity.HasIndex(e => e.TransactionGuid).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.PaytenTransactionId);
            });

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
