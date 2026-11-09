using System.Net;
using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.HTTP.Testing;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace SolTechnology.Core.HTTP.Tests;

[TestFixture]
public sealed class WireMockStartupTests
{
    [TestCase("/cities?name=Wroclaw", HttpStatusCode.OK)]
    [TestCase("/cities?name=Warsaw", HttpStatusCode.NotFound)]
    [TestCase("/cities", HttpStatusCode.NotFound)]
    public async Task Run_WithDefaultSettings_MatchesOnlyRequestsThatMeetEveryMatcher(
        string pathAndQuery, HttpStatusCode expected)
    {
        // Arrange
        var startup = new WireMockStartup();
        startup.Run();
        using var server = startup.WireMockServer;
        server
            .Given(Request.Create().UsingGet().WithPath("/cities").WithParam("name", "Wroclaw"))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));
        using var client = new HttpClient { BaseAddress = new Uri(startup.Url) };

        // Act
        using var response = await client.GetAsync(pathAndQuery);

        // Assert
        WireMockStartup.DefaultSettings().AllowPartialMapping.Should().BeFalse();
        response.StatusCode.Should().Be(expected);
    }
}
