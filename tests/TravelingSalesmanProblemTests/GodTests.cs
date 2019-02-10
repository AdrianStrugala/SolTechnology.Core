namespace TravelingSalesmanProblemTests
{
    using System;
    using System.Linq;
    using Xunit;

    public class GodTests
    {
        readonly TravelingSalesmanProblem.God _sut = new TravelingSalesmanProblem.God();

        [Fact]
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
            Assert.Equal(0, result[0]);
            Assert.Equal(noOfCities - 1, result.Last());
        }

        [Fact]
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
