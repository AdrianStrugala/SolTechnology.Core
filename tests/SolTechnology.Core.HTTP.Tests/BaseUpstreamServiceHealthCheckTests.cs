using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using SolTechnology.Core.HTTP.HealthChecks;

namespace SolTechnology.Core.HTTP.Tests;

/// <summary>
/// Verifies the exception/status taxonomy and result-caching of
/// <see cref="BaseUpstreamServiceHealthCheck{TReport}"/>.
/// </summary>
public sealed class BaseUpstreamServiceHealthCheckTests
{
    private sealed record UpstreamReport(string Status);

    private sealed class TestUpstreamCheck(
        HttpClient client,
        UpstreamHealthCheckOptions options,
        TimeProvider? time = null)
        : BaseUpstreamServiceHealthCheck<UpstreamReport>(client, "/health", options, NullLogger.Instance, time)
    {
        protected override HealthCheckResult EvaluateReport(UpstreamReport report) =>
            report.Status == "ok"
                ? HealthCheckResult.Healthy("upstream ok")
                : HealthCheckResult.Unhealthy($"upstream status: {report.Status}");
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return await responder(request, cancellationToken);
        }
    }

    private static HttpClient ClientReturning(HttpStatusCode code, string body, StubHandler? capture = null)
    {
        var handler = capture ?? new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(code)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        }));
        return new HttpClient(handler) { BaseAddress = new Uri("http://upstream/") };
    }

    private static HealthCheckContext Context() => new()
    {
        Registration = new HealthCheckRegistration("upstream", _ => null!, HealthStatus.Unhealthy, null)
    };

    [Test]
    public async Task ValidReport_Ok_ReturnsHealthy()
    {
        var client = ClientReturning(HttpStatusCode.OK, """{"status":"ok"}""");
        var sut = new TestUpstreamCheck(client, new UpstreamHealthCheckOptions());

        var result = await sut.CheckHealthAsync(Context());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Test]
    public async Task ValidReport_NotOk_MappedByEvaluateReport()
    {
        var client = ClientReturning(HttpStatusCode.OK, """{"status":"degraded-upstream"}""");
        var sut = new TestUpstreamCheck(client, new UpstreamHealthCheckOptions());

        var result = await sut.CheckHealthAsync(Context());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Test]
    public async Task ConnectionFailure_ReturnsUnhealthy()
    {
        var handler = new StubHandler((_, _) => throw new HttpRequestException("connection refused"));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://upstream/") };
        var sut = new TestUpstreamCheck(client, new UpstreamHealthCheckOptions());

        var result = await sut.CheckHealthAsync(Context());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Test]
    public async Task BadPayload_ReturnsDegraded()
    {
        var client = ClientReturning(HttpStatusCode.OK, "this is not json {{{");
        var sut = new TestUpstreamCheck(client, new UpstreamHealthCheckOptions());

        var result = await sut.CheckHealthAsync(Context());

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Test]
    public async Task NullReport_ReturnsDegraded()
    {
        var client = ClientReturning(HttpStatusCode.OK, "null");
        var sut = new TestUpstreamCheck(client, new UpstreamHealthCheckOptions());

        var result = await sut.CheckHealthAsync(Context());

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Test]
    public async Task PerCallTimeout_ReturnsUnhealthy()
    {
        var handler = new StubHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct); // never completes until the probe timeout fires
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://upstream/") };
        var options = new UpstreamHealthCheckOptions { Timeout = TimeSpan.FromMilliseconds(50) };
        var sut = new TestUpstreamCheck(client, options);

        var result = await sut.CheckHealthAsync(Context());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Test]
    public async Task CallerCancellation_Rethrows()
    {
        var handler = new StubHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://upstream/") };
        var sut = new TestUpstreamCheck(client, new UpstreamHealthCheckOptions());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await sut.CheckHealthAsync(Context(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Result_IsCached_WithinCacheDuration()
    {
        var time = new FakeTimeProvider();
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"status":"ok"}""", Encoding.UTF8, "application/json")
        }));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://upstream/") };
        var options = new UpstreamHealthCheckOptions { CacheDuration = TimeSpan.FromSeconds(30) };
        var sut = new TestUpstreamCheck(client, options, time);

        await sut.CheckHealthAsync(Context());
        await sut.CheckHealthAsync(Context()); // within cache window — no second call

        handler.CallCount.Should().Be(1);
    }

    [Test]
    public async Task Result_Refetched_AfterCacheExpiry()
    {
        var time = new FakeTimeProvider();
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"status":"ok"}""", Encoding.UTF8, "application/json")
        }));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://upstream/") };
        var options = new UpstreamHealthCheckOptions { CacheDuration = TimeSpan.FromSeconds(30) };
        var sut = new TestUpstreamCheck(client, options, time);

        await sut.CheckHealthAsync(Context());
        time.Advance(TimeSpan.FromSeconds(31)); // past cache window
        await sut.CheckHealthAsync(Context());

        handler.CallCount.Should().Be(2);
    }
}

