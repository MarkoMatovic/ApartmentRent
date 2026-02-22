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

            entity.HasIndex(e => e.UserId);
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
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<ConversationSettings> ConversationSettings { get; set; }
    public DbSet<ReportedMessage> ReportedMessages { get; set; }

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

            entity.Property(e => e.FileUrl).HasMaxLength(500);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FileSize);
            entity.Property(e => e.FileType).HasMaxLength(50);

            // Note: Sender and Receiver navigation properties are ignored to avoid cross-schema FK constraints
            // User entities are in UsersRoles schema, not Communication schema
            entity.Ignore(e => e.Sender);
            entity.Ignore(e => e.Receiver);
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.HasKey(e => e.EmailLogId).HasName("PK__EmailLogs__C87C0C9D");
            entity.ToTable("EmailLogs", "Communication");

            entity.Property(e => e.RecipientEmail).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(500).IsRequired();
            entity.Property(e => e.HtmlContent).HasColumnType("text");
            entity.Property(e => e.TemplateId).HasMaxLength(100);
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDelivered).HasDefaultValue(false);
            entity.Property(e => e.SendGridMessageId).HasMaxLength(255);
            entity.Property(e => e.ErrorMessage).HasColumnType("text");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<ConversationSettings>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("PK__ConversationSettings__SettingId");
            entity.ToTable("ConversationSettings", "Communication");

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.OtherUserId).IsRequired();
            entity.Property(e => e.IsArchived).HasDefaultValue(false);
            entity.Property(e => e.IsMuted).HasDefaultValue(false);
            entity.Property(e => e.IsBlocked).HasDefaultValue(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

            // Unique constraint: one setting per user-otherUser pair
            entity.HasIndex(e => new { e.UserId, e.OtherUserId }).IsUnique();
        });

        modelBuilder.Entity<ReportedMessage>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__ReportedMessages__ReportId");
            entity.ToTable("ReportedMessages", "Communication");

            entity.Property(e => e.MessageId).IsRequired();
            entity.Property(e => e.ReportedByUserId).IsRequired();
            entity.Property(e => e.ReportedUserId).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReviewedDate).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.AdminNotes).HasColumnType("text");

            entity.HasOne(d => d.Message).WithMany()
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("FK__ReportedMessages__Message")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.MessageId);
            entity.HasIndex(e => e.ReportedByUserId);
            entity.HasIndex(e => e.ReportedUserId);
            entity.HasIndex(e => e.Status);
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
            
            entity.Property(e => e.IsFurnished).HasDefaultValue(false);
            entity.Property(e => e.HasBalcony).HasDefaultValue(false);
            entity.Property(e => e.HasElevator).HasDefaultValue(false);
            entity.Property(e => e.HasParking).HasDefaultValue(false);
            entity.Property(e => e.HasInternet).HasDefaultValue(false);
            entity.Property(e => e.HasAirCondition).HasDefaultValue(false);
            entity.Property(e => e.IsPetFriendly).HasDefaultValue(false);
            entity.Property(e => e.IsSmokingAllowed).HasDefaultValue(false);
            
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

public class UsersContext : DbContext, IUnitofWork
{
    public UsersContext(DbContextOptions<UsersContext> options)
        : base(options)
    { }
    private IDbContextTransaction? _currentTransaction;
    public DbSet<User> Users { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

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

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId })
                .HasName("PK__RolePerm__RolePermission");

            entity.ToTable("RolePermissions", "UsersRoles");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Role)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__RolePerm__RoleId");

            entity.HasOne(d => d.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__RolePerm__PermId");
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

            entity.HasIndex(e => e.UserId);

            // Phase 1: Performance Optimization Indexes
            entity.HasIndex(e => e.BudgetMin);
            entity.HasIndex(e => e.BudgetMax);
            entity.HasIndex(e => e.AvailableFrom);
            entity.HasIndex(e => e.CreatedDate);
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

public class AnalyticsContext : DbContext, IUnitofWork
{
    public AnalyticsContext(DbContextOptions<AnalyticsContext> options)
        : base(options)
    { }
    
    private IDbContextTransaction? _currentTransaction;
    public DbSet<Lander.src.Modules.Analytics.Models.AnalyticsEvent> AnalyticsEvents { get; set; }

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
        modelBuilder.HasDefaultSchema("Analytics");
        modelBuilder.Entity<Lander.src.Modules.Analytics.Models.AnalyticsEvent>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__AnalyticsEvents__EventId");
            entity.ToTable("AnalyticsEvents", "Analytics");

            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EventCategory).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.SearchQuery).HasMaxLength(500);
            entity.Property(e => e.MetadataJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.EventCategory);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedDate);
            entity.HasIndex(e => new { e.EventType, e.CreatedDate });
        });
    }
}





