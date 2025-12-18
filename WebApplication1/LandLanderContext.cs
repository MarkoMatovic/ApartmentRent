using System.Data;
using Lander.Helpers;
using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Reviews.Modules;
using Lander.src.Modules.Roommates.Models;
using Lander.src.Modules.SearchRequests.Models;
using Lander.src.Modules.SavedSearches.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Notifications.Models;
using Lander.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lander;


public class ApplicationsContext : DbContext, IUnitofWork
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

            // User je cross-context - navigacioni property je NotMapped
            // Foreign key constraint će biti kreiran ručno u migraciji ka UsersRoles.Users
            entity.HasIndex(e => e.UserId);
            
            // ApartmentId je cross-context foreign key - navigacioni property je NotMapped u modelu
            // Foreign key constraint će biti kreiran ručno u migraciji ka Listings.Apartments
            entity.HasIndex(e => e.ApartmentId);
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
    }
}
public partial class NotificationContext : DbContext, IUnitofWork
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

            // Cross-context foreign keys - navigacioni property-je nisu definisani
            entity.HasIndex(e => e.RecipientUserId);
            entity.HasIndex(e => e.SenderUserId);
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

public class CommunicationsContext : DbContext, IUnitofWork
{
    public CommunicationsContext(DbContextOptions<CommunicationsContext> options)
        : base(options)
    { }
    private IDbContextTransaction? _currentTransaction;
    public DbSet<Message> Messages { get; set; }

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
        modelBuilder.HasDefaultSchema("Communication");
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Messages__C87C0C9CEDE67554");

            entity.ToTable("Messages", "Communication");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.MessageText).HasColumnType("text");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Receiver).WithMany(p => p.MessageReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .HasConstraintName("FK__Messages__Receiv__6383C8BA")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders)
                .HasForeignKey(d => d.SenderId)
                .HasConstraintName("FK__Messages__Sender__628FA481")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
public class ListingsContext : DbContext, IUnitofWork
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

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.Rent).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.AvailableFrom).HasColumnType("date");
            entity.Property(e => e.AvailableUntil).HasColumnType("date");
            entity.Property(e => e.NumberOfRooms);
            entity.Property(e => e.RentIncludeUtilities).HasDefaultValue(false);
            
            // Location (for maps & search)
            entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)");
            
            // Apartment characteristics (filters)
            entity.Property(e => e.SizeSquareMeters);
            entity.Property(e => e.ApartmentType)
                .HasConversion<int>();
            
            // Furnishing & amenities
            entity.Property(e => e.IsFurnished).HasDefaultValue(false);
            entity.Property(e => e.HasBalcony).HasDefaultValue(false);
            entity.Property(e => e.HasElevator).HasDefaultValue(false);
            entity.Property(e => e.HasParking).HasDefaultValue(false);
            entity.Property(e => e.HasInternet).HasDefaultValue(false);
            entity.Property(e => e.HasAirCondition).HasDefaultValue(false);
            
            // Rules
            entity.Property(e => e.IsPetFriendly).HasDefaultValue(false);
            entity.Property(e => e.IsSmokingAllowed).HasDefaultValue(false);
            
            // Availability & rental terms
            entity.Property(e => e.DepositAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.MinimumStayMonths);
            entity.Property(e => e.MaximumStayMonths);
            entity.Property(e => e.IsImmediatelyAvailable).HasDefaultValue(false);

            // Landlord (User) je cross-context - navigacioni property je NotMapped
            // Foreign key constraint će biti kreiran ručno u migraciji ka UsersRoles.Users
            entity.HasIndex(e => e.LandlordId);
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
            entity.HasIndex(e => e.ApartmentType);
            entity.HasIndex(e => e.IsImmediatelyAvailable);
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
public class ReviewsContext : DbContext, IUnitofWork
{
    public ReviewsContext(DbContextOptions<ReviewsContext> options)
        : base(options)
    { }
    private IDbContextTransaction? _currentTransaction;
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Favorite> Favorites { get; set; }

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

            // Cross-context foreign keys - navigacioni property-je su NotMapped u modelima
            // Foreign key constraint-e će biti kreirani kroz migracije
            // ApartmentId, RoommateId, SearchRequestId i UserId će biti indeksirani za performanse
            entity.HasIndex(e => e.ApartmentId);
            entity.HasIndex(e => e.RoommateId);
            entity.HasIndex(e => e.SearchRequestId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.ApartmentId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.RoommateId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.SearchRequestId }).IsUnique();
        });
    }
}

public class UsersContext : DbContext, IUnitofWork
{
    public UsersContext(DbContextOptions<UsersContext> options)
        : base(options)
    { }
    private IDbContextTransaction? _currentTransaction;
    public DbSet<User> Users { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Role> Roles { get; set; }

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
        modelBuilder.HasDefaultSchema("UsersRoles");
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C6C2B7A29");

            entity.ToTable("Users", "UsersRoles");

            entity.HasIndex(e => e.UserGuid, "UQ__Users__99B7F23B67B93C73").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105343BBC7EAE").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.ProfilePicture).HasMaxLength(255);
            entity.Property(e => e.UserGuid).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.UserRole).WithMany(p => p.Users)
                .HasForeignKey(d => d.UserRoleId)
                .HasConstraintName("FK__Users__UserRoleI__5535A963")
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(u => u.MessageSenders)
      .WithOne(m => m.Sender) 
      .HasForeignKey(m => m.SenderId)
      .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.MessageReceivers)
                .WithOne(m => m.Receiver) 
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PK__Permissi__EFA6FB2F8C949F07");

            entity.ToTable("Permissions", "UsersRoles");

            entity.HasIndex(e => e.PermissionName, "UQ__Permissi__0FFDA357FFAF5633").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.PermissionName).HasMaxLength(100);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AF7E91F2B");

            entity.ToTable("Roles", "UsersRoles");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160CB8B6873").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.RoleName).HasMaxLength(100);
        });
    }
}

public class RoommatesContext : DbContext, IUnitofWork
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

            // Cross-context foreign key - UserId references UsersRoles.Users
            entity.HasIndex(e => e.UserId);
        });
    }
}

public class SearchRequestsContext : DbContext, IUnitofWork
{
    public SearchRequestsContext(DbContextOptions<SearchRequestsContext> options)
        : base(options)
    { }
    private IDbContextTransaction? _currentTransaction;
    public DbSet<SearchRequest> SearchRequests { get; set; }

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
        modelBuilder.HasDefaultSchema("SearchRequests");
        modelBuilder.Entity<SearchRequest>(entity =>
        {
            entity.HasKey(e => e.SearchRequestId).HasName("PK__SearchRequests__SearchRequestId");
            entity.ToTable("SearchRequests", "SearchRequests");

            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.PreferredLocation).HasMaxLength(255);
            entity.Property(e => e.PreferredLifestyle).HasMaxLength(50);
            
            entity.Property(e => e.BudgetMin).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.BudgetMax).HasColumnType("decimal(10, 2)");
            
            entity.Property(e => e.AvailableFrom).HasColumnType("date");
            entity.Property(e => e.AvailableUntil).HasColumnType("date");
            
            entity.Property(e => e.RequestType)
                .HasConversion<int>();
            
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // Cross-context foreign key - UserId references UsersRoles.Users
            entity.HasIndex(e => e.UserId);
        });
    }
}

public class SavedSearchesContext : DbContext, IUnitofWork
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
        });
    }
}





