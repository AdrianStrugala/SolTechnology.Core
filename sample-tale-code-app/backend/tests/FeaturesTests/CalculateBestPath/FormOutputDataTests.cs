using System.Collections.Generic;
using DreamTravel.Domain.Cities;
using DreamTravel.DreamTrips.CalculateBestPath.Executors;
using Xunit;

namespace DreamTravel.FeaturesTests.CalculateBestPath
{
    public class FormPathsFromMatricesTests
    {
        private readonly FormPathsFromMatrices _sut = new FormPathsFromMatrices();

        [Fact]
        public void GetDurationBetweenTwoCitiesByTollRoad_InvokeWithValidCities_ReturnsSomeDuration()
        {
            //Arrange
            int noOfCities = 3;

            City firstCity = new City
            {
                Name = "first",
                Latitude = 51,
                Longitude = 17
            };

            City secondCity = new City
            {
                Name = "second",
                Latitude = 53,
                Longitude = 19
            };

            City thirdCity = new City
            {
                Name = "third",
                Latitude = 55,
                Longitude = 21
            };
            List<City> listOfCities = new List<City> { firstCity, secondCity, thirdCity };

            List<int> orderOfCities = new List<int>(noOfCities) { 0, 2, 1 };

            EvaluationMatrix evaluationMatrix = new EvaluationMatrix(noOfCities);

            //Act
            var result = _sut.Execute(listOfCities, evaluationMatrix, orderOfCities);

            //Assert
            Assert.Equal(noOfCities - 1, result.Count);
            Assert.Equal("first", result[0].StartingCity.Name);
            Assert.Equal("third", result[1].StartingCity.Name);
            Assert.Equal("second", result[1].EndingCity.Name);
        }
    }
}