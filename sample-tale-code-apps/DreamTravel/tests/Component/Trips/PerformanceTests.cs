using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.Queries.CalculateBestPath;
using FluentAssertions;
using NUnit.Framework;
// using NSubstitute;

namespace DreamTravel.FunctionalTests.Trips
{
    public class PerformanceTests
    {
        private readonly CalculateBestPathStory _sut;

        public PerformanceTests()
        {
            // IGoogleHTTPClient googleHTTPClient = new GoogleHTTPClient(Options.Create(new GoogleHTTPOptions()), Substitute.For<HttpClient>(), NullLogger<GoogleHTTPClient>.Instance);
            // IMichelinHTTPClient michelinHTTPClient = new MichelinHTTPClient(Options.Create(new MichelinHTTPOptions()), Substitute.For<HttpClient>(), NullLogger<MichelinHTTPClient>.Instance);
            //
            // DownloadRoadData downloadRoadData = new DownloadRoadData(googleApiClient, michelinApiClient);
            // IFormCalculateBestPathResult formOutputData = new FormPathsFromMatrices();
            //
            // ITSP tsp = new AntColony();
            //
            // FindProfitablePath evaluationBrain = new FindProfitablePath();
            //
            // _sut = new CalculateBestPathHandler(downloadRoadData, formOutputData, tsp, evaluationBrain);
        }

        // [Fact(Skip = "Manual test")]
        public async Task Performance_TestFor10Cities_UseWithResharperProfiling_ForFindingOptimizations()
        {
            //Arrange

            List<City> cities = new List<City>
            {
                new City { Name = "Wroclaw", Latitude = 51, Longitude = 17 },
                new City { Name = "Warsaw", Latitude = 52, Longitude = 21 },
                new City { Name = "Gdańsk", Latitude = 54, Longitude = 18 },
                new City { Name = "Rzeszów", Latitude = 50, Longitude = 22 },
                new City { Name = "Lublin", Latitude = 51, Longitude = 22 },
                new City { Name = "Poznan", Latitude = 52, Longitude = 16 },
                new City { Name = "Kraków", Latitude = 50, Longitude = 20 },
                new City { Name = "Białystok", Latitude = 53, Longitude = 53 },
                new City { Name = "Łódź", Latitude = 51, Longitude = 19 },
                new City { Name = "Opole", Latitude = 50, Longitude = 17 }
            };

            //Act
            var result = await _sut.Handle(new CalculateBestPathQuery { Cities = cities });


            //Assert
            result.Should().NotBeNull();
        }
    }
}
