using DreamTravel.Domain.Cities;
using DreamTravel.FunctionalTests.FakeApis;
using DreamTravel.GeolocationDataClients.GoogleApi;
using FluentAssertions;
using SolTechnology.Core.Faker;
using DreamTravel.Queries.CalculateBestPath;
using SolTechnology.Core.CQRS;

namespace DreamTravel.FunctionalTests.Trips
{
    public class CalculateBestPathFeatureTest
    {
        private HttpClient _apiClient;
        private WireMockFixture _wireMockFixture;

        [SetUp]
        public void Setup()
        {
            _apiClient = ComponentTestsFixture.ApiFixture.ServerClient;
            _wireMockFixture = ComponentTestsFixture.WireMockFixture;
        }

        [Test]
        public async Task FindsOptimalPath()
        {
            // "Given is list of cities".x(() =>
            var cities = new List<City>
            {
                new() { Name = "Wroclaw", Latitude = 51.107883, Longitude = 17.038538, Country = "Poland" },
                new() { Name = "Firenze", Latitude = 43.769562, Longitude = 11.255814, Country = "Italy" },
                new() { Name = "Vienna", Latitude = 48.210033, Longitude = 16.363449, Country = "Austria" },
                new() { Name = "Barcelona", Latitude = 41.390205, Longitude = 2.154007, Country = "Spain" }
            };

            // "Given is fake google city API".x(() =>
            foreach (var city in cities)
            {
                _wireMockFixture.Fake<IGoogleHTTPClient>()
                    .WithRequest(x => x.GetLocationOfCity, city.Name)
                    .WithResponse(x => x
                        .WithSuccess()
                        .WithBody(GoogleFakeApi.BuildGeocodingResponse(city)));
            }

            // "Given is fake google distance API".x(() =>
            _wireMockFixture.Fake<IGoogleHTTPClient>()
                .WithRequest(x => x.GetDurationMatrixByFreeRoad, cities)
                .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.FreeDistanceMatrix));

            _wireMockFixture.Fake<IGoogleHTTPClient>()
                .WithRequest(x => x.GetDurationMatrixByTollRoad, cities)
                .WithResponse(x => x.WithSuccess().WithBody(GoogleFakeApi.TollDistanceMatrix));

            // "When user searches for location of each of the cities".x(async () =>
            foreach (var city in cities)
            {
                var findCityByNameResponse = await _apiClient
                    .CreateRequest("/api/FindCityByName")
                    .WithHeader("X-API-KEY", "<SECRET>")
                    .WithHeader("X-API-VERSION", "2.0")
                    .WithBody(new { city.Name })
                    .PostAsync<Result<City>>();

                findCityByNameResponse.IsSuccess.Should().BeTrue();
                findCityByNameResponse.Data.Should().BeEquivalentTo(city);
            }

            // "And when user searches for the best path".x(async () =>
            var apiResponse = await _apiClient
                .CreateRequest("/api/CalculateBestPath")
                .WithHeader("X-API-KEY", "<SECRET>")
                .WithHeader("X-API-VERSION", "2.0")
                .WithBody(new { Cities = cities })
                .PostAsync<Result<CalculateBestPathResult>>();
            apiResponse.IsSuccess.Should().BeTrue();
            var paths = apiResponse.Data.BestPaths;

            // "Then returned path is optimal".x(() =>
            paths[0].StartingCity.Name.Should().Be("Wroclaw");
            paths[0].StartingCity.Country.Should().Be("Poland");
            paths[0].EndingCity.Name.Should().Be("Vienna");
            paths[0].EndingCity.Country.Should().Be("Austria");

            paths[1].StartingCity.Name.Should().Be("Vienna");
            paths[1].StartingCity.Country.Should().Be("Austria");
            paths[1].EndingCity.Name.Should().Be("Firenze");
            paths[1].EndingCity.Country.Should().Be("Italy");

            paths[2].StartingCity.Name.Should().Be("Firenze");
            paths[2].StartingCity.Country.Should().Be("Italy");
            paths[2].EndingCity.Name.Should().Be("Barcelona");
            paths[2].EndingCity.Country.Should().Be("Spain");
        }
    }
}