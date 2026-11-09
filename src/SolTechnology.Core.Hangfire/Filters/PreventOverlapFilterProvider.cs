using Hangfire.Common;

namespace SolTechnology.Core.Hangfire.Filters;

/// <summary>
/// Applies <see cref="PreventOverlapJobFilter"/> to the runners of recurring jobs registered with
/// <c>preventOverlap: true</c>; every other job gets no filter from this provider.
/// </summary>
internal sealed class PreventOverlapFilterProvider(IEnumerable<RecurringJobDescriptor> descriptors) : IJobFilterProvider
{
    private static readonly JobFilter[] OverlapFilter = [new(new PreventOverlapJobFilter(), JobFilterScope.Method, null)];

    private readonly HashSet<Type> _guardedRunnerTypes = descriptors
        .Where(descriptor => descriptor.PreventOverlap)
        .Select(descriptor => typeof(RecurringJobRunner<>).MakeGenericType(descriptor.JobType))
        .ToHashSet();

    public bool IsInstalled { get; private set; }

    public IEnumerable<JobFilter> GetFilters(Job? job) =>
        job is not null && _guardedRunnerTypes.Contains(job.Type) ? OverlapFilter : [];

    /// <summary>Adds this provider to <paramref name="providers"/>, replacing one added by an earlier host.</summary>
    public void InstallInto(JobFilterProviderCollection providers)
    {
        foreach (var earlier in providers.OfType<PreventOverlapFilterProvider>().ToList())
        {
            providers.Remove(earlier);
        }

        providers.Add(this);
        IsInstalled = true;
    }
}
