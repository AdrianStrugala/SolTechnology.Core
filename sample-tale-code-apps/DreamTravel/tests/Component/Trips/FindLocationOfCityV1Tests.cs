using System.Net;
using System.Text;
using System.Text.Json;
using DreamTravel.Domain.Cities;
using DreamTravel.FunctionalTests.FakeApis;
using DreamTravel.GeolocationDataClients.GoogleApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Testing;
using SolTechnology.Core.HTTP.Testing;

namespace DreamTravel.FunctionalTests.Trips;

public class FindLocationOfCityV1Tests
{
    private HttpClient _apiClient = null!;
    private IServiceProvider _apiServices = null!;
    private WireMockFixture _wireMockFixture = null!;

    [SetUp]
    public void Setup()
    {
        _apiClient = ComponentTestsFixture.ApiFixture.ServerClient;
        _apiServices = ComponentTestsFixture.ApiFixture.TestServer.Services;
        _wireMockFixture = ComponentTestsFixture.WireMockFixture;
    }

    [Test]
    public async Task FindLocationOfCity_KnownCity_ReturnsCityAndLogsCityName()
    {
        // Arrange
        var city = new City { Name = "V1City" + Guid.NewGuid().ToString("N")[..8], Latitude = 50.06, Longitude = 19.94, Country = "Poland" };
        _wireMockFixture.Fake<IGoogleHTTPClient>()
            .WithRequest(x => x.GetLocationOfCity(city.Name))
            .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.BuildGeocodingResponse(city)));

        // Act
        using var response = await FindLocation(city.Name);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonSerializer.Deserialize<City>(body, JsonSerializerOptions.Web).Should().BeEquivalentTo(city);
        var controllerLog = _apiServices.GetFakeLogCollector().GetSnapshot()
            .Should().ContainSingle(r => r.Message == "Looking for city: " + city.Name).Which;
        controllerLog.Scopes.OfType<IEnumerable<KeyValuePair<string, object?>>>().SelectMany(s => s)
            .Should().Contain(new KeyValuePair<string, object?>("CityName", city.Name));
    }

    [Test]
    public async Task FindLocationOfCity_EmptyName_ReturnsValidationProblemDetails()
    {
        // Act
        using var response = await FindLocation("");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonDocument.Parse(body).RootElement;
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).Should().Contain("Name");
    }

    [Test]
    public async Task FindLocationOfCity_UnknownCity_ReturnsNotFoundProblemDetails()
    {
        // Arrange
        var name = "V1Unknown" + Guid.NewGuid().ToString("N")[..8];
        _wireMockFixture.Fake<IGoogleHTTPClient>()
            .WithRequest(x => x.GetLocationOfCity(name))
            .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.ZeroResultsResponse));

        // Act
        using var response = await FindLocation(name);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        JsonDocument.Parse(body).RootElement.GetProperty("status").GetInt32().Should().Be(404);
    }

    // A buffered body with Content-Length: LogDetail reads the city name only from such a body.
    private Task<HttpResponseMessage> FindLocation(string name)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/FindLocationOfCity")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { name }), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-API-KEY", "<SECRET>");
        request.Headers.Add("X-API-VERSION", "1.0");
        return _apiClient.SendAsync(request);
    }
}
