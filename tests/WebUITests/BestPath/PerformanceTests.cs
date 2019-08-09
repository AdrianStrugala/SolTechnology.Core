namespace DreamTravel.WebUITests.BestPath
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging.Abstractions;
    using TravelingSalesmanProblem;
    using WebUI.BestPath.DataAccess;
    using WebUI.BestPath.Executors;
    using WebUI.BestPath.Interfaces;
    using WebUI.Contract;
    using Xunit;

    public class PerformanceTests
    {
        private readonly CalculateBestPath _sut;

        public PerformanceTests()
        {
            DownloadDurationMatrixByFreeRoad downloadDurationMatrixByFreeRoad = new DownloadDurationMatrixByFreeRoad(NullLogger<DownloadDurationMatrixByFreeRoad>.Instance);
            DownloadDurationMatrixByTollRoad downloadDurationMatrixByTollRoad = new DownloadDurationMatrixByTollRoad(NullLogger<DownloadDurationMatrixByTollRoad>.Instance);
            DownloadCostBetweenTwoCities downloadCostBetweenTwoCities = new DownloadCostBetweenTwoCities(NullLogger<DownloadCostBetweenTwoCities>.Instance);

            DownloadRoadData downloadRoadData = new DownloadRoadData(downloadDurationMatrixByTollRoad, downloadDurationMatrixByFreeRoad, downloadCostBetweenTwoCities);

            IFormOutputData formOutputData = new FormPathsFromMatrices();

            ITSP tsp = new AntColony();

            EvaluationBrain evaluationBrain = new EvaluationBrain();

            _sut = new CalculateBestPath(downloadRoadData, formOutputData, tsp, evaluationBrain);
        }

        [Fact]
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
            var result = await _sut.Execute(cities, true);


            //Assert
            Assert.NotNull(result);
        }
    }
}
