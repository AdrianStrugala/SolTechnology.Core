using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.HTTP.Handlers;
using SolTechnology.Core.Logging;
using SolTechnology.Core.Logging.Correlations;
using NUnit.Framework;

namespace SolTechnology.Core.HTTP.Tests;

/// <summary>
/// Outbound-correlation behaviour of <see cref="CorrelationPropagatingHandler"/>.
/// Constructs the handler directly with a mocked inner handler so the tests are
/// independent of the full DI pipeline.
/// </summary>
public sealed class CorrelationPropagationTests
{
    private sealed class CapturingInnerHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Captured { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Captured = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static (HttpClient Client, CapturingInnerHandler Capture, ICorrelationIdService Service) NewClient()
    {
        var capture = new CapturingInnerHandler();
        var service = new CorrelationIdService();

        var handler = new CorrelationPropagatingHandler(service) { InnerHandler = capture };
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") };
        return (client, capture, service);
    }

    [Test]
    public async Task OutboundRequest_NoActivity_CarriesXCorrelationIdOnly()
    {
        // Run in a fresh ExecutionContext so any ambient Activity from the test
        // host bleeds away. Activity.Current is set to null explicitly to make
        // the intent obvious to readers.
        Activity.Current = null;

        var (client, capture, _) = NewClient();

        await client.GetAsync("x");

        capture.Captured.Should().NotBeNull();
        capture.Captured!.Headers.Should().Contain(h => h.Key == CorrelationId.HeaderKey);
        capture.Captured.Headers.Should().NotContain(h => h.Key == CorrelationId.TraceParentHeaderKey,
            "stand-alone correlation must not fabricate a traceparent span id");
    }

    [Test]
    public async Task OutboundRequest_ActivityInScope_CarriesTraceparentAndMatchingCorrelationId()
    {
        using var activity = new Activity("test").Start();

        var (client, capture, _) = NewClient();
        await client.GetAsync("x");

        capture.Captured.Should().NotBeNull();

        capture.Captured!.Headers.TryGetValues(CorrelationId.HeaderKey, out var correlationValues).Should().BeTrue();
        var correlation = string.Join(",", correlationValues!);
        correlation.Should().Be(activity.TraceId.ToHexString());

        capture.Captured.Headers.TryGetValues(CorrelationId.TraceParentHeaderKey, out var tpValues).Should().BeTrue();
        var traceparent = string.Join(",", tpValues!);
        traceparent.Should().StartWith("00-").And.Contain(activity.TraceId.ToHexString());
    }

    [Test]
    public async Task OutboundRequest_AmbientCorrelationIdMatchesService()
    {
        // The handler must source its id from the same ICorrelationIdService
        // instance the rest of the app uses — that's the whole point of the
        // singleton registration in ModuleInstaller. We seed the correlation
        // in the test's ExecutionContext (mirroring what
        // Core.Logging.LoggingMiddleware does on inbound) and then verify the
        // outbound handler observes the SAME value.
        //
        // Note: AsyncLocal writes propagate downward (caller→callee), not
        // upward — so we have to set the value before the SendAsync call
        // rather than read it after.
        Activity.Current = null;

        var (client, capture, service) = NewClient();

        var expected = service.GetOrGenerate();

        await client.GetAsync("x");

        var fromHeader = string.Join(",", capture.Captured!.Headers.GetValues(CorrelationId.HeaderKey));
        fromHeader.Should().Be(expected.Value);
    }

    [Test]
    public async Task AddCorrelationIdService_RegistrationIsIdempotent()
    {
        // ModuleInstaller calls AddCorrelationIdService for every typed client;
        // calling AddCoreLogging in the same host adds it again. Both paths
        // must resolve to the SAME singleton instance — otherwise inbound
        // middleware and outbound handler see different AsyncLocal stores.
        var services = new ServiceCollection();
        services.AddCorrelationIdService();
        services.AddCorrelationIdService();

        var sp = services.BuildServiceProvider();

        var a = sp.GetRequiredService<ICorrelationIdService>();
        var b = sp.GetRequiredService<ICorrelationIdService>();
        a.Should().BeSameAs(b);
    }

    [Test]
    public async Task Handler_PreExistingCorrelationHeader_PreservesCallerValueAndSkipsGenerate()
    {
        // Hosts that own correlation (OpenTelemetry / firm middleware) attach
        // their own X-Correlation-Id before our handler runs. The handler must
        // be additive-only — keep the caller's value AND avoid seeding our
        // AsyncLocal store, which would otherwise mint a second id that
        // Core.Logging would observe and log a value different from the one
        // the partner sees on the wire.
        Activity.Current = null;
        const string callerSupplied = "caller-owned-correlation-id";

        var (client, capture, service) = NewClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "x");
        request.Headers.TryAddWithoutValidation(CorrelationId.HeaderKey, callerSupplied);

        await client.SendAsync(request);

        capture.Captured!.Headers.GetValues(CorrelationId.HeaderKey)
            .Should().ContainSingle(v => v == callerSupplied);

        // No call to service.GetOrGenerate() inside the handler → ambient store
        // is still empty. Get() returns null when no correlation has been seeded.
        service.Get().Should().BeNull(
            "additive-only handler must not seed AsyncLocal when caller owns correlation");
    }
}


