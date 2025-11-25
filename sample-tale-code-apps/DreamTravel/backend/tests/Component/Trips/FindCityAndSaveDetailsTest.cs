using System.Diagnostics;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.GeolocationDataClients.GoogleApi;
using DreamTravel.Trips.Sql;
using DreamTravel.Trips.Sql.QueryBuilders;
using DreamTravel.FunctionalTests.FakeApis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Faker;
using Path = DreamTravel.Trips.Domain.Paths.Path;

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

        _wireMockFixture.Fake<IGoogleApiClient>()
            .WithRequest(x => x.GetLocationOfCity, city.Name)
            .WithResponse(x => x
                .WithSuccess()
                .WithBody(GoogleFakeApi.BuildGeocodingResponse(city)));

        var apiResponse = await _apiClient
            .CreateRequest("/api/v2/FindCityByName")
            .WithHeader("X-API-KEY", "<SECRET>")
            .WithBody(new { city.Name })
            .PostAsync<Result<City>>();

        apiResponse.IsSuccess.Should().BeTrue();
        apiResponse.Data.Should().BeEquivalentTo(city);

        
        // "Then background job is triggered, city details fetched and stored".x(() =>
        var storedCity =
            await Retry.Unless(
                async () => await _dbContext.Cities.WhereName(city.Name).FirstOrDefaultAsync(),
                cityDetails => cityDetails != null,
                TimeSpan.FromSeconds(10),
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

public static class Retry
{
    public static async Task<T?> Unless<T>(
        Func<Task<T>> action,
        Func<T, bool> condition,
        TimeSpan totalWaitTime,
        TimeSpan pauseInterval,
        CancellationToken ct = default)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        do
        {
            var result = await action();

            if (condition(result))
            {
                return result;
            }

            await Task.Delay(pauseInterval, ct);
        } while (stopwatch.Elapsed < totalWaitTime);

        return default;
    }
}