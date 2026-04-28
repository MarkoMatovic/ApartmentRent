using System.Data;
using Lander.Helpers;
using Lander.src.Modules.Listings.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// Moved from LandLanderContext.cs (root mega-file) into per-module location.
// Namespace intentionally kept as 'Lander' to avoid breaking migrations,
// service registrations, and all existing consumers.
namespace Lander;

public class ListingsContext : DbContext, IUnitOfWork
{
    public ListingsContext(DbContextOptions<ListingsContext> options)
        : base(options)
    { }
    private IDbContextTransaction? _currentTransaction;
    public DbSet<Apartment> Apartments { get; set; }
    public DbSet<ApartmentImage> ApartmentImages { get; set; }

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
        modelBuilder.HasDefaultSchema("Listings");
        modelBuilder.Entity<Apartment>(entity =>
        {
            entity.HasKey(e => e.ApartmentId).HasName("PK__Apartmen__CBDF57642DC4E9F1");

            entity.ToTable("Apartments", "Listings");

            // .NET 10 Feature: Named Query Filter for soft deletes
            entity.HasQueryFilter(a => !a.IsDeleted && a.IsActive);

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.Rent).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.AvailableFrom).HasColumnType("date");
            entity.Property(e => e.AvailableUntil).HasColumnType("date");
            entity.Property(e => e.NumberOfRooms);
            entity.Property(e => e.RentIncludeUtilities).HasDefaultValue(false);

            entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)");

            entity.Property(e => e.SizeSquareMeters);
            entity.Property(e => e.ApartmentType)
                .HasConversion<int>();

            entity.Property(e => e.Features)
                .IsRequired()
                .HasDefaultValue("{}");

            entity.Property(e => e.DepositAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.MinimumStayMonths);
            entity.Property(e => e.MaximumStayMonths);
            entity.Property(e => e.IsImmediatelyAvailable).HasDefaultValue(false);

            entity.HasIndex(e => e.LandlordId);
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
            entity.HasIndex(e => e.ApartmentType);
            entity.HasIndex(e => e.IsImmediatelyAvailable);

            // Phase 1: Performance Optimization Indexes
            entity.HasIndex(e => e.City);
            entity.HasIndex(e => e.Rent);
            entity.HasIndex(e => e.Price);
            entity.HasIndex(e => e.NumberOfRooms);
            entity.HasIndex(e => e.ListingType);
            entity.HasIndex(e => e.CreatedDate);

            // Phase 2: Composite indexes for global query filter + common sorts
            // The EF query filter (!IsDeleted && IsActive) executes on every query —
            // without a covering index SQL Server does a full table scan first.
            entity.HasIndex(e => new { e.IsActive, e.IsDeleted })
                  .HasDatabaseName("IX_Apartments_IsActive_IsDeleted");
            entity.HasIndex(e => new { e.IsActive, e.IsDeleted, e.CreatedDate })
                  .HasDatabaseName("IX_Apartments_IsActive_IsDeleted_CreatedDate");
            entity.HasIndex(e => new { e.IsActive, e.IsDeleted, e.City })
                  .HasDatabaseName("IX_Apartments_IsActive_IsDeleted_City");
            // IsFeatured is used in ORDER BY on every listing query — needs its own index
            entity.HasIndex(e => new { e.IsFeatured, e.IsActive, e.IsDeleted, e.CreatedDate })
                  .HasDatabaseName("IX_Apartments_IsFeatured_IsActive_IsDeleted_CreatedDate");
        });

        modelBuilder.Entity<ApartmentImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__Apartmen__7516F70CD829ACA5");

            entity.ToTable("ApartmentImages", "Listings");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Apartment).WithMany(p => p.ApartmentImages)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK__Apartment__Apart__5CD6CB2B")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
