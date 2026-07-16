using System.Linq.Expressions;
using Hangfire;

namespace Lander.src.Infrastructure.Services;

public sealed class HangfireBackgroundScheduler : IBackgroundScheduler
{
    public void Enqueue<T>(Expression<Action<T>> job) => BackgroundJob.Enqueue(job);
    public void Enqueue<T>(Expression<Func<T, Task>> job) => BackgroundJob.Enqueue(job);
    public void Schedule<T>(TimeSpan delay, Expression<Action<T>> job) => BackgroundJob.Schedule(job, delay);
}
