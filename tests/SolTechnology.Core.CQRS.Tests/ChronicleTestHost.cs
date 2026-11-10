using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.CQRS.Tests;

/// <summary>A real Chronicle from <c>AddSolChronicle</c> whose reports land in a <see cref="FakeLogCollector"/>.</summary>
public sealed class ChronicleTestHost : IDisposable
{
    private readonly ServiceProvider _provider;

    public ChronicleTestHost(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(new FakeLoggerProvider(Logs)));
        services.AddSolChronicle();
        configure?.Invoke(services);
        _provider = services.BuildServiceProvider();
    }

    public FakeLogCollector Logs { get; } = new();

    public IServiceProvider Services => _provider;

    public IChronicle Chronicle => _provider.GetRequiredService<IChronicle>();

    /// <summary>The Chronicle reports (EventId 2154) written so far.</summary>
    public IReadOnlyList<FakeLogRecord> Reports => [.. Logs.GetSnapshot().Where(r => r.Id.Id == 2154)];

    public ILogger<T> Logger<T>() => _provider.GetRequiredService<ILogger<T>>();

    public void Dispose() => _provider.Dispose();
}
