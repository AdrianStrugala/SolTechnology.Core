using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace SolTechnology.Core.Hangfire.Tests;

/// <summary>Starts the recurring job registrar of one application against a shared storage (in-memory by default).</summary>
internal sealed class RecurringJobHost : IDisposable
{
    public const string Cron = "0 0 * * *";

    private readonly JobStorage _storage;
    private readonly RecurringJobManager _manager;

    public RecurringJobHost(JobStorage? storage = null)
    {
        _storage = storage ?? new InMemoryStorage();
        _manager = new RecurringJobManager(_storage);
    }

    public async Task Start(string applicationName, Action<IServiceCollection> registerJobs, bool removeStaleJobs = false)
    {
        var services = new ServiceCollection().AddLogging();
        services.Configure<RecurringJobsOptions>(o => o.RemoveStaleRecurringJobs = removeStaleJobs);
        services.AddSingleton<JobStorage>(_storage);
        services.AddSingleton<IRecurringJobManager>(_manager);
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ApplicationName.Returns(applicationName);
        services.AddSingleton(hostEnvironment);
        registerJobs(services);
        await using var provider = services.BuildServiceProvider();
        await provider.GetServices<IHostedService>().OfType<RecurringJobRegistrar>().Single().StartAsync(CancellationToken.None);
    }

    public void SeedLegacyEntry<TJob>(string legacyId) where TJob : class, IJob
    {
        var runner = typeof(RecurringJobRunner<TJob>);
        _manager.AddOrUpdate(legacyId, new Job(runner, runner.GetMethod("RunAsync")!, CancellationToken.None), Cron, new RecurringJobOptions());
    }

    public List<RecurringJobDto> StoredJobs()
    {
        using var connection = _storage.GetConnection();
        return connection.GetRecurringJobs();
    }

    public IEnumerable<string> StoredIds() => StoredJobs().Select(job => job.Id);

    public void Dispose() => (_storage as IDisposable)?.Dispose();
}
