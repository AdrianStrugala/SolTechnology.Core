using System;
using System.Linq;
using TravelingSalesmanProblem;
using TravelingSalesmanProblem.Models;
using Xunit;

namespace TravelingSalesmanProblemTests
{
    public class UnitTest1
    {
        readonly God _sut = new God();

        [Fact]
        public void SolveTSP_RunWithValidParameters_FirstAndLastCitiesStaysTheSame()
        {
            //Arrange
            int noOfCities = 4;
            DistanceMatrixEvaluated distanceMatrix = new DistanceMatrixEvaluated(noOfCities);

            //Act
            var result = _sut.SolveTSP(distanceMatrix);

            //Assert
            Assert.Equal(0, result[0]);
            Assert.Equal(noOfCities-1, result.Last());
        }

        [Fact]
        public void SolveTSP_RunWithValidParameters_EachCityAppearsOnlyOnce()
        {
            //Arrange
            int noOfCities = 7;
            DistanceMatrixEvaluated distanceMatrix = new DistanceMatrixEvaluated(noOfCities);

            //Act
            var result = _sut.SolveTSP(distanceMatrix);

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
