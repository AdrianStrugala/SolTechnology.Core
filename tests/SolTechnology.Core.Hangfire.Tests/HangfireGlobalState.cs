using Hangfire;
using Hangfire.Common;
using Hangfire.Logging;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Hangfire.Filters;

namespace SolTechnology.Core.Hangfire.Tests;

/// <summary>
/// Undoes what <c>AddHangfire</c> and <c>UseSolFilters</c> write into Hangfire's process-wide
/// configuration, so a disposed test provider does not leak into later tests.
/// </summary>
internal static class HangfireGlobalState
{
    public static void Reset(IServiceProvider provider)
    {
        // AddHangfire points Hangfire's static log provider at the container's ILoggerFactory.
        LogProvider.SetCurrentLogProvider(null);

        foreach (var filter in new object?[] { provider.GetService<CorrelationIdJobFilter>(), provider.GetService<SmartRetryJobFilter>(), provider.GetService<ChronicleJobFilter>() })
        {
            if (filter is not null)
            {
                GlobalJobFilters.Filters.Remove(filter);
            }
        }

        if (provider.GetService<PreventOverlapFilterProvider>() is { } overlapFilterProvider)
        {
            JobFilterProviders.Providers.Remove(overlapFilterProvider);
        }
    }
}
