using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SolTechnology.Core.Logging.Correlations;
using SolTechnology.Core.Logging.Enrichment;
using NUnit.Framework;

namespace SolTechnology.Core.Logging.Tests;

public class ModuleInstallerTests
{
    private static readonly TestCaseData[] InvalidOptions =
    [
        new TestCaseData((Action<LoggingOptions>)(o => o.MaxLoggedJsonBodyBytes = -1)).SetArgDisplayNames("NegativeMaxBodyBytes"),
        new TestCaseData((Action<LoggingOptions>)(o => o.SkipPaths = ["/health", "  "])).SetArgDisplayNames("WhitespaceSkipPath")
    ];

    private sealed class DummyEnricher : ILogScopeEnricher
    {
        public void Enrich(HttpContext context, IDictionary<string, object?> scope) { }
    }

    [Test]
    public void AddSolLogging_ResolvedTwice_ReturnsSameCorrelationService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSolLogging();
        var sp = services.BuildServiceProvider();

        // Act
        var first = sp.GetRequiredService<ICorrelationIdService>();
        var second = sp.GetRequiredService<ICorrelationIdService>();

        // Assert
        first.Should().BeSameAs(second);
    }

    [Test]
    public void AddSolLogging_CalledTwice_RegistersCorrelationServiceOnce()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSolLogging();
        services.AddSolLogging();

        // Assert
        services.Count(d => d.ServiceType == typeof(ICorrelationIdService)).Should().Be(1);
    }

    [TestCaseSource(nameof(InvalidOptions))]
    public void AddSolLogging_InvalidOptions_ThrowsOnOptionsResolution(Action<LoggingOptions> configure)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSolLogging(configure);
        var sp = services.BuildServiceProvider();

        // Act
        var act = () => sp.GetRequiredService<IOptions<LoggingOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }

    [Test]
    public void LogDetail_CalledTwice_RegistersOneAggregatingEnricher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSolLogging();

        // Act
        services.LogDetail("X-Tenant-Id", asName: "TenantId", source: LogDetailSource.Header);
        services.LogDetail("name", source: LogDetailSource.Body);

        // Assert
        var enrichers = services.BuildServiceProvider().GetServices<ILogScopeEnricher>().ToArray();
        enrichers.Should().HaveCount(2, "one LogDetailEnricher for both calls plus the built-in RequestHeadersEnricher");
    }

    [Test]
    public void AddSolLogScopeEnricher_CustomEnricher_IsResolvedAlongsideBuiltIns()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSolLogging();
        services.LogDetail("X-Tenant-Id", source: LogDetailSource.Header);

        // Act
        services.AddSolLogScopeEnricher<DummyEnricher>();

        // Assert
        var enrichers = services.BuildServiceProvider().GetServices<ILogScopeEnricher>().ToArray();
        enrichers.Should().HaveCount(3, "RequestHeadersEnricher and LogDetailEnricher are built in");
        enrichers.Should().ContainSingle(e => e is DummyEnricher);
    }
}
