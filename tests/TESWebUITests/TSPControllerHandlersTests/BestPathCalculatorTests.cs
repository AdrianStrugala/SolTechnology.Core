using System.Collections.Generic;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using DreamTravel.TSPControllerHandlers;
using NSubstitute;
using TravelingSalesmanProblem;
using Xunit;

namespace TESWebUITests.TSPControllerHandlersTests
{
    
    public class BestPathCalculatorTests
    {
        private readonly IProcessInputData _processInputData;
        private readonly IFormOutputDataForBestPath _formOutputDataForBestPath;
        private readonly ITSP _tspSolver;
        private readonly IEvaluationBrain _evaluationBrain;

        private readonly CalculateBestPath _sut;
        public BestPathCalculatorTests()
        {
            _processInputData = Substitute.For<IProcessInputData>();
            _formOutputDataForBestPath = Substitute.For<IFormOutputDataForBestPath>();
            _tspSolver = Substitute.For<ITSP>();
            _evaluationBrain = Substitute.For<IEvaluationBrain>();

            _sut = new CalculateBestPath(_processInputData, _formOutputDataForBestPath, _tspSolver, _evaluationBrain);
        }

        [Fact]
        void Handle_ValidData_AllCallsAreDone()
        {
            //Arrange
            _processInputData.Execute(Arg.Any<List<City>>(), Arg.Any<EvaluationMatrix>()).Returns(new EvaluationMatrix(1));
            _evaluationBrain.Execute(Arg.Any<EvaluationMatrix>(), Arg.Any<int>()).Returns(new EvaluationMatrix(1));
            _tspSolver.SolveTSP(Arg.Any<double[]>()).Returns(new int[1]);
            _formOutputDataForBestPath
                .Execute(Arg.Any<List<City>>(), Arg.Any<int[]>(), Arg.Any<EvaluationMatrix>())
                .Returns(new List<Path>());

            List<City> cities = new List<City> {new City {Name = "Wroclaw", Latitude = 21, Longitude = 37}};

            //Act
            var result = _sut.Execute(cities);

            //Assert
            Assert.NotNull(result);

            _processInputData.Received(1).Execute(Arg.Any<List<City>>(), Arg.Any<EvaluationMatrix>());
            _evaluationBrain.Received(1).Execute(Arg.Any<EvaluationMatrix>(), Arg.Any<int>());
            _tspSolver.Received(1).SolveTSP(Arg.Any<double[]>());
            _formOutputDataForBestPath.Received(1)
                .Execute(Arg.Any<List<City>>(), Arg.Any<int[]>(), Arg.Any<EvaluationMatrix>());
        }
    }
}
