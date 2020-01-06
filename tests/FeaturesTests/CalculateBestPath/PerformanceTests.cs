using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Matrices;
using DreamTravel.DreamTrips.CalculateBestPath;
using DreamTravel.DreamTrips.CalculateBestPath.Interfaces;
using DreamTravel.GeolocationData;
using DreamTravel.GeolocationData.Matrices;
using DreamTravel.TravelingSalesmanProblem;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DreamTravel.FeaturesTests.CalculateBestPath
{
    public class PerformanceTests
    {
        private readonly DreamTrips.CalculateBestPath.CalculateBestPath _sut;

        public PerformanceTests()
        {
            IMatrixRepository matrixRepository = new MatrixRepository(NullLogger<MatrixRepository>.Instance);
            DownloadRoadData downloadRoadData = new DownloadRoadData(matrixRepository);
            IFormPathsFromMatrices formOutputData = new FormPathsFromMatrices();

            ITSP tsp = new AntColony();

            FindProfitablePath evaluationBrain = new FindProfitablePath();

            _sut = new DreamTrips.CalculateBestPath.CalculateBestPath(downloadRoadData, formOutputData, tsp, evaluationBrain);
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
            var result = await _sut.Execute(cities);


            //Assert
            Assert.NotNull(result);
        }
    }
}
