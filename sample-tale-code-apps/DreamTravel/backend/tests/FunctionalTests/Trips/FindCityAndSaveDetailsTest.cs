using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DreamTravel.Trips.Domain.Cities;
using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.Trips.Domain.Paths;
using DreamTravel.Trips.Sql;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SolTechnology.Core.Faker;
using SolTechnology.Core.CQRS;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.FunctionalTests.Trips
{
    public class FindCityAndSaveDetailsTest
    {
        private HttpClient _apiClient;
        private WireMockFixture _wireMockFixture;
        private DreamTripsDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            _apiClient = IntegrationTestsFixture.ApiFixture.ServerClient;
            _wireMockFixture = IntegrationTestsFixture.WireMockFixture;

            var scope = IntegrationTestsFixture.WorkerFixture.TestServer.Services.CreateScope();
            _dbContext = scope.ServiceProvider.GetService<DreamTripsDbContext>();
        }

        [Test]
        public async Task Search_and_store_details()
        {
            List<Path> paths = null;

            City city = new()
            {
                Name = "Wrocław",
                Latitude = 51.107883,
                Longitude = 17.038538
            };

            _wireMockFixture.Fake<IGoogleApiClient>()
                .WithRequest(x => x.GetLocationOfCity, city.Name)
                .WithResponse(x => x.WithSuccess().WithBody(
                    $@"{{
                           ""results"" : 
                           [
                              {{
                                 ""geometry"" : 
                                 {{
                                    ""location"" : 
                                    {{
                                       ""lat"" : {city.Latitude},
                                       ""lng"" : {city.Longitude}
                                    }}
                                 }}
                              }}
                           ],
                           ""status"" : ""OK""
                        }}"));

            var apiResponse = await _apiClient
                .CreateRequest("/api/v2/FindCityByName")
                .WithHeader("Authorization", "<SECRET>")
                .WithBody(new { city.Name })
                .PostAsync<Result<City>>();

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Data.Should().BeEquivalentTo(city);



            // "Then background job is triggered, city details fetched and stored".x(() =>

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            CityDetails storedCity;

            do
            {
                storedCity = await _dbContext.Cities.FirstOrDefaultAsync(c => c.Name == city.Name);
            } while (storedCity == null && stopwatch.Elapsed.TotalSeconds < 10);

            storedCity.Should().NotBeNull();
            storedCity!.Name.Should().Be(city.Name);
            storedCity.Country.Should().Be("Poland");
            storedCity.Population.Should().BeGreaterThan(600000);
            ;
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
        }
    }
}
