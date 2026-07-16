using System.Linq.Expressions;

namespace Lander.src.Infrastructure.Services;

public interface IBackgroundScheduler
{
    void Enqueue<T>(Expression<Action<T>> job);
    void Enqueue<T>(Expression<Func<T, Task>> job);
    void Schedule<T>(TimeSpan delay, Expression<Action<T>> job);
}
