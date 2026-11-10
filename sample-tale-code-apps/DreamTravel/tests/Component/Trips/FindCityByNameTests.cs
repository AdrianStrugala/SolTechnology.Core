using System.Net;
using System.Text.Json;
using DreamTravel.FunctionalTests.FakeApis;
using DreamTravel.GeolocationDataClients.GoogleApi;
using FluentAssertions;
using SolTechnology.Core.HTTP;
using SolTechnology.Core.HTTP.Testing;

namespace DreamTravel.FunctionalTests.Trips;

public class FindCityByNameTests
{
    private HttpClient _apiClient = null!;
    private WireMockFixture _wireMockFixture = null!;

    [SetUp]
    public void Setup()
    {
        _apiClient = ComponentTestsFixture.ApiFixture.ServerClient;
        _wireMockFixture = ComponentTestsFixture.WireMockFixture;
    }

    [Test]
    public async Task FindCityByName_UnknownCity_ReturnsNotFoundProblemDetails()
    {
        // Arrange
        var name = "Unknown" + Guid.NewGuid().ToString("N")[..8];
        _wireMockFixture.Fake<IGoogleHTTPClient>()
            .WithRequest(x => x.GetLocationOfCity(name))
            .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.ZeroResultsResponse));

        // Act
        using var response = await _apiClient
            .CreateRequest("/api/FindCityByName")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithHeader("X-API-VERSION", "2.0")
            .WithBody(new { Name = name })
            .Post();
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = JsonDocument.Parse(body).RootElement;
        problem.GetProperty("status").GetInt32().Should().Be(404);
        problem.GetProperty("title").GetString().Should().Be($"City [{name}] was not found");
    }
}
