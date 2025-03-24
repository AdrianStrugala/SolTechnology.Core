using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.CalculateBestPath;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using NSubstitute;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.SuperChain;

namespace DreamTravel.Trips.Queries.UnitTests.CalculateBestPath
{
    public class CalculateBestPathTests
    {

        private readonly CalculateBestPathHandler _sut;
        private readonly IChainStep<CalculateBestPathContext> _downloadRoadData;
        private readonly IChainStep<CalculateBestPathContext> _formCalculateBestPathResult;
        private readonly IChainStep<CalculateBestPathContext> _tspSolver;
        private readonly IChainStep<CalculateBestPathContext> _evaluationBrain;
        private readonly IChainStep<CalculateBestPathContext> _initiateContext;
        private readonly IServiceProvider _serviceProvider;

        public CalculateBestPathTests()
        {
            var fixture = new Fixture().Customize(
                new AutoNSubstituteCustomization { ConfigureMembers = true });

            _initiateContext = fixture.Freeze<IChainStep<CalculateBestPathContext>>();
            _downloadRoadData = fixture.Freeze<IChainStep<CalculateBestPathContext>>();
            _formCalculateBestPathResult = fixture.Freeze<IChainStep<CalculateBestPathContext>>();
            _tspSolver = fixture.Freeze<IChainStep<CalculateBestPathContext>>();
            _evaluationBrain = fixture.Freeze<IChainStep<CalculateBestPathContext>>();

            _serviceProvider = fixture.Freeze<IServiceProvider>();
            
            _sut = fixture.Create<CalculateBestPathHandler>();
        }

        [Fact]
        public async Task Handle_ValidData_AllCallsAreDone()
        {
            //Arrange
            List<City> cities = new() { new() { Name = "Wroclaw", Latitude = 21, Longitude = 37 } };

            _serviceProvider.GetService(typeof(InitiateContext)).Returns(_initiateContext);
            _serviceProvider.GetService(typeof(DownloadRoadData)).Returns(_downloadRoadData);
            _serviceProvider.GetService(typeof(SolveTsp)).Returns(_tspSolver);
            _serviceProvider.GetService(typeof(FindProfitablePath)).Returns(_evaluationBrain);
            _serviceProvider.GetService(typeof(FormCalculateBestPathResult)).Returns(_formCalculateBestPathResult);
            
            
            _initiateContext.Execute(Arg.Any<CalculateBestPathContext>()).ReturnsForAnyArgs(Result.SuccessAsTask());
            _downloadRoadData.Execute(Arg.Any<CalculateBestPathContext>()).ReturnsForAnyArgs(Result.SuccessAsTask());
            _tspSolver.Execute(Arg.Any<CalculateBestPathContext>()).ReturnsForAnyArgs(Result.SuccessAsTask());
            _evaluationBrain.Execute(Arg.Any<CalculateBestPathContext>()).ReturnsForAnyArgs(Result.SuccessAsTask());
            _formCalculateBestPathResult.Execute(Arg.Any<CalculateBestPathContext>()).ReturnsForAnyArgs(Result.SuccessAsTask());


            //Act
            var result = await _sut.Handle(new CalculateBestPathQuery { Cities = cities! });


            //Assert
            Assert.True(result.IsSuccess);

            //TODO: below would not work, as we are freezing the same interface 5 times. It returns the same instance
            // await _initiateContext.Received(1).Execute(Arg.Any<CalculateBestPathContext>());
            // await _downloadRoadData.Received(1).Execute(Arg.Any<CalculateBestPathContext>());
            // await _evaluationBrain.Received(1).Execute(Arg.Any<CalculateBestPathContext>());
            // await _tspSolver.Received(1).Execute(Arg.Any<CalculateBestPathContext>());
            // await _formCalculateBestPathResult.Received(1).Execute(Arg.Any<CalculateBestPathContext>());
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
