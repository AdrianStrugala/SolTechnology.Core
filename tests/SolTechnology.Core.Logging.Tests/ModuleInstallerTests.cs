using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Enrichment;
using Xunit;

namespace SolTechnology.Core.Logging.Tests;

public class ModuleInstallerTests
{
    [Fact]
    public void AddCoreLogging_registers_correlation_service_as_singleton()
    {
        var services = new ServiceCollection();
        services.AddCoreLogging();
        var sp = services.BuildServiceProvider();

        var first = sp.GetRequiredService<ICorrelationIdService>();
        var second = sp.GetRequiredService<ICorrelationIdService>();

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void AddCoreLogging_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddCoreLogging();
        services.AddCoreLogging();

        services.Count(d => d.ServiceType == typeof(ICorrelationIdService)).Should().Be(1);
    }

    [Fact]
    public void AddCoreLogging_validates_options_on_resolution()
    {
        var services = new ServiceCollection();
        services.AddCoreLogging(o => o.MaxLoggedJsonBodyBytes = -1);
        var sp = services.BuildServiceProvider();

        var act = () => sp.GetRequiredService<IOptions<LoggingOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void AddCoreLogging_rejects_whitespace_skip_paths()
    {
        var services = new ServiceCollection();
        services.AddCoreLogging(o => o.SkipPaths = new[] { "/health", "  " });
        var sp = services.BuildServiceProvider();

        var act = () => sp.GetRequiredService<IOptions<LoggingOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void LogDetail_registers_descriptor_and_aggregating_enricher_once()
    {
        var services = new ServiceCollection();
        services.AddCoreLogging();
        services.LogDetail("X-Tenant-Id", asName: "TenantId", source: LogDetailSource.Header);
        services.LogDetail("name", source: LogDetailSource.Body);

        var sp = services.BuildServiceProvider();
        var enrichers = sp.GetServices<ILogScopeEnricher>().ToArray();

        // Two LogDetail calls but a single aggregating enricher.
        enrichers.Should().HaveCount(1);
    }

    private sealed class DummyEnricher : ILogScopeEnricher
    {
        public void Enrich(Microsoft.AspNetCore.Http.HttpContext context, IDictionary<string, object?> scope) { }
    }

    [Fact]
    public void AddLogScopeEnricher_appends_user_enricher_alongside_built_in()
    {
        var services = new ServiceCollection();
        services.AddCoreLogging();
        services.LogDetail("X-Tenant-Id", source: LogDetailSource.Header);
        services.AddLogScopeEnricher<DummyEnricher>();

        var sp = services.BuildServiceProvider();
        var enrichers = sp.GetServices<ILogScopeEnricher>().ToArray();

        enrichers.Should().HaveCount(2);
        enrichers.Should().ContainSingle(e => e is DummyEnricher);
    }
}

