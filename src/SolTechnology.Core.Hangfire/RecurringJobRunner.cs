using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Hangfire.Filters;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Resolves <typeparamref name="TJob"/> from a fresh DI scope and executes it.
/// Hangfire targets this class by expression; the activator resolves the singleton instance.
/// A thrown exception is not retried; a recoverable failed <see cref="Result"/> is retried up to
/// <see cref="RecurringJobRetry.RecoverableResultAttempts"/> times when <c>UseSolFilters</c> installed Smart Retry.
/// </summary>
internal sealed class RecurringJobRunner<TJob>(IServiceScopeFactory scopeFactory) where TJob : class, IJob
{
    [AutomaticRetry(Attempts = RecurringJobRetry.RecoverableResultAttempts, OnlyOn = [typeof(RecoverableResultException)])]
    public async Task<Result> RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<TJob>();
        return await job.Execute(cancellationToken);
    }
}
