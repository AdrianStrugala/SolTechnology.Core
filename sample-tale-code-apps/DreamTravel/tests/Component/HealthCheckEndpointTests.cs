using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace DreamTravel.FunctionalTests;

/// <summary>
/// Verifies the <c>MapCoreHealthChecks</c> endpoint (Core.Api) renders the registered health checks
/// as JSON via <c>HealthReportJsonFormatter</c>, against the real DreamTravel API host.
/// </summary>
public sealed class HealthCheckEndpointTests
{
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = ComponentTestsFixture.ApiFixture.ServerClient;
    }

    [Test]
    public async Task Healthz_Returns_200_With_Json_Body()
    {
        var response = await _client.GetAsync("/healthz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // Overall status is present
        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");

        // The Aspire "self" liveness check is rendered
        doc.RootElement.GetProperty("entries").GetProperty("self")
            .GetProperty("status").GetString().Should().Be("Healthy");

        // The Core.SQL health check is rendered (connectivity to the Docker SQL container)
        doc.RootElement.GetProperty("entries").GetProperty("sql")
            .GetProperty("status").GetString().Should().Be("Healthy");
    }
}


