using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Enrichment;
using NUnit.Framework;

namespace SolTechnology.Core.Logging.Tests;

public class ModuleInstallerTests
{
    [Test]
    public void AddSolLogging_registers_correlation_service_as_singleton()
    {
        var services = new ServiceCollection();
        services.AddSolLogging();
        var sp = services.BuildServiceProvider();

        var first = sp.GetRequiredService<ICorrelationIdService>();
        var second = sp.GetRequiredService<ICorrelationIdService>();

        first.Should().BeSameAs(second);
    }

    [Test]
    public void AddSolLogging_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddSolLogging();
        services.AddSolLogging();

        services.Count(d => d.ServiceType == typeof(ICorrelationIdService)).Should().Be(1);
    }

    [Test]
    public void AddSolLogging_validates_options_on_resolution()
    {
        var services = new ServiceCollection();
        services.AddSolLogging(o => o.MaxLoggedJsonBodyBytes = -1);
        var sp = services.BuildServiceProvider();

        var act = () => sp.GetRequiredService<IOptions<LoggingOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Test]
    public void AddSolLogging_rejects_whitespace_skip_paths()
    {
        var services = new ServiceCollection();
        services.AddSolLogging(o => o.SkipPaths = new[] { "/health", "  " });
        var sp = services.BuildServiceProvider();

        var act = () => sp.GetRequiredService<IOptions<LoggingOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Test]
    public void LogDetail_registers_descriptor_and_aggregating_enricher_once()
    {
        var services = new ServiceCollection();
        services.AddSolLogging();
        services.LogDetail("X-Tenant-Id", asName: "TenantId", source: LogDetailSource.Header);
        services.LogDetail("name", source: LogDetailSource.Body);

        var sp = services.BuildServiceProvider();
        var enrichers = sp.GetServices<ILogScopeEnricher>().ToArray();

        // Two LogDetail calls but a single aggregating LogDetail enricher,
        // plus the always-on RequestHeadersEnricher registered by AddSolLogging.
        enrichers.Should().HaveCount(2);
    }

    private sealed class DummyEnricher : ILogScopeEnricher
    {
        public void Enrich(Microsoft.AspNetCore.Http.HttpContext context, IDictionary<string, object?> scope) { }
    }

    [Test]
    public void AddSolLogScopeEnricher_appends_user_enricher_alongside_built_in()
    {
        var services = new ServiceCollection();
        services.AddSolLogging();
        services.LogDetail("X-Tenant-Id", source: LogDetailSource.Header);
        services.AddSolLogScopeEnricher<DummyEnricher>();

        var sp = services.BuildServiceProvider();
        var enrichers = sp.GetServices<ILogScopeEnricher>().ToArray();

        // RequestHeadersEnricher (built-in) + LogDetailEnricher + DummyEnricher (user).
        enrichers.Should().HaveCount(3);
        enrichers.Should().ContainSingle(e => e is DummyEnricher);
    }
}

