using System.Data;
using Lander.Helpers;
using Lander.src.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;


namespace Lander;

public partial class NotificationContext : DbContext, IUnitOfWork
{
    public NotificationContext()
    {
    }

    public NotificationContext(DbContextOptions<NotificationContext> options)
        : base(options)
    {
    }
    private IDbContextTransaction? _currentTransaction;
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<ReadNotification> ReadNotifications { get; set; }

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
        modelBuilder.HasDefaultSchema("Notification");
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Notifications", "Notification");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.ActionTarget).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.SenderUserId);
            entity.Property(e => e.RecipientUserId);
            entity.Property(e => e.CreatedByGuid);

            entity.HasIndex(e => e.RecipientUserId);
            entity.HasIndex(e => e.SenderUserId);
            // Covers: GetUserNotifications (RecipientUserId + IsRead + CreatedDate DESC)
            entity.HasIndex(e => new { e.RecipientUserId, e.IsRead, e.CreatedDate })
                  .HasDatabaseName("IX_Notifications_RecipientUserId_IsRead_CreatedDate");
        });

        modelBuilder.Entity<ReadNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ReadNotifications", "Notification");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.ActionTarget).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.SenderUserId);
            entity.Property(e => e.RecipientUserId);
            entity.Property(e => e.CreatedByGuid);

            entity.HasIndex(e => e.RecipientUserId);
            entity.HasIndex(e => e.SenderUserId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
