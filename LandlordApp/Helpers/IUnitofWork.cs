using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lander.Helpers
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default);
        Task<IDbContextTransaction?> BeginTransactionAsync();
        Task CommitTransactionAsync(IDbContextTransaction? transaction);
        void RollBackTransaction();

        /// <summary>
        /// Exposes the EF Core DatabaseFacade so that extension methods can call
        /// Database.CreateExecutionStrategy() for retry-aware transaction wrapping.
        /// All DbContext implementations satisfy this automatically.
        /// </summary>
        DatabaseFacade Database { get; }
    }
}
