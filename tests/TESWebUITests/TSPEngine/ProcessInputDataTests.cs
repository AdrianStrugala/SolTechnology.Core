using System.Collections.Generic;
using TESWebUI.ExternalConnection;
using TravelingSalesmanProblem.Models;
using Xunit;

namespace TESWebUITests.TSPEngine
{
    public class ProcessInputDataTests
    {
        private readonly ProcessInputData _sut = new ProcessInputData();
        [Fact]
        public void GetDurationBetweenTwoCitiesByTollRoad_InvokeWithValidCities_ReturnsSomeDuration()
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
            var result = _sut.GetDurationBetweenTwoCitiesByTollRoad(firstCity, secondCity);

            //Assert
            Assert.NotEqual(0,result);
        }

        [Fact]
        public void GetDurationBetweenTwoCitiesByFreeRoad_InvokeWithValidCities_ReturnsSomeDuration()
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
            var result = _sut.GetDurationBetweenTwoCitiesByFreeRoad(firstCity, secondCity);

            //Assert
            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetCostBetweenTwoCities_InvokeWithValidCities_ReturnsSomeCost()
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
            var result = _sut.GetCostBetweenTwoCities(firstCity, secondCity);

            //Assert
            Assert.NotEqual(0, result);
        }


        [Fact]
        public void GetCityByName_InvokeWithRealName_ReturnsCityObject()
        {
            //Arrange
            List<string> cityNames = new List<string>();
            cityNames.Add("Wroclaw");
            List<City> cities = new List<City>();

            //Act
            cities = _sut.GetCitiesFromGoogleApi(cityNames);

            //Assert
            Assert.Equal("Wroclaw", cities[0].Name);
            Assert.NotEqual(0, cities[0].Latitude);
            Assert.NotEqual(0, cities[0].Longitude);
        }


        [Fact]
        public void ReadCities_ProvideValidEntryString_ReturnsListOfCities()
        {
            //Arrange
            string entryString = "Wroclaw\nKrakow\rGdansk\r\nWarszawa";
            //Act
            var cities = _sut.ReadCities(entryString);

            //Assert
            Assert.Equal("Wroclaw", cities[0]);
            Assert.Equal("Krakow", cities[1]);
            Assert.Equal("Gdansk", cities[2]);
            Assert.Equal("Warszawa", cities[3]);
        }
    }
}
