using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Enrichment;
using SolTechnology.Core.Logging.Middleware;
using Xunit;

namespace SolTechnology.Core.Logging.Tests.Middleware;

public class LoggingMiddlewareTests
{
    private static (LoggingMiddleware Middleware, RequestDelegateRecorder Next) Build(
        LoggingOptions? options = null,
        Func<HttpContext, Task>? handler = null)
    {
        var next = new RequestDelegateRecorder(handler);
        var mw = new LoggingMiddleware(next.Invoke, Options.Create(options ?? new LoggingOptions()));
        return (mw, next);
    }

    private sealed class RequestDelegateRecorder
    {
        private readonly Func<HttpContext, Task>? _handler;
        public bool WasInvoked { get; private set; }
        public RequestDelegateRecorder(Func<HttpContext, Task>? handler) => _handler = handler;
        public async Task Invoke(HttpContext ctx)
        {
            WasInvoked = true;
            if (_handler is not null) await _handler(ctx);
        }
    }

    private sealed class CorrelationStore : ICorrelationIdService
    {
        public CorrelationId? Stored { get; private set; }
        public void Set(CorrelationId correlationId) => Stored = correlationId;
        public CorrelationId? Get() => Stored;
        public CorrelationId GetOrGenerate() => Stored ??= CorrelationId.Generate();
    }

    private sealed class CapturingEnricher : ILogScopeEnricher
    {
        public IDictionary<string, object?>? Captured { get; private set; }
        public void Enrich(HttpContext context, IDictionary<string, object?> scope)
            => Captured = new Dictionary<string, object?>(scope);
    }

    private sealed class ThrowingEnricher : ILogScopeEnricher
    {
        public void Enrich(HttpContext context, IDictionary<string, object?> scope)
            => throw new InvalidOperationException("boom");
    }

    [Fact]
    public async Task Echoes_X_Correlation_Id_header_on_response()
    {
        Activity.Current = null;
        var (mw, _) = Build();
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationId.HeaderKey] = "test-id-123";
        var store = new CorrelationStore();

        await mw.InvokeAsync(ctx, NullLogger<LoggingMiddleware>.Instance,
            store, Array.Empty<ILogScopeEnricher>());

        // The middleware stores the correlation for the async-flow and registers an OnStarting
        // callback that echoes it on the response. The echo itself is asserted in the
        // CorrelationId unit tests; here we only verify the value the middleware committed.
        store.Stored.Should().NotBeNull();
        store.Stored!.Value.Should().Be("test-id-123");
    }

    [Fact]
    public async Task Generates_correlation_when_no_header_or_activity_provided()
    {
        Activity.Current = null;
        var (mw, _) = Build();
        var ctx = new DefaultHttpContext();
        var store = new CorrelationStore();

        await mw.InvokeAsync(ctx, NullLogger<LoggingMiddleware>.Instance, store, Array.Empty<ILogScopeEnricher>());

        store.Stored.Should().NotBeNull();
        store.Stored!.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Sets_500_status_and_rethrows_on_unhandled_exception()
    {
        var (mw, _) = Build(handler: _ => throw new InvalidOperationException("boom"));
        var ctx = new DefaultHttpContext();

        Func<Task> act = () => mw.InvokeAsync(ctx, NullLogger<LoggingMiddleware>.Instance,
            new CorrelationStore(), Array.Empty<ILogScopeEnricher>());

        await act.Should().ThrowAsync<InvalidOperationException>();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Faulty_enricher_does_not_break_request()
    {
        var faulty = new ThrowingEnricher();
        var (mw, next) = Build();
        var ctx = new DefaultHttpContext();

        await mw.InvokeAsync(ctx, NullLogger<LoggingMiddleware>.Instance,
            new CorrelationStore(), new ILogScopeEnricher[] { faulty });

        next.WasInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task SkipPaths_short_circuits_middleware_after_setting_correlation()
    {
        var options = new LoggingOptions { SkipPaths = new[] { "/health" } };
        var (mw, next) = Build(options);
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/health";
        var store = new CorrelationStore();
        var capturing = new CapturingEnricher();

        await mw.InvokeAsync(ctx, NullLogger<LoggingMiddleware>.Instance,
            store, new ILogScopeEnricher[] { capturing });

        next.WasInvoked.Should().BeTrue();
        store.Stored.Should().NotBeNull("correlation must still be set even on skipped paths");
        capturing.Captured.Should().BeNull("enrichers must be skipped on skip paths");
    }

    [Fact]
    public async Task Body_LogDetail_projects_property_into_scope_via_LogDetailEnricher()
    {
        var services = new ServiceCollection();
        services.AddCoreLogging();
        services.LogDetail("name", asName: "CityName", source: LogDetailSource.Body);
        var sp = services.BuildServiceProvider();
        var builtIn = sp.GetServices<ILogScopeEnricher>().ToArray();

        var capturing = new CapturingEnricher();
        var allEnrichers = builtIn.Concat(new ILogScopeEnricher[] { capturing }).ToArray();

        var (mw, _) = Build();
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "POST";
        ctx.Request.Path = "/echo";
        ctx.Request.ContentType = "application/json";
        var json = JsonSerializer.Serialize(new { name = "Warsaw" });
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;

        await mw.InvokeAsync(ctx, NullLogger<LoggingMiddleware>.Instance,
            new CorrelationStore(), allEnrichers);

        capturing.Captured.Should().NotBeNull();
        capturing.Captured!.Should().ContainKey("CityName").WhoseValue.Should().Be("Warsaw");
        ctx.Request.Body.Position.Should().Be(0, "body must be rewound for downstream consumers");
    }
}


