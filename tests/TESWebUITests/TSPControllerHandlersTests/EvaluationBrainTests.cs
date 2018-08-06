using System;
using DreamTravel.Models;
using DreamTravel.TSPControllerHandlers;
using Xunit;

namespace TESWebUITests.TSPControllerHandlersTests
{
    public class EvaluationBrainTests
    {
        private readonly EvaluationBrain _sut;

        public EvaluationBrainTests()
        {
            _sut = new EvaluationBrain();
        }

        [Fact]
        public void EvaluateCost_ValidData_IntelligentResults()
        {
            //Arrange
            int noOfCities = 3;
            EvaluationMatrix matrix = new EvaluationMatrix(noOfCities);
            matrix.Costs = new[]
            {
                Double.MaxValue, 10, 19,
                10, Double.MaxValue, 30,
                19, 30,  Double.MaxValue
            };

            matrix.FreeDistances = new[]
            {
                Double.MaxValue, 100, 200,
                100, Double.MaxValue, 300,
                200, 300,  Double.MaxValue
            };

            matrix.TollDistances = new[]
            {
                Double.MaxValue, 90, 20,
                90, Double.MaxValue, 350,
                20, 350,  Double.MaxValue
            };


            //Act 
            var result = _sut.Execute(matrix, noOfCities);


            //Assert
            //1) takes profitable road
            Assert.Equal(20, result.OptimalDistances[2]);
            Assert.Equal(19, result.OptimalCosts[2]);
            Assert.Equal(20, result.OptimalDistances[6]);
            Assert.Equal(19, result.OptimalCosts[6]);

            //2) Rejects non-profitable road
            Assert.Equal(100, result.OptimalDistances[1]);
            Assert.Equal(0, result.OptimalCosts[1]);
            Assert.Equal(100, result.OptimalDistances[3]);
            Assert.Equal(0, result.OptimalCosts[3]);

            //3) Rejects toll road longer than free
            Assert.Equal(300, result.OptimalDistances[5]);
            Assert.Equal(0, result.OptimalCosts[5]);
            Assert.Equal(300, result.OptimalDistances[7]);
            Assert.Equal(0, result.OptimalCosts[7]);
        }
    }
}
