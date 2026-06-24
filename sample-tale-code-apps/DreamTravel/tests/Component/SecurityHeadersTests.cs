using FluentAssertions;

namespace DreamTravel.FunctionalTests;

/// <summary>
/// Verifies the <c>UseSecurityHeaders()</c> middleware stamps baseline security headers on every
/// response from the DreamTravel API — including relaxed CSP on Swagger paths.
/// </summary>
public sealed class SecurityHeadersTests
{
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = ComponentTestsFixture.ApiFixture.ServerClient;
    }

    [Test]
    public async Task Api_Response_Contains_Strict_Security_Headers()
    {
        var response = await _client.GetAsync("/api/FindCityByName?cityName=test");

        response.Headers.GetValues("Content-Security-Policy").Should()
            .ContainSingle("default-src 'none'; frame-ancestors 'none'");
        response.Headers.GetValues("X-Content-Type-Options").Should()
            .ContainSingle("nosniff");
        response.Headers.GetValues("Referrer-Policy").Should()
            .ContainSingle("no-referrer");
    }

    [Test]
    public async Task Swagger_Path_Gets_Relaxed_CSP()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        csp.Should().Contain("'self'");
        csp.Should().Contain("'unsafe-inline'");
        csp.Should().NotBe("default-src 'none'; frame-ancestors 'none'");

        // Other headers still strict
        response.Headers.GetValues("X-Content-Type-Options").Should()
            .ContainSingle("nosniff");
    }
}

