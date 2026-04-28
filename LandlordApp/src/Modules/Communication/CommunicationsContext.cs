using System.Data;
using Lander.Helpers;
using Lander.src.Modules.Communication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// Moved from LandLanderContext.cs (root mega-file) into per-module location.
// Namespace intentionally kept as 'Lander' to avoid breaking migrations,
// service registrations, and all existing consumers.
namespace Lander;

public class CommunicationsContext : DbContext, IUnitOfWork
{
    public CommunicationsContext(DbContextOptions<CommunicationsContext> options)
        : base(options)
    { }
    private IDbContextTransaction? _currentTransaction;
    public DbSet<Message> Messages { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<ConversationSettings> ConversationSettings { get; set; }
    public DbSet<ReportedMessage> ReportedMessages { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

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
                .HasDefaultValueSql("(GETUTCDATE())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.MessageText).HasColumnType("text");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(GETUTCDATE())")
                .HasColumnType("datetime");

            entity.Property(e => e.FileUrl).HasMaxLength(500);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FileSize);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.IsSuperLike).HasDefaultValue(false);

            // Note: Sender and Receiver navigation properties are ignored to avoid cross-schema FK constraints
            // User entities are in UsersRoles schema, not Communication schema
            entity.Ignore(e => e.Sender);
            entity.Ignore(e => e.Receiver);

            // Composite indexes for conversation queries (covers GetConversation + GetUserConversations)
            entity.HasIndex(e => new { e.SenderId, e.ReceiverId, e.SentAt })
                  .HasDatabaseName("IX_Messages_SenderId_ReceiverId_SentAt");
            entity.HasIndex(e => new { e.ReceiverId, e.IsRead })
                  .HasDatabaseName("IX_Messages_ReceiverId_IsRead");
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
            entity.Property(e => e.ProviderMessageId).HasMaxLength(255);
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

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("OutboxMessages", "Communication");
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Payload).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime");
            entity.Property(e => e.Error).HasColumnType("nvarchar(max)");
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.HasIndex(e => e.ProcessedAt); // fast unprocessed query
        });
    }
}
