using DreamTravel.BestPath.Models;
using System.Linq;
using Xunit;

namespace TravelingSalesmanProblemTests
{
    using System;

    public class GodTests
    {
        readonly TravelingSalesmanProblem.God _sut = new TravelingSalesmanProblem.God();

        [Fact]
        public void SolveTSP_RunWithValidParameters_FirstAndLastCitiesStaysTheSame()
        {
            //Arrange
            int noOfCities = 4;
            EvaluationMatrix evaluationMatrix = new EvaluationMatrix(noOfCities * noOfCities);

            Random random = new Random();
            for (int i = 0; i < noOfCities * noOfCities; i++)
            {
                evaluationMatrix.OptimalDistances[i] = random.NextDouble() * 1000;
            }


            //Act
            var result = _sut.SolveTSP(evaluationMatrix.OptimalDistances.ToList());


            //Assert
            Assert.Equal(0, result[0]);
            Assert.Equal(noOfCities - 1, result.Last());
        }

        [Fact]
        public void SolveTSP_RunWithValidParameters_EachCityAppearsOnlyOnce()
        {
            //Arrange
            int noOfCities = 7;
            EvaluationMatrix evaluationMatrix = new EvaluationMatrix(noOfCities * noOfCities);

            Random random = new Random();
            for (int i = 0; i < noOfCities * noOfCities; i++)
            {
                evaluationMatrix.OptimalDistances[i] = (random.NextDouble() * 1000);
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
