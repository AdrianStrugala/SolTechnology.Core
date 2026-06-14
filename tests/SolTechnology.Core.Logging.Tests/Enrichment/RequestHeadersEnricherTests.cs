using FluentAssertions;
using SolTechnology.Core.Logging.Enrichment;
using NUnit.Framework;

namespace SolTechnology.Core.Logging.Tests.Enrichment;

public class RequestHeadersEnricherTests
{
    private static ILogScopeEnricher BuildEnricher(Action<LoggingOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddCoreLogging(o =>
        {
            // Header logging is off by default; tests opt in unless overridden.
            o.LogRequestHeaders = true;
            configure?.Invoke(o);
        });

        var sp = services.BuildServiceProvider();
        // No LogDetail() registered — only the built-in headers enricher is present.
        return sp.GetServices<ILogScopeEnricher>().Single();
    }

    private static HttpContext WithHeaders(params (string Name, string Value)[] headers)
    {
        var ctx = new DefaultHttpContext();
        foreach (var (name, value) in headers)
        {
            ctx.Request.Headers[name] = value;
        }
        return ctx;
    }

    [Test]
    public void Disabled_by_default_writes_nothing_to_scope()
    {
        var enricher = BuildEnricher(o => o.LogRequestHeaders = false);
        var ctx = WithHeaders(("X-Tenant", "acme"));
        var scope = new Dictionary<string, object?>();

        enricher.Enrich(ctx, scope);

        scope.Should().BeEmpty();
    }

    [Test]
    public void Enabled_projects_all_headers_under_RequestHeaders_key()
    {
        var enricher = BuildEnricher();
        var ctx = WithHeaders(("X-Tenant", "acme"), ("User-Agent", "tests/1.0"));
        var scope = new Dictionary<string, object?>();

        enricher.Enrich(ctx, scope);

        scope.Should().ContainKey("RequestHeaders");
        var projected = (IDictionary<string, string>)scope["RequestHeaders"]!;
        projected["X-Tenant"].Should().Be("acme");
        projected["User-Agent"].Should().Be("tests/1.0");
    }

    [Test]
    public void Masks_default_sensitive_headers_case_insensitively()
    {
        var enricher = BuildEnricher();
        var ctx = WithHeaders(
            ("authorization", "Bearer abc.def.ghi"),
            ("Cookie", "session=xyz"),
            ("X-Tenant", "acme"));
        var scope = new Dictionary<string, object?>();

        enricher.Enrich(ctx, scope);

        var projected = (IDictionary<string, string>)scope["RequestHeaders"]!;
        projected["authorization"].Should().Be(LoggingDefaults.MaskedValue);
        projected["Cookie"].Should().Be(LoggingDefaults.MaskedValue);
        projected["X-Tenant"].Should().Be("acme");
    }

    [Test]
    public void Masks_Bearer_values_in_unexpected_headers()
    {
        var enricher = BuildEnricher();
        var ctx = WithHeaders(("X-Forwarded-Authorization", "Bearer leaked"));
        var scope = new Dictionary<string, object?>();

        enricher.Enrich(ctx, scope);

        var projected = (IDictionary<string, string>)scope["RequestHeaders"]!;
        projected["X-Forwarded-Authorization"].Should().Be(LoggingDefaults.MaskedValue);
    }

    [Test]
    public void Custom_MaskedHeaders_replace_defaults()
    {
        var enricher = BuildEnricher(o => o.MaskedHeaders = new List<string> { "X-Internal-Token" });

        var ctx = WithHeaders(
            ("Authorization", "Basic stillVisible"),
            ("X-Internal-Token", "secret"));
        var scope = new Dictionary<string, object?>();

        enricher.Enrich(ctx, scope);

        var projected = (IDictionary<string, string>)scope["RequestHeaders"]!;
        projected["Authorization"].Should().Be("Basic stillVisible");
        projected["X-Internal-Token"].Should().Be(LoggingDefaults.MaskedValue);
    }
}



