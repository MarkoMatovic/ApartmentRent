using System.Data;
using Lander.Helpers;
using Lander.src.Modules.SavedSearches.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// Moved from LandLanderContext.cs (root mega-file) into per-module location.
// Namespace intentionally kept as 'Lander' to avoid breaking migrations,
// service registrations, and all existing consumers.
namespace Lander;

public class SavedSearchesContext : DbContext, IUnitOfWork
{
    public SavedSearchesContext(DbContextOptions<SavedSearchesContext> options)
        : base(options)
    { }
    private IDbContextTransaction? _currentTransaction;
    public DbSet<SavedSearch> SavedSearches { get; set; }

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
        modelBuilder.HasDefaultSchema("SavedSearches");
        modelBuilder.Entity<SavedSearch>(entity =>
        {
            entity.HasKey(e => e.SavedSearchId).HasName("PK__SavedSearches__SavedSearchId");
            entity.ToTable("SavedSearches", "SavedSearches");

            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.SearchType).HasMaxLength(50);
            entity.Property(e => e.FiltersJson).HasColumnType("nvarchar(max)");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.LastNotificationSent).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasIndex(e => e.UserId);
            // Notification query filter: WHERE IsActive = 1 AND EmailNotificationsEnabled = 1
            entity.HasIndex(e => new { e.IsActive, e.EmailNotificationsEnabled })
                  .HasDatabaseName("IX_SavedSearches_IsActive_EmailNotifications");
        });
    }
}
