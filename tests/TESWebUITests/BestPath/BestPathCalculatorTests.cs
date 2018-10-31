using DreamTravel.BestPath;
using DreamTravel.BestPath.Interfaces;
using DreamTravel.BestPath.Models;
using DreamTravel.ExternalConnection.Interfaces;
using DreamTravel.SharedModels;
using NSubstitute;
using System.Collections.Generic;
using TravelingSalesmanProblem;
using Xunit;

namespace DreamTravelITests.BestPath
{

    public class BestPathCalculatorTests
    {
        private readonly IDownloadRoadData _downloadRoadData;
        private readonly ITSP _tspSolver;
        private readonly IEvaluationBrain _evaluationBrain;

        private readonly CalculateBestPath _sut;
        public BestPathCalculatorTests()
        {
            _downloadRoadData = Substitute.For<IDownloadRoadData>();
            IFormOutputData formOutputData = new FormOutputData();
            _tspSolver = Substitute.For<ITSP>();
            _evaluationBrain = Substitute.For<IEvaluationBrain>();

            _sut = new CalculateBestPath(_downloadRoadData, formOutputData, _tspSolver, _evaluationBrain);
        }

        [Fact]
        void Handle_ValidData_AllCallsAreDone()
        {
            //Arrange
            _downloadRoadData.Execute(Arg.Any<List<City>>(), Arg.Any<EvaluationMatrix>()).Returns(new EvaluationMatrix(1));
            _evaluationBrain.Execute(Arg.Any<EvaluationMatrix>(), Arg.Any<int>()).Returns(new EvaluationMatrix(1));
            _tspSolver.SolveTSP(Arg.Any<List<double>>()).Returns(new List<int> { 1 });

            List<City> cities = new List<City> { new City { Name = "Wroclaw", Latitude = 21, Longitude = 37 } };

            //Act
            var result = _sut.Execute(cities, true);

            //Assert
            Assert.NotNull(result);

            _downloadRoadData.Received(1).Execute(Arg.Any<List<City>>(), Arg.Any<EvaluationMatrix>());
            _evaluationBrain.Received(1).Execute(Arg.Any<EvaluationMatrix>(), Arg.Any<int>());
            _tspSolver.Received(1).SolveTSP(Arg.Any<List<double>>());
        }

        [Fact]
        void Handle_DoNotOptimizePath_ResultOrderIsTheSameAsInput()
        {
            //Arrange
            _downloadRoadData.Execute(Arg.Any<List<City>>(), Arg.Any<EvaluationMatrix>()).Returns(new EvaluationMatrix(3));
            _evaluationBrain.Execute(Arg.Any<EvaluationMatrix>(), Arg.Any<int>()).Returns(new EvaluationMatrix(3));
            List<int> order = new List<int> { 0, 2, 1 };
            _tspSolver.SolveTSP(Arg.Any<List<double>>()).Returns(order);

            List<City> cities = new List<City>
            {
                new City { Name = "Wroclaw", Latitude = 21, Longitude = 37 },
                new City { Name = "WroclawNorth", Latitude = 22, Longitude = 37 },
                new City { Name = "WroclawSouth", Latitude = 20, Longitude = 37 }
            };

            //Act
            var result = _sut.Execute(cities, false);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(cities[0], result[0].StartingCity);
            Assert.Equal(cities[1], result[1].StartingCity);
            Assert.Equal(cities[2], result[1].EndingCity);
        }
    }
}
