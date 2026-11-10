using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NUnit.Framework;
using Polly.Timeout;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.HTTP.Tests;

/// <summary>An <c>AddSolHTTPClient</c> client over a scripted primary handler, with Chronicle reports captured.</summary>
public abstract class ChronicleHTTPTestBase
{
    private ServiceProvider? _provider;

    protected FakeLogCollector Logs { get; private set; } = null!;
    protected ScriptedHandler Upstream { get; private set; } = null!;

    protected IReadOnlyList<FakeLogRecord> Reports => [.. Logs.GetSnapshot().Where(r => r.Id.Id == 2154)];

    protected IChronicle Chronicle => _provider!.GetRequiredService<IChronicle>();

    protected HttpClient Client => _provider!.GetRequiredService<ISampleHTTPClient>().HttpClient;

    protected IServiceProvider Services => _provider!;

    [SetUp]
    public void SetUpClient()
    {
        Logs = new FakeLogCollector();
        Upstream = new ScriptedHandler();
        Build();
    }

    [TearDown]
    public void TearDownClient()
    {
        _provider?.Dispose();
        Upstream.Dispose();
    }

    protected void Build(HttpPolicyConfiguration? policy = null, bool propagateCorrelation = true)
    {
        _provider?.Dispose();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(new FakeLoggerProvider(Logs)));
        services.AddSolHTTPClient<ISampleHTTPClient, SampleHTTPClient>(
            "Sample",
            new HTTPClientConfiguration { BaseAddress = "http://api.test/" },
            policy ?? new HttpPolicyConfiguration { RetryInitialDelay = 0, RequestTimeout = 50 },
            propagateCorrelation);
        services.AddHttpClient("Sample").ConfigurePrimaryHttpMessageHandler(() => Upstream);
        _provider = services.BuildServiceProvider();
    }

    /// <summary>Sends inside a Chronicle root and returns the root's rendered report.</summary>
    protected async Task<string> SendInRoot(HttpMethod method, string path = "api/cities", CancellationToken cancellationToken = default)
    {
        using (Chronicle.Begin("Import"))
        {
            try
            {
                using var response = await Client.SendAsync(new HttpRequestMessage(method, path), cancellationToken);
            }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutRejectedException)
            {
            }
        }

        return Reports.Single().Message;
    }
}

/// <summary>Primary handler that answers attempt <c>n</c> with the <c>n</c>-th script entry (the last one repeats).</summary>
public sealed class ScriptedHandler : HttpMessageHandler
{
    private readonly List<Func<CancellationToken, Task<HttpResponseMessage>>> _script = [];

    public ConcurrentQueue<HttpRequestMessage> Requests { get; } = new();

    public ScriptedHandler Then(HttpStatusCode statusCode)
        => Then(_ => Task.FromResult(new HttpResponseMessage(statusCode)));

    public ScriptedHandler Then(Func<CancellationToken, Task<HttpResponseMessage>> respond)
    {
        _script.Add(respond);
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);
        var index = Math.Min(Requests.Count, Math.Max(_script.Count, 1)) - 1;
        return _script.Count == 0 ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)) : _script[index](cancellationToken);
    }
}
