using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Resolves <typeparamref name="TJob"/> from a fresh DI scope and executes it.
/// Hangfire targets this class by expression; the activator resolves the singleton instance.
/// </summary>
internal sealed class RecurringJobRunner<TJob>(IServiceScopeFactory scopeFactory) where TJob : class, IJob
{
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TJob>();
        await job.Execute(cancellationToken);
    }
}

