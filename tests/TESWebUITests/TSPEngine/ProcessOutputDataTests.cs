using System.Collections.Generic;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using Xunit;

namespace TESWebUITests.TSPEngine
{
    public class ProcessOutputDataTests
    {
        private readonly ProcessOutputData _sut = new ProcessOutputData();

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
            List<City> listOfCities = new List<City> {firstCity, secondCity, thirdCity};

            int[] orderOfCities = new int[noOfCities];
            orderOfCities[0] = 0;
            orderOfCities[1] = 2;
            orderOfCities[2] = 1;

            EvaluationMatrix evaluationMatrix = new EvaluationMatrix(noOfCities);

            //Act
            var result = _sut.FormOutputFromTSFResult(listOfCities, orderOfCities, evaluationMatrix);

            //Assert
            Assert.Equal(noOfCities-1, result.Count);
            Assert.Equal("first", result[0].StartingCity.Name);
            Assert.Equal("third", result[1].StartingCity.Name);
            Assert.Equal("second", result[1].EndingCity.Name);
        }
    }
}
