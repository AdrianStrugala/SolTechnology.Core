using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Middleware;

namespace SolTechnology.Core.Logging.Tests.Middleware;

/// <summary>Runs <see cref="LoggingMiddleware"/> with the real recorder from <c>AddSolChronicle</c> and captures every entry.</summary>
public abstract class LoggingMiddlewareChronicleTestBase
{
    private ServiceProvider? _provider;

    protected FakeTimeProvider Time { get; private set; } = null!;
    protected FakeLogCollector Logs { get; private set; } = null!;
    protected IChronicle Chronicle { get; private set; } = null!;

    protected IReadOnlyList<FakeLogRecord> FinishEntries
        => [.. Logs.GetSnapshot().Where(r => r.Id.Id is 2144 or 2145 or 2146 or 2147 or 2154 or 2155)];

    [SetUp]
    public void SetUpChronicle()
    {
        AmbientChronicle.Current = null;
        Time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));
        Logs = new FakeLogCollector();
        BuildChronicle();
    }

    [TearDown]
    public void TearDownChronicle()
    {
        _provider?.Dispose();
        _provider = null;
        AmbientChronicle.Current = null;
    }

    protected void BuildChronicle(Action<ChronicleOptions>? configure = null)
    {
        _provider?.Dispose();
        var services = new ServiceCollection()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(new FakeLoggerProvider(Logs)))
            .AddSingleton<TimeProvider>(Time);
        services.AddSolChronicle(configure);
        _provider = services.BuildServiceProvider();
        Chronicle = _provider.GetRequiredService<IChronicle>();
    }

    protected async Task<HttpContext> Invoke(
        RequestDelegate next,
        Action<HttpContext>? arrange = null,
        IChronicle? chronicle = null,
        LoggingOptions? options = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/trips";
        arrange?.Invoke(context);
        var sut = new LoggingMiddleware(next, Options.Create(options ?? new LoggingOptions()));
        var logger = _provider!.GetRequiredService<ILogger<LoggingMiddleware>>();
        try
        {
            await sut.InvokeAsync(context, logger, new NoOpCorrelationStore(), [], chronicle ?? Chronicle);
        }
        catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException)
        {
        }

        return context;
    }

    private sealed class NoOpCorrelationStore : ICorrelationIdService
    {
        public void Set(CorrelationId correlationId) { }
        public CorrelationId? Get() => null;
        public void Clear() { }
        public CorrelationId GetOrGenerate() => CorrelationId.Generate();
    }
}
