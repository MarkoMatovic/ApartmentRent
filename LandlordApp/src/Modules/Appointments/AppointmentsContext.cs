using Lander.Helpers;
using Lander.src.Modules.Appointments.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Lander.src.Modules.Appointments
{
    public class AppointmentsContext : DbContext, IUnitOfWork
    {
        private IDbContextTransaction? _currentTransaction;
        public AppointmentsContext(DbContextOptions<AppointmentsContext> options) : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<LandlordAvailability> LandlordAvailabilities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.ToTable("Appointments", "appointments");
                entity.HasKey(e => e.AppointmentId);
                entity.Property(e => e.AppointmentGuid).HasDefaultValueSql("NEWID()");
                entity.HasIndex(e => e.AppointmentGuid).IsUnique();
                entity.HasIndex(e => e.ApartmentId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.LandlordId);
                entity.HasIndex(e => e.AppointmentDate);
                entity.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<LandlordAvailability>(entity =>
            {
                entity.ToTable("LandlordAvailabilities", "appointments");
                entity.HasKey(e => e.AvailabilityId);
                entity.HasIndex(e => e.LandlordId);
                entity.HasIndex(e => new { e.LandlordId, e.DayOfWeek });
            });
        }

        // IUnitOfWork.SaveEntitiesAsync returns Task<int> (row count)
        public async Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default)
            => await base.SaveChangesAsync(cancellationToken);

        // IUnitOfWork.BeginTransactionAsync (no isolation level parameter on the interface)
        public async Task<IDbContextTransaction?> BeginTransactionAsync()
        {
            if (_currentTransaction is not null) return null;
            _currentTransaction = await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            return _currentTransaction;
        }

        /// <summary>Overload accepting an explicit isolation level (used internally by RunInTransactionAsync).</summary>
        public async Task<IDbContextTransaction?> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            if (_currentTransaction is not null) return null;
            _currentTransaction = await Database.BeginTransactionAsync(isolationLevel);
            return _currentTransaction;
        }

        public async Task CommitTransactionAsync(IDbContextTransaction? transaction)
        {
            if (transaction == null || transaction != _currentTransaction) return;
            try
            {
                await _currentTransaction.CommitAsync();
            }
            finally
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
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
                _currentTransaction?.Dispose();
                _currentTransaction = null;
            }
        }
    }
}
