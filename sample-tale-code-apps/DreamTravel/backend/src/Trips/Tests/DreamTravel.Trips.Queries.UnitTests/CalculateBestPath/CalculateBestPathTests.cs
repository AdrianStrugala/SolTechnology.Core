using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.CalculateBestPath;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using NSubstitute;

namespace DreamTravel.Trips.Queries.UnitTests.CalculateBestPath
{
    public class CalculateBestPathTests
    {
        private readonly IDownloadRoadData _downloadRoadData;
        private readonly ITSP _tspSolver;
        private readonly IFindProfitablePath _evaluationBrain;

        private readonly CalculateBestPathHandler _sut;

        public CalculateBestPathTests()
        {
            _downloadRoadData = Substitute.For<IDownloadRoadData>();
            IFormPathsFromMatrices formPathsFromMatrices = new FormPathsFromMatrices();
            _tspSolver = Substitute.For<ITSP>();
            _evaluationBrain = Substitute.For<IFindProfitablePath>();

            _sut = new CalculateBestPathHandler(_downloadRoadData, formPathsFromMatrices, _tspSolver, _evaluationBrain);
        }

        [Fact]
        public void Handle_ValidData_AllCallsAreDone()
        {
            //Arrange
            _tspSolver.SolveTSP(Arg.Any<List<double>>()).Returns(new List<int> { 1 });

            List<City> cities = new List<City> { new City { Name = "Wroclaw", Latitude = 21, Longitude = 37 } };

            //Act
            var result = _sut.Handle(new CalculateBestPathQuery { Cities = cities });


            //Assert
            Assert.NotNull(result);

            _downloadRoadData.Received(1).Execute(Arg.Any<List<City>>(), Arg.Any<CalculateBestPathContext>());
            _evaluationBrain.Received(1).Execute(Arg.Any<CalculateBestPathContext>(), Arg.Any<int>());
            _tspSolver.Received(1).SolveTSP(Arg.Any<List<double>>());
        }

        //        [Fact]
        //        async Task Handle_DoNotOptimizePath_ResultOrderIsTheSameAsInput()
        //        {
        //            //Arrange
        //            _downloadRoadData.DownloadCostBetweenTwoCities(Arg.Any<List<City>>(), Arg.Any<CalculateBestPathContext>()).Returns(new CalculateBestPathContext(3));
        //            _evaluationBrain.DownloadCostBetweenTwoCities(Arg.Any<CalculateBestPathContext>(), Arg.Any<int>()).Returns(new CalculateBestPathContext(3));
        //            List<int> order = new List<int> { 0, 2, 1 };
        //            _tspSolver.SolveTSP(Arg.Any<List<double>>()).Returns(order);
        //
        //            List<City> cities = new List<City>
        //            {
        //                new City { Name = "Wroclaw", Latitude = 21, Longitude = 37 },
        //                new City { Name = "WroclawNorth", Latitude = 22, Longitude = 37 },
        //                new City { Name = "WroclawSouth", Latitude = 20, Longitude = 37 }
        //            };
        //            
        //            //Act
        //            var result = await _sut.DownloadCostBetweenTwoCities(cities, false);
        //            var bestPaths = result.BestPaths;
        //
        //
        //            //Assert
        //            Assert.NotNull(result);
        //            Assert.Equal(cities[0], bestPaths[0].StartingCity);
        //            Assert.Equal(cities[1], bestPaths[1].StartingCity);
        //            Assert.Equal(cities[2], bestPaths[1].EndingCity);
        //        }
    }
}
