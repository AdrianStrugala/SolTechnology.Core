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
        private readonly IProcessOutputData _processOutputData;
        private readonly ITSP _tspSolver;
        private readonly IEvaluationBrain _evaluationBrain;

        private readonly BestPathCalculator _sut;
        public BestPathCalculatorTests()
        {
            _processInputData = Substitute.For<IProcessInputData>();
            _processOutputData = Substitute.For<IProcessOutputData>();
            _tspSolver = Substitute.For<ITSP>();
            _evaluationBrain = Substitute.For<IEvaluationBrain>();

            _sut = new BestPathCalculator(_processInputData, _processOutputData, _tspSolver, _evaluationBrain);
        }

        [Fact]
        void Handle_ValidData_AllCallsAreDone()
        {
            //Arrange
            _processInputData.ReadCities(Arg.Any<string>()).Returns(new List<string>());
            _processInputData.GetCitiesFromGoogleApi(Arg.Any<List<string>>()).Returns(new List<City>());
            _processInputData.DownloadExternalData(Arg.Any<List<City>>(), Arg.Any<EvaluationMatrix>()).Returns(new EvaluationMatrix(1));
            _evaluationBrain.EvaluateCost(Arg.Any<EvaluationMatrix>(), Arg.Any<int>()).Returns(new EvaluationMatrix(1));
            _tspSolver.SolveTSP(Arg.Any<double[]>()).Returns(new int[1]);
            _processOutputData
                .FormOutputFromTSPResult(Arg.Any<List<City>>(), Arg.Any<int[]>(), Arg.Any<EvaluationMatrix>())
                .Returns(new List<Path>());

            string listOfCities = "someValidListOfCities";

            //Act
            var result = _sut.Handle(listOfCities);

            //Assert
            Assert.NotNull(result);

            _processInputData.Received(1).ReadCities(Arg.Any<string>());
            _processInputData.Received(1).GetCitiesFromGoogleApi(Arg.Any<List<string>>());
            _processInputData.Received(1).DownloadExternalData(Arg.Any<List<City>>(), Arg.Any<EvaluationMatrix>());
            _evaluationBrain.Received(1).EvaluateCost(Arg.Any<EvaluationMatrix>(), Arg.Any<int>());
            _tspSolver.Received(1).SolveTSP(Arg.Any<double[]>());
            _processOutputData.Received(1)
                .FormOutputFromTSPResult(Arg.Any<List<City>>(), Arg.Any<int[]>(), Arg.Any<EvaluationMatrix>());
        }
    }
}
