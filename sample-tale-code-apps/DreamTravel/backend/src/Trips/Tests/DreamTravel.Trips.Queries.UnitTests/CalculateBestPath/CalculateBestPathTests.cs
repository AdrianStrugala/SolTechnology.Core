using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.CalculateBestPath;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using NSubstitute;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.UnitTests.CalculateBestPath
{
    public class CalculateBestPathTests
    {
        private readonly IDownloadRoadData _downloadRoadData;
        private readonly ISolveTsp _tspSolver;
        private readonly IFindProfitablePath _evaluationBrain;

        private readonly CalculateBestPathHandler _sut;
        private readonly IFormCalculateBestPathResult _formCalculateBestPathResult;

        public CalculateBestPathTests()
        {
            var fixture = new Fixture().Customize(
                new AutoNSubstituteCustomization { ConfigureMembers = true });

            _downloadRoadData = fixture.Freeze<IDownloadRoadData>();
            _formCalculateBestPathResult = fixture.Freeze<IFormCalculateBestPathResult>();
            _tspSolver = fixture.Freeze<ISolveTsp>();
            _evaluationBrain = fixture.Freeze<IFindProfitablePath>();

            _sut = fixture.Create<CalculateBestPathHandler>();
        }

        [Fact]
        public async Task Handle_ValidData_AllCallsAreDone()
        {
            //Arrange
            List<City> cities = new() { new() { Name = "Wroclaw", Latitude = 21, Longitude = 37 } };

            _downloadRoadData.Execute(Arg.Any<CalculateBestPathContext>()).ReturnsForAnyArgs(Result.SucceededTask());
            _tspSolver.Execute(Arg.Any<CalculateBestPathContext>()).ReturnsForAnyArgs(Result.SucceededTask());
            _evaluationBrain.Execute(Arg.Any<CalculateBestPathContext>()).ReturnsForAnyArgs(Result.SucceededTask());


            //Act
            var result = await _sut.Handle(new CalculateBestPathQuery { Cities = cities! });


            //Assert
            Assert.True(result.IsSuccess);

            await _downloadRoadData.Received(1).Execute(Arg.Any<CalculateBestPathContext>());
            await _evaluationBrain.Received(1).Execute(Arg.Any<CalculateBestPathContext>());
            await _tspSolver.Received(1).Execute(Arg.Any<CalculateBestPathContext>());
            _formCalculateBestPathResult.Received(1).Execute(Arg.Any<CalculateBestPathContext>());
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
