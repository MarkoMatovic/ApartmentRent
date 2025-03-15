﻿using System.Data;using Lander.Helpers;using Lander.src.Modules.ApartmentApplications.Models;using Lander.src.Modules.Communication.Models;using Lander.src.Modules.Listings.Models;using Lander.src.Modules.Reviews.Modules;using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;using Lander.src.Notifications.Models;using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore.Storage;namespace Lander;


public class ApplicationsContext : DbContext{    public ApplicationsContext(DbContextOptions<ApplicationsContext> options)        : base(options)    { }    public DbSet<ApartmentApplication> ApartmentApplications { get; set; }    public DbSet<SearchPreference> SearchPreferences { get; set; }    protected override void OnModelCreating(ModelBuilder modelBuilder)    {
        modelBuilder.HasDefaultSchema("Applications");        modelBuilder.Entity<ApartmentApplication>(entity =>        {
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

           
            entity.HasOne(d => d.User).WithMany(p => p.ApartmentApplications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__ApartmentApplications__UserId");
        });        modelBuilder.Entity<SearchPreference>(entity =>        {
            entity.HasKey(e => e.PreferenceId).HasName("PK__SearchPr__E228496FC715AE79");            entity.ToTable("SearchPreferences", "Applications");

            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MaxRent).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.SearchPreferences)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__SearchPre__UserI__787EE5A0");
        });    }}
public partial class NotificationContext : DbContext
{
    public NotificationContext()
    {
    }

    public NotificationContext(DbContextOptions<NotificationContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<ReadNotification> ReadNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Notification");
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications", "Notification");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Title).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

public class CommunicationsContext : DbContext
{
    public CommunicationsContext(DbContextOptions<CommunicationsContext> options)
        : base(options)
    { }

    public DbSet<Message> Messages { get; set; }

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
                .HasConstraintName("FK__Messages__Receiv__6383C8BA");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders)
                .HasForeignKey(d => d.SenderId)
                .HasConstraintName("FK__Messages__Sender__628FA481");
        });
    }
}
public class ListingsContext : DbContext
{
    public ListingsContext(DbContextOptions<ListingsContext> options)
        : base(options)
    { }

    public DbSet<Apartment> Apartments { get; set; }
    public DbSet<ApartmentImage> ApartmentImages { get; set; }

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

            entity.HasOne(d => d.Landlord).WithMany(p => p.Apartments)
                .HasForeignKey(d => d.LandlordId)
                .HasConstraintName("FK__Apartment__Landl__59063A47");
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
                .HasConstraintName("FK__Apartment__Apart__5CD6CB2B");
        });


    }
}
public class ReviewsContext : DbContext
{
    public ReviewsContext(DbContextOptions<ReviewsContext> options)
        : base(options)
    { }

    public DbSet<Review> Reviews { get; set; }
    public DbSet<Favorite> Favorites { get; set; }

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

            entity.HasOne(d => d.Landlord).WithMany(p => p.ReviewLandlords)
                .HasForeignKey(d => d.LandlordId)
                .HasConstraintName("FK__Reviews__Landlor__6E01572D");

            entity.HasOne(d => d.Tenant).WithMany(p => p.ReviewTenants)
                .HasForeignKey(d => d.TenantId)
                .HasConstraintName("FK__Reviews__TenantI__6D0D32F4");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.FavoriteId).HasName("PK__Favorite__CE74FAD5F5FEA175");

            entity.ToTable("Favorites", "ReviewsFavorites");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

            //entity.HasOne(d => d.Apartment).WithMany(p => p.Favorites)
            //    .HasForeignKey(d => d.ApartmentId)
            //    .HasConstraintName("FK__Favorites__Apart__68487DD7");

            //entity.HasOne(d => d.User).WithMany(p => p.Favorites)
            //    .HasForeignKey(d => d.UserId)
            //    .HasConstraintName("FK__Favorites__UserI__6754599E");
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
                .HasConstraintName("FK__Users__UserRoleI__5535A963");
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





