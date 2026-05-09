namespace Lander.Helpers;

public static class UnitOfWorkExtensions
{
    /// <summary>
    /// Wraps <paramref name="action"/> in a begin/commit/rollback transaction,
    /// eliminating the 3-line try/catch pattern duplicated throughout the codebase.
    /// </summary>
    public static async Task ExecuteInTransactionAsync(this IUnitOfWork unitOfWork, Func<Task> action)
    {
        var transaction = await unitOfWork.BeginTransactionAsync();
        try
        {
            await action();
            await unitOfWork.CommitTransactionAsync(transaction);
        }
        catch
        {
            unitOfWork.RollBackTransaction();
            throw;
        }
    }
}
