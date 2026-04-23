using System.Data;
using Lander.Helpers;
using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// Moved from LandLanderContext.cs (root mega-file) into per-module location.
// Namespace intentionally kept as 'Lander' to avoid breaking migrations,
// service registrations, and all existing consumers.
namespace Lander;

public class ApplicationsContext : DbContext, IUnitOfWork
{
    public ApplicationsContext(DbContextOptions<ApplicationsContext> options)
        : base(options)
    { }
    private IDbContextTransaction? _currentTransaction;
    public DbSet<ApartmentApplication> ApartmentApplications { get; set; }
    public DbSet<SearchPreference> SearchPreferences { get; set; }

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
        modelBuilder.HasDefaultSchema("Applications");
        modelBuilder.Entity<ApartmentApplication>(entity =>
        {
            entity.HasKey(e => e.ApplicationId).HasName("PK__ApartmentApplications__C93A4C99A9D487DE");
            entity.ToTable("ApartmentApplications", "Applications");

            entity.Property(e => e.ApplicationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.ApartmentId).HasColumnName("ApartmentId");
            entity.Property(e => e.IsPriority).HasDefaultValue(false);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ApartmentId);
            entity.HasIndex(e => new { e.UserId, e.ApartmentId }).IsUnique()
                  .HasFilter("[UserId] IS NOT NULL AND [ApartmentId] IS NOT NULL");
        });

        modelBuilder.Entity<SearchPreference>(entity =>
        {
            entity.HasKey(e => e.PreferenceId).HasName("PK__SearchPr__E228496FC715AE79");
            entity.ToTable("SearchPreferences", "Applications");

            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MaxRent).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.SearchPreferences)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__SearchPre__UserI__787EE5A0")
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Exclude User from migrations in this context as it belongs to UsersContext
        modelBuilder.Entity<User>().ToTable("Users", "UsersRoles").Metadata.SetIsTableExcludedFromMigrations(true);
        // Also exclude Role/Permission which might be brought in via User
        modelBuilder.Entity<Role>().ToTable("Roles", "UsersRoles").Metadata.SetIsTableExcludedFromMigrations(true);
        modelBuilder.Entity<Permission>().ToTable("Permissions", "UsersRoles").Metadata.SetIsTableExcludedFromMigrations(true);
        modelBuilder.Entity<RolePermission>().ToTable("RolePermissions", "UsersRoles").Metadata.SetIsTableExcludedFromMigrations(true);
    }
}
