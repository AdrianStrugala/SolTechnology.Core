using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace DreamTravel.TravelingSalesmanProblem.UnitTests
{
    [TestFixture]
    public class GodTests
    {
        readonly DreamTravel.TravelingSalesmanProblem.God _sut = new DreamTravel.TravelingSalesmanProblem.God();

        [Test]
        public void SolveTSP_RunWithValidParameters_FirstAndLastCitiesStaysTheSame()
        {
            //Arrange
            Random random = new Random();

            int noOfCities = 4;
            double[] distances = new double[noOfCities * noOfCities];
            for (int i = 0; i < distances.Length; i++)
            {
                distances[i] = random.NextDouble() * 1000;
            }

            //Act
            var result = _sut.SolveTSP(distances.ToList());

            //Assert
            result[0].Should().Be(0);
            result.Last().Should().Be(noOfCities - 1);
        }

        [Test]
        public void SolveTSP_RunWithValidParameters_EachCityAppearsOnlyOnce()
        {
            //Arrange
            Random random = new Random();

            int noOfCities = 7;

            double[] distances = new double[noOfCities * noOfCities];
            for (int i = 0; i < distances.Length; i++)
            {
                distances[i] = random.NextDouble() * 1000;
            }

            //Act
            var result = _sut.SolveTSP(distances.ToList());

            //Assert
            for (int i = 0; i < noOfCities; i++)
            {
                result.Count(x => x.Equals(i)).Should().Be(1);
            }
        }
    }
}
