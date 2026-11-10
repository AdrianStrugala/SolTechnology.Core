using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Tale.Tests;

/// <summary>A Tale host whose Chronicle reports (EventId 2154) land in a <see cref="FakeLogCollector"/>.</summary>
public abstract class TaleChronicleTestBase
{
    protected FakeLogCollector Logs { get; private set; } = null!;
    protected ServiceProvider Services { get; set; } = null!;
    protected CallerCancellation Caller { get; private set; } = null!;

    protected IReadOnlyList<FakeLogRecord> Reports => [.. Logs.GetSnapshot().Where(r => r.Id.Id == 2154)];

    protected IChronicle Chronicle => Services.GetRequiredService<IChronicle>();

    [SetUp]
    public void SetUpHost()
    {
        Logs = new FakeLogCollector();
        Caller = new CallerCancellation();
        Services = Build();
    }

    [TearDown]
    public void TearDownHost()
    {
        Services.Dispose();
        Caller.Dispose();
    }

    protected ServiceProvider Build(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(new FakeLoggerProvider(Logs)));
        services.AddSingleton(Caller);
        services.AddSolTale(assemblies: typeof(TaleChronicleTestBase).Assembly);
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    protected Task<Result<LifecycleOutput>> Handle<THandler>(int value = 1)
        where THandler : TaleHandler<LifecycleInput, LifecycleContext, LifecycleOutput>
        => Services.GetRequiredService<THandler>().Handle(new LifecycleInput { Value = value }, Caller.Source.Token);

    /// <summary>The rendered tree lines of the only report, without the message prefix.</summary>
    protected string[] TreeLines() => Reports.Single().Message[..^1].Split('\n')[1..];
}
