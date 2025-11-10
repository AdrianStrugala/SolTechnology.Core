using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Domain.SearchStatistics;
using DreamTravel.Trips.Queries.GetSearchStatistics;
using DreamTravel.Trips.Sql;
using DreamTravel.Trips.Sql.DbModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.CQRS;

namespace DreamTravel.FunctionalTests.Trips;

public class StatisticsTests
{
    private HttpClient _apiClient;
    private DreamTripsDbContext _dbContext;

    [SetUp]
    public void Setup()
    {
        _apiClient = ComponentTestsFixture.ApiFixture.ServerClient;

        var scope = ComponentTestsFixture.WorkerFixture.TestServer.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetService<DreamTripsDbContext>()!;
    }

    [Test]
    public async Task Get_Countries_Statistics()
    {
        // "Given is set of city and searches"
        var cities = new List<CityDetails>
        {
            new()
            {
                Name = "Warszawa",
                Latitude = 52.2297,
                Longitude = 21.0122,
                Country = "Polska",
                Region = "Mazowieckie",
                Population = 1_860_000
            },
            new CityDetails
            {
                Name = "Kraków",
                Latitude = 50.0647,
                Longitude = 19.9450,
                Country = "Polska",
                Region = "Małopolskie",
                Population = 800_000
            },
            new CityDetails
            {
                Name = "Gdańsk",
                Latitude = 54.3520,
                Longitude = 18.6466,
                Country = "Germany",
                Region = "Pomerania",
                Population = 470_000
            }
        };
        
        var statistics = new List<CityStatisticsEntity>
        {
            new()
            {
                CityId = 1,
                SearchCount = 3
            },
            new()
            {
                CityId = 2,
                SearchCount = 2
            },
            new()
            {
                CityId = 3,
                SearchCount = 2
            }
        };
        
        await _dbContext.Cities.AddRangeAsync(cities);
        await _dbContext.CityStatistics.AddRangeAsync(statistics);
        await _dbContext.SaveChangesAsync();
        
        // "When user queries for statistics
        var apiResponse = await _apiClient
            .CreateRequest("/api/v2/statistics/countries")
            .WithHeader("X-API-KEY", "<SECRET>")
            .GetAsync<Result<GetSearchStatisticsResult>>();
        
        
        // "Then the statistics matches expected result"
        var expected = new GetSearchStatisticsResult()
        {
            CountryStatistics = new List<CountryStatistics>()
            {
                new()
                {
                    Country = "Polska",
                    TotalSearchCount = 5
                },
                new()
                {
                    Country = "Germany",
                    TotalSearchCount = 2
                }
            }
        };
        
        apiResponse.IsSuccess.Should().BeTrue();
        apiResponse.Data!.Should().BeEquivalentTo(expected);
    }
}