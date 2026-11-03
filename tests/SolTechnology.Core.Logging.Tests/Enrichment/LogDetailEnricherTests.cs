using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using SolTechnology.Core.Logging.Enrichment;

namespace SolTechnology.Core.Logging.Tests.Enrichment;

public class LogDetailEnricherTests
{
    private readonly LogDetailEnricher _sut = new(
    [
        new LogDetailDescriptor("X-Tenant-Id", "TenantId", LogDetailSource.Header, ["/api/v1/Trips"])
    ]);

    [TestCase("/api/v1/Trips", true)]
    [TestCase("/api/v1/trips/42", true)]
    [TestCase("/api/v1/TripsArchive", false)]
    public void Enrich_EndpointPrefix_MatchesWholeSegmentsOnly(string path, bool expectedEnriched)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Headers["X-Tenant-Id"] = "acme";
        var scope = new Dictionary<string, object?>();

        // Act
        _sut.Enrich(context, scope);

        // Assert
        scope.ContainsKey("TenantId").Should().Be(expectedEnriched);
    }
}
