using System.Data;
using Lander.Helpers;
using Lander.src.Modules.Billing.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lander.src.Modules.Billing;

/// <summary>Tenancies, their bills and who pays what (schema "Billing").</summary>
public class BillingContext : DbContext, IUnitOfWork
{
    public BillingContext(DbContextOptions<BillingContext> options) : base(options) { }

    private IDbContextTransaction? _currentTransaction;

    public DbSet<Tenancy> Tenancies { get; set; }
    public DbSet<TenancyMember> TenancyMembers { get; set; }
    public DbSet<Bill> Bills { get; set; }
    public DbSet<BillShare> BillShares { get; set; }
    public DbSet<BillingProfile> BillingProfiles { get; set; }

    public async Task<IDbContextTransaction?> BeginTransactionAsync()
    {
        if (_currentTransaction is not null) return null;
        _currentTransaction = await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(IDbContextTransaction? transaction)
    {
        if (transaction is null) throw new ArgumentNullException(nameof(transaction));
        if (transaction != _currentTransaction)
            throw new InvalidOperationException($"Transaction {transaction.TransactionId} is not current transaction.");

        try
        {
            await SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            RollBackTransaction();
            throw;
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public void RollBackTransaction()
    {
        try
        {
            _currentTransaction?.Rollback();
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default) =>
        base.SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Billing");

        modelBuilder.Entity<Tenancy>(entity =>
        {
            entity.HasKey(e => e.TenancyId);
            entity.ToTable("Tenancies");
            entity.Property(e => e.ApartmentTitle).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MonthlyRent).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PayeeName).HasMaxLength(70).IsRequired();
            entity.Property(e => e.PayeeAccount).HasMaxLength(18).IsRequired();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");

            entity.HasIndex(e => e.LandlordUserId);
            // One active tenancy per apartment; ended ones stay as history.
            entity.HasIndex(e => e.ApartmentId).IsUnique().HasFilter("[Status] = 0");
        });

        modelBuilder.Entity<TenancyMember>(entity =>
        {
            entity.HasKey(e => e.TenancyMemberId);
            entity.ToTable("TenancyMembers");
            entity.Property(e => e.JoinedDate).HasColumnType("datetime2");
            entity.Property(e => e.LeftDate).HasColumnType("datetime2");

            entity.HasOne(e => e.Tenancy)
                .WithMany(t => t.Members)
                .HasForeignKey(e => e.TenancyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            // A person is in a tenancy once at a time (they may come back after leaving).
            entity.HasIndex(e => new { e.TenancyId, e.UserId }).IsUnique().HasFilter("[LeftDate] IS NULL");
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.BillId);
            entity.ToTable("Bills");
            entity.Property(e => e.Kind).HasConversion<int>();
            entity.Property(e => e.PayTo).HasConversion<int>();
            entity.Property(e => e.Title).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Period).HasMaxLength(7);
            entity.Property(e => e.PayeeName).HasMaxLength(70).IsRequired();
            entity.Property(e => e.PayeeAccount).HasMaxLength(18).IsRequired();
            entity.Property(e => e.PaymentCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.PaymentModel).HasMaxLength(2);
            entity.Property(e => e.PaymentReference).HasMaxLength(22);
            entity.Property(e => e.Purpose).HasMaxLength(35).IsRequired();
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.PaidAt).HasColumnType("datetime2");
            entity.Property(e => e.CancelledAt).HasColumnType("datetime2");
            entity.Property(e => e.DueSoonNotifiedAt).HasColumnType("datetime2");
            entity.Property(e => e.OverdueNotifiedAt).HasColumnType("datetime2");
            // Two tenants marking the same company bill paid at once: only one of them wins.
            entity.Property(e => e.PaidAt).IsConcurrencyToken();

            entity.HasOne(e => e.Tenancy)
                .WithMany(t => t.Bills)
                .HasForeignKey(e => e.TenancyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenancyId, e.DueDate });
            // The monthly job may run twice: one rent bill per month.
            entity.HasIndex(e => new { e.TenancyId, e.Period }).IsUnique().HasFilter("[Period] IS NOT NULL");
        });

        modelBuilder.Entity<BillShare>(entity =>
        {
            entity.HasKey(e => e.BillShareId);
            entity.ToTable("BillShares");
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.ReceiverName).HasMaxLength(70).IsRequired();
            entity.Property(e => e.ReceiverAccount).HasMaxLength(18).IsRequired();
            // "Paid", "arrived" and "did not arrive" clicked at the same moment: the second one is refused.
            entity.Property(e => e.Status).HasConversion<int>().IsConcurrencyToken();
            entity.Property(e => e.MarkedPaidAt).HasColumnType("datetime2");
            entity.Property(e => e.ConfirmedAt).HasColumnType("datetime2");
            entity.Property(e => e.DueSoonNotifiedAt).HasColumnType("datetime2");
            entity.Property(e => e.OverdueNotifiedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.Bill)
                .WithMany(b => b.Shares)
                .HasForeignKey(e => e.BillId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.PayerUserId, e.Status });
            entity.HasIndex(e => new { e.ReceiverUserId, e.Status });
        });

        modelBuilder.Entity<BillingProfile>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.ToTable("BillingProfiles");
            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.PayoutName).HasMaxLength(70).IsRequired();
            entity.Property(e => e.PayoutAccount).HasMaxLength(18).IsRequired();
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime2");
        });
    }
}
