using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.CalculateBestPath;
using Xunit;
// using NSubstitute;

namespace DreamTravel.FunctionalTests
{
    public class PerformanceTests
    {
        private readonly CalculateBestPathHandler _sut;

        public PerformanceTests()
        {
            // IGoogleApiClient googleApiClient = new GoogleApiClient(Options.Create(new GoogleApiOptions()), Substitute.For<HttpClient>(), NullLogger<GoogleApiClient>.Instance);
            // IMichelinApiClient michelinApiClient = new MichelinApiClient(Options.Create(new MichelinApiOptions()), Substitute.For<HttpClient>(), NullLogger<MichelinApiClient>.Instance);
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

        [Fact(Skip = "Manual test")]
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
            var result = await _sut.Handle(new CalculateBestPathQuery{Cities = cities});


            //Assert
            Assert.NotNull(result);
        }
    }
}
