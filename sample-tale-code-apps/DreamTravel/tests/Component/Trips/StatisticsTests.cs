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
        // Arrange
        var cities = new List<CityEntity>
        {
            new()
            {
                CityId = Auid.New("CTY"),
                Latitude = 52.2297,
                Longitude = 21.0122,
                Country = "Polska",
                AlternativeNames = new()
                {
                    new() { AlternativeName = "Warszawa" }
                },
                Statistics = new()
                {
                    new() { SearchCount = 3 }
                }
            },
            new()
            {
                CityId = Auid.New("CTY"),
                Latitude = 50.0647,
                Longitude = 19.9450,
                Country = "Polska",
                Statistics = new()
                {
                    new() { SearchCount = 2 }
                }
            },
            new()
            {
                CityId = Auid.New("CTY"),
                Latitude = 54.3520,
                Longitude = 18.6466,
                Country = "Germany",
                Statistics = new()
                {
                    new() { SearchCount = 2 }
                }
            }
        };
    
        await _dbContext.Cities.AddRangeAsync(cities);
        await _dbContext.SaveChangesAsync();
    
        // Act
        var apiResponse = await _apiClient
            .CreateRequest("/api/v2/statistics/countries")
            .WithHeader("X-API-KEY", "<SECRET>")
            .GetAsync<Result<GetSearchStatisticsResult>>();

        // Assert
        apiResponse.IsSuccess.Should().BeTrue();

        var polskaStats = apiResponse.Data!.CountryStatistics.FirstOrDefault(c => c.Country == "Polska");
        polskaStats.Should().NotBeNull();
        polskaStats!.TotalSearchCount.Should().BeGreaterOrEqualTo(5, "because we added 3 + 2 searches for Polska");

        var germanyStats = apiResponse.Data!.CountryStatistics.FirstOrDefault(c => c.Country == "Germany");
        germanyStats.Should().NotBeNull();
        germanyStats!.TotalSearchCount.Should().BeGreaterOrEqualTo(2, "because we added 2 searches for Germany");
    }
}