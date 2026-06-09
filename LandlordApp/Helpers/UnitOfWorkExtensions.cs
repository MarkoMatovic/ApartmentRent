using System.Data;
using Lander.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lander.Helpers;

public static class UnitOfWorkExtensions
{
    /// <summary>
    /// Wraps <paramref name="action"/> in a retry-aware execution strategy + explicit
    /// transaction.  Named <c>RunInTransactionAsync</c> (not <c>RunInTransactionAsync</c>)
    /// to avoid collision with <c>RelationalDatabaseFacadeExtensions.RunInTransactionAsync</c>
    /// introduced in EF Core 10.
    /// </summary>
    public static Task RunInTransactionAsync(
        this IUnitOfWork unitOfWork,
        Func<Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        var strategy = unitOfWork.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(
            state: (unitOfWork, action),
            operation: static async (_, state, ct) =>
            {
                var (uow, act) = state;
                var transaction = await uow.BeginTransactionAsync();
                try
                {
                    await act();
                    await uow.CommitTransactionAsync(transaction);
                    return true;
                }
                catch
                {
                    uow.RollBackTransaction();
                    throw;
                }
            },
            verifySucceeded: null);
    }

    /// <summary>Value-returning overload of <see cref="RunInTransactionAsync"/>.</summary>
    public static Task<T> RunInTransactionAsync<T>(
        this IUnitOfWork unitOfWork,
        Func<Task<T>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        var strategy = unitOfWork.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(
            state: (unitOfWork, action),
            operation: static async (_, state, ct) =>
            {
                var (uow, act) = state;
                var transaction = await uow.BeginTransactionAsync();
                try
                {
                    var result = await act();
                    await uow.CommitTransactionAsync(transaction);
                    return result;
                }
                catch
                {
                    uow.RollBackTransaction();
                    throw;
                }
            },
            verifySucceeded: null);
    }
}
