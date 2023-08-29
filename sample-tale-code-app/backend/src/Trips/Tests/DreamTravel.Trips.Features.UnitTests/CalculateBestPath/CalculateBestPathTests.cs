using System.Collections.Generic;
using DreamTravel.DreamTrips.CalculateBestPath;
using DreamTravel.DreamTrips.CalculateBestPath.Executors;
using DreamTravel.DreamTrips.CalculateBestPath.Interfaces;
using DreamTravel.TravelingSalesmanProblem;
using DreamTravel.Trips.Domain.Cities;
using NSubstitute;
using Xunit;

namespace DreamTravel.Trips.Commands.UnitTests.CalculateBestPath
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

            _sut = new DreamTrips.CalculateBestPath.CalculateBestPathHandler(_downloadRoadData, formPathsFromMatrices, _tspSolver, _evaluationBrain);
        }

        [Fact]
        public void Handle_ValidData_AllCallsAreDone()
        {
            //Arrange
            _downloadRoadData.Execute(Arg.Any<List<City>>()).Returns(new EvaluationMatrix(1));
            _evaluationBrain.Execute(Arg.Any<EvaluationMatrix>(), Arg.Any<int>()).Returns(new EvaluationMatrix(1));
            _tspSolver.SolveTSP(Arg.Any<List<double>>()).Returns(new List<int> { 1 });

            List<City> cities = new List<City> { new City { Name = "Wroclaw", Latitude = 21, Longitude = 37 } };

            //Act
            var result = _sut.Handle(new CalculateBestPathQuery { Cities = cities });


            //Assert
            Assert.NotNull(result);

            _downloadRoadData.Received(1).Execute(Arg.Any<List<City>>());
            _evaluationBrain.Received(1).Execute(Arg.Any<EvaluationMatrix>(), Arg.Any<int>());
            _tspSolver.Received(1).SolveTSP(Arg.Any<List<double>>());
        }

        //        [Fact]
        //        async Task Handle_DoNotOptimizePath_ResultOrderIsTheSameAsInput()
        //        {
        //            //Arrange
        //            _downloadRoadData.DownloadCostBetweenTwoCities(Arg.Any<List<City>>(), Arg.Any<EvaluationMatrix>()).Returns(new EvaluationMatrix(3));
        //            _evaluationBrain.DownloadCostBetweenTwoCities(Arg.Any<EvaluationMatrix>(), Arg.Any<int>()).Returns(new EvaluationMatrix(3));
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
