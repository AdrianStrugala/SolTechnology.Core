using System;
using System.Linq;
using DreamTravel.BestPath.Models;
using TravelingSalesmanProblem;
using Xunit;

namespace TravelingSalesmanProblemTests
{
    [Collection("Benchmark")]
    public class AntColonyTests
    {
        readonly AntColony _sut = new AntColony();

        [Fact]
        public void SolveTSP_RunWithValidParameters_FirstAndLastCitiesStaysTheSame()
        {
            //Arrange
            Random random = new Random();

            int noOfCities = 4;
            EvaluationMatrix evaluationMatrix = new EvaluationMatrix(noOfCities);

            for (int i = 0; i < evaluationMatrix.OptimalDistances.Length; i++)
            {
                evaluationMatrix.OptimalDistances[i] = random.NextDouble() * 1000;
            }

            //Act
            var result = _sut.SolveTSP(evaluationMatrix.OptimalDistances.ToList());

            //Assert
            Assert.Equal(0, result[0]);
            Assert.Equal(noOfCities-1, result.Last());
        }

        [Fact]
        public void SolveTSP_RunWithValidParameters_EachCityAppearsOnlyOnce()
        {
            //Arrange
            Random random = new Random();

            int noOfCities = 7;
            EvaluationMatrix evaluationMatrix = new EvaluationMatrix(noOfCities);

            for (int i = 0; i < evaluationMatrix.OptimalDistances.Length; i++)
            {
                evaluationMatrix.OptimalDistances[i] = random.NextDouble() * 1000;
            }

            //Act
            var result = _sut.SolveTSP(evaluationMatrix.OptimalDistances.ToList());

            //Assert
            Assert.Equal(1, result.Count(i => i.Equals(0)));
            Assert.Equal(1, result.Count(i => i.Equals(1)));
            Assert.Equal(1, result.Count(i => i.Equals(2)));
            Assert.Equal(1, result.Count(i => i.Equals(3)));
            Assert.Equal(1, result.Count(i => i.Equals(4)));
            Assert.Equal(1, result.Count(i => i.Equals(5)));
            Assert.Equal(1, result.Count(i => i.Equals(6)));
        }
    }
}
