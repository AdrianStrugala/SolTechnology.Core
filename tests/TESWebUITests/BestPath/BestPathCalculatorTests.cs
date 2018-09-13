using System.Collections.Generic;
using DreamTravel.BestPath;
using DreamTravel.BestPath.Interfaces;
using DreamTravel.BestPath.Models;
using DreamTravel.ExternalConnection.Interfaces;
using DreamTravel.SharedModels;
using NSubstitute;
using TravelingSalesmanProblem;
using Xunit;

namespace DreamTravelITests.BestPath
{
    
    public class BestPathCalculatorTests
    {
        private readonly IDownloadRoadData _downloadRoadData;
        private readonly IFormOutputData _formOutputData;
        private readonly ITSP _tspSolver;
        private readonly IEvaluationBrain _evaluationBrain;

        private readonly CalculateBestPath _sut;
        public BestPathCalculatorTests()
        {
            _downloadRoadData = Substitute.For<IDownloadRoadData>();
            _formOutputData = Substitute.For<IFormOutputData>();
            _tspSolver = Substitute.For<ITSP>();
            _evaluationBrain = Substitute.For<IEvaluationBrain>();

            _sut = new CalculateBestPath(_downloadRoadData, _formOutputData, _tspSolver, _evaluationBrain);
        }

        [Fact]
        void Handle_ValidData_AllCallsAreDone()
        {
            //Arrange
            _downloadRoadData.Execute(Arg.Any<List<City>>(), Arg.Any<EvaluationMatrix>()).Returns(new EvaluationMatrix(1));
            _evaluationBrain.Execute(Arg.Any<EvaluationMatrix>(), Arg.Any<int>()).Returns(new EvaluationMatrix(1));
            _tspSolver.SolveTSP(Arg.Any<double[]>()).Returns(new int[1]);
            _formOutputData
                .Execute(Arg.Any<List<City>>(), Arg.Any<int[]>(), Arg.Any<EvaluationMatrix>())
                .Returns(new List<Path>());

            List<City> cities = new List<City> {new City {Name = "Wroclaw", Latitude = 21, Longitude = 37}};

            //Act
            var result = _sut.Execute(cities);

            //Assert
            Assert.NotNull(result);

            _downloadRoadData.Received(1).Execute(Arg.Any<List<City>>(), Arg.Any<EvaluationMatrix>());
            _evaluationBrain.Received(1).Execute(Arg.Any<EvaluationMatrix>(), Arg.Any<int>());
            _tspSolver.Received(1).SolveTSP(Arg.Any<double[]>());
            _formOutputData.Received(1)
                .Execute(Arg.Any<List<City>>(), Arg.Any<int[]>(), Arg.Any<EvaluationMatrix>());
        }
    }
}
