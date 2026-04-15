using System.Data;
using Lander.Helpers;
using Lander.src.Modules.Reviews.Modules;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// Moved from LandLanderContext.cs (root mega-file) into per-module location.
// Namespace intentionally kept as 'Lander' to avoid breaking migrations,
// service registrations, and all existing consumers.
namespace Lander;

public class ReviewsContext : DbContext, IUnitOfWork
{
    public ReviewsContext(DbContextOptions<ReviewsContext> options)
        : base(options)
    { }
    private IDbContextTransaction? _currentTransaction;
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<User> Users { get; set; }

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
        modelBuilder.HasDefaultSchema("ReviewsFavorites");
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__74BC79CE27F3C467");

            entity.ToTable("Reviews", "ReviewsFavorites");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.ReviewText).HasColumnType("text");
            entity.Property(e => e.Rating).HasMaxLength(5);
            entity.Property(e => e.IsAnonymous).HasDefaultValue(false);
            entity.Property(e => e.IsPublic).HasDefaultValue(true);

            entity.HasOne(d => d.Landlord).WithMany(p => p.ReviewLandlords)
                .HasForeignKey(d => d.LandlordId)
                .HasConstraintName("FK__Reviews__Landlor__6E01572D")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Tenant).WithMany(p => p.ReviewTenants)
                .HasForeignKey(d => d.TenantId)
                .HasConstraintName("FK__Reviews__TenantI__6D0D32F4")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.FavoriteId).HasName("PK__Favorite__CE74FAD5F5FEA175");

            entity.ToTable("Favorites", "ReviewsFavorites");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

            entity.HasIndex(e => e.ApartmentId);
            entity.HasIndex(e => e.RoommateId);
            entity.HasIndex(e => e.SearchRequestId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.ApartmentId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.RoommateId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.SearchRequestId }).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "UsersRoles");
            entity.HasKey(e => e.UserId);
            entity.Metadata.SetIsTableExcludedFromMigrations(true);
        });
    }
}
