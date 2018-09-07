using System.Collections.Generic;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using Xunit;

namespace DreamTravelITests.ExternalConnection
{
    public class ApiCallerTests
    {
        readonly CallAPI _sut = new CallAPI();

        [Fact]
        public void DowloadDurationMatrixByTollRoad_InvokeWithValidCities_ReturnsSomeDuration()
        {
            //Arrange
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

            var list = new List<City> {firstCity, secondCity};

            //Act
            var result = _sut.DowloadDurationMatrixByTollRoad(list);

            //Assert
            Assert.NotEqual(0, result[1]);
            Assert.Equal(0, result[0]);
        }

        [Fact]
        public void DowloadDurationMatrixByFreeRoad_InvokeWithValidCities_ReturnsSomeDuration()
        {
            //Arrange
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

            var list = new List<City> { firstCity, secondCity };

            //Act
            var result = _sut.DowloadDurationMatrixByFreeRoad(list);

            //Assert
            Assert.NotEqual(0, result[1]);
            Assert.Equal(0, result[0]);
        }

        [Fact]
        public void DowloadCostBetweenTwoCities_InvokeWithValidCities_ReturnsSomeCost()
        {
            //Arrange
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

            //Act
            var result = _sut.DowloadCostBetweenTwoCities(firstCity, secondCity);

            //Assert
            Assert.NotEqual(0, result);
        }
    }
}
