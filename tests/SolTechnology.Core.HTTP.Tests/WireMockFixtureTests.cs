using System.Net;
using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.HTTP.Testing;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Settings;

namespace SolTechnology.Core.HTTP.Tests;

[TestFixture]
public sealed class WireMockFixtureTests
{
    [TestCase(true, HttpStatusCode.OK)]
    [TestCase(false, HttpStatusCode.NotFound)]
    public async Task Initialize_WithSettings_ServesPartialMatchOnlyWhenAllowed(
        bool allowPartialMapping, HttpStatusCode expected)
    {
        // Arrange
        await using var fixture = new WireMockFixture();
        var settings = WireMockStartup.DefaultSettings();
        settings.AllowPartialMapping = allowPartialMapping;
        var startup = fixture.Initialize(settings);
        startup.WireMockServer
            .Given(Request.Create().UsingGet().WithPath("/cities").WithParam("name", "Wroclaw"))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));
        using var client = new HttpClient { BaseAddress = new Uri(fixture.Url) };

        // Act
        using var response = await client.GetAsync("/cities?name=Warsaw");

        // Assert
        response.StatusCode.Should().Be(expected);
    }

    [Test]
    public async Task Initialize_ThenAllowPartialMapping_ServesPartialMatch()
    {
        // Arrange
        await using var fixture = new WireMockFixture();
        var server = fixture.Initialize().WireMockServer;
        server.AllowPartialMapping();
        server
            .Given(Request.Create().UsingGet().WithPath("/cities").WithParam("name", "Wroclaw"))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));
        using var client = new HttpClient { BaseAddress = new Uri(fixture.Url) };

        // Act
        using var response = await client.GetAsync("/cities?name=Warsaw");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public void Initialize_NullSettings_Throws()
    {
        // Arrange
        using var fixture = new WireMockFixture();

        // Act
        var act = () => fixture.Initialize((WireMockServerSettings)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
