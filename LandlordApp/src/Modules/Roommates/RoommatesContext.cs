using System.Data;
using Lander.Helpers;
using Lander.src.Modules.Roommates.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// Moved from LandLanderContext.cs (root mega-file) into per-module location.
// Namespace intentionally kept as 'Lander' to avoid breaking migrations,
// service registrations, and all existing consumers.
namespace Lander;

public class RoommatesContext : DbContext, IUnitOfWork
{
    public RoommatesContext(DbContextOptions<RoommatesContext> options)
        : base(options)
    { }
    private IDbContextTransaction? _currentTransaction;
    public DbSet<Roommate> Roommates { get; set; }

    public async Task<IDbContextTransaction?> BeginTransactionAsync()
    {
        if (_currentTransaction is not null) return null;
        _currentTransaction = await Database.BeginTransactionAsync(isolationLevel: IsolationLevel.ReadCommitted);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(IDbContextTransaction? transaction)
    {
        if (transaction is null) throw new ArgumentNullException(paramName: nameof(transaction));
        if (transaction != _currentTransaction)
            throw new InvalidOperationException(message: $"Transaction {transaction?.TransactionId} is not current transaction.");

        try
        {
            await SaveChangesAsync();
            await transaction?.CommitAsync();
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

    public async Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken: cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Roommates");
        modelBuilder.Entity<Roommate>(entity =>
        {
            entity.HasKey(e => e.RoommateId).HasName("PK__Roommates__RoommateId");
            entity.ToTable("Roommates", "Roommates");

            entity.Property(e => e.Bio).HasColumnType("text");
            entity.Property(e => e.Hobbies).HasMaxLength(500);
            entity.Property(e => e.Profession).HasMaxLength(100);
            entity.Property(e => e.Lifestyle).HasMaxLength(50);
            entity.Property(e => e.Cleanliness).HasMaxLength(50);
            entity.Property(e => e.BudgetIncludes).HasMaxLength(500);
            entity.Property(e => e.LookingForRoomType).HasMaxLength(50);
            entity.Property(e => e.LookingForApartmentType).HasMaxLength(100);
            entity.Property(e => e.PreferredLocation).HasMaxLength(255);

            entity.Property(e => e.BudgetMin).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.BudgetMax).HasColumnType("decimal(10, 2)");

            entity.Property(e => e.AvailableFrom).HasColumnType("date");
            entity.Property(e => e.AvailableUntil).HasColumnType("date");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasIndex(e => e.UserId);

            // Phase 1: Performance Optimization Indexes
            entity.HasIndex(e => e.BudgetMin);
            entity.HasIndex(e => e.BudgetMax);
            entity.HasIndex(e => e.AvailableFrom);
            entity.HasIndex(e => e.CreatedDate);
        });
    }
}
