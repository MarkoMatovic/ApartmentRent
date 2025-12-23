using Microsoft.EntityFrameworkCore.Storage;

namespace Lander.Helpers
{
    public interface IUnitofWork : IDisposable
    {
        Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<IDbContextTransaction?> BeginTransactionAsync();
        Task CommitTransactionAsync(IDbContextTransaction? transaction);
        void RollBackTransaction();
    }
}
