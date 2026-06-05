using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationDataClients.GoogleApi;
using DreamTravel.Sql;
using DreamTravel.Sql.QueryBuilders;
using DreamTravel.FunctionalTests.FakeApis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.HTTP.Testing;
using SolTechnology.Core.Testing;

namespace DreamTravel.FunctionalTests.Trips;

public class FindCityAndSaveDetailsTest
{
    private HttpClient _apiClient;
    private DreamTripsDbContext _dbContext;
    private WireMockFixture _wireMockFixture;

    [SetUp]
    public void Setup()
    {
        _apiClient = ComponentTestsFixture.ApiFixture.ServerClient;
        _wireMockFixture = ComponentTestsFixture.WireMockFixture;

        var scope = ComponentTestsFixture.WorkerFixture.TestServer.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetService<DreamTripsDbContext>()!;
    }

    [Test]
    public async Task Search_and_store_details()
    {
        City city = new()
        {
            Name = "Wrocław",
            Latitude = 51.107883,
            Longitude = 17.038538,
            Country = "Poland"
        };

        _wireMockFixture.Fake<IGoogleHTTPClient>()
            .WithRequest(x => x.GetLocationOfCity(city.Name))
            .WithResponse(x => x
                .WithSuccess()
                .WithBody(GoogleFakeApi.BuildGeocodingResponse(city)));

        var apiResponse = await _apiClient
            .CreateRequest("/api/FindCityByName")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithHeader("X-API-VERSION", "2.0")
            .WithBody(new { city.Name })
            .PostAsync<City>();

        apiResponse.Should().BeEquivalentTo(city);


        // "Then background job is triggered, city details fetched and stored".x(() =>
        var storedCity =
            await Retry.UntilConditionMetOrTimeout(
                async () => await _dbContext.Cities
                    .Include(c => c.AlternativeNames)
                    .WhereName(city.Name)
                    .FirstOrDefaultAsync(),
                cityDetails => cityDetails != null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(1));

        storedCity.Should().NotBeNull();
        storedCity!.AlternativeNames.Select(x => x.AlternativeName).Should().Contain(city.Name);
        storedCity.Country.Should().Be("Poland");
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }
}

