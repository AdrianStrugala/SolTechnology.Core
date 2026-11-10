using System.Net;
using System.Text.Json;
using DreamTravel.Domain.Cities;
using DreamTravel.FunctionalTests.FakeApis;
using DreamTravel.GeolocationDataClients.GoogleApi;
using FluentAssertions;
using SolTechnology.Core.HTTP;
using SolTechnology.Core.HTTP.Testing;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.FunctionalTests.Trips;

public class CalculateBestPathV1Tests
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
    public async Task CalculateBestPathV1_ValidCities_ReturnsJsonArrayOfPaths()
    {
        // Arrange
        var cities = new List<City>
        {
            new() { Name = "Wroclaw", Latitude = 51.107883, Longitude = 17.038538, Country = "Poland" },
            new() { Name = "Firenze", Latitude = 43.769562, Longitude = 11.255814, Country = "Italy" },
            new() { Name = "Vienna", Latitude = 48.210033, Longitude = 16.363449, Country = "Austria" },
            new() { Name = "Barcelona", Latitude = 41.390205, Longitude = 2.154007, Country = "Spain" }
        };
        _wireMockFixture.Fake<IGoogleHTTPClient>()
            .WithRequest(x => x.GetDurationMatrixByFreeRoad(cities))
            .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.FreeDistanceMatrix));
        _wireMockFixture.Fake<IGoogleHTTPClient>()
            .WithRequest(x => x.GetDurationMatrixByTollRoad(cities))
            .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.TollDistanceMatrix));

        // Act
        using var response = await BestPath(cities).Post();
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(body).RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        var paths = JsonSerializer.Deserialize<List<Path>>(body, JsonSerializerOptions.Web)!;
        paths.Should().HaveCount(3);
        paths[0].StartingCity.Name.Should().Be("Wroclaw");
        paths[0].EndingCity.Name.Should().Be("Vienna");
    }

    [Test]
    public async Task CalculateBestPathV1_NoCities_ReturnsProblemDetails()
    {
        // Act
        using var response = await BestPath(null).Post();
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonDocument.Parse(body).RootElement;
        problem.ValueKind.Should().Be(JsonValueKind.Object);
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).Should().Contain("Cities");
    }

    private RequestBuilder BestPath(List<City>? cities) => _apiClient
        .CreateRequest("/api/CalculateBestPath")
        .WithHeader("X-API-KEY", "<SECRET>")
        .WithHeader("X-API-VERSION", "1.0")
        .WithBody(new { Cities = cities });
}
