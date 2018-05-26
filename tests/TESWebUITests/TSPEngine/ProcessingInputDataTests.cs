using System.Collections.Generic;
using TESWebUI.ExternalConnection;
using TravelingSalesmanProblem.Models;
using Xunit;

namespace TESWebUITests.TSPEngine
{
    public class ProcessingInputDataTests
    {
        [Fact]
        public void DurationBetweenTwoCitiesByTollRoadCall_ReturnsSomeDuration()
        {
            //Arrange
            ProcessInputData processingInputData = new ProcessInputData();

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
            var result = processingInputData.GetDurationBetweenTwoCitiesByTollRoad(firstCity, secondCity);

            //Assert
            Assert.NotEqual(0,result);
        }

        [Fact]
        public void GetDurationBetweenTwoCitiesByFreeRoadCall_ReturnsSomeDuration()
        {
            //Arrange
            ProcessInputData processingInputData = new ProcessInputData();

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
            var result = processingInputData.GetDurationBetweenTwoCitiesByFreeRoad(firstCity, secondCity);

            //Assert
            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetCostBetweenTwoCities_ReturnsAnyCost()
        {
            //Arrange
            ProcessInputData processingInputData = new ProcessInputData();

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
            var result = processingInputData.GetCostBetweenTwoCities(firstCity, secondCity);

            //Assert
            Assert.NotEqual(0, result);
        }


        [Fact]
        public void GetCityByName_ReturnsCityObject()
        {
            //Arrange
            ProcessInputData processingInputData = new ProcessInputData();
            List<string> cityNames = new List<string>();
            cityNames.Add("Wroclaw");
            List<City> cities = new List<City>();

            //Act
            cities = processingInputData.GetCitiesFromGoogleApi(cityNames);

            //Assert
            Assert.Equal("Wroclaw", cities[0].Name);
            Assert.NotEqual(0, cities[0].Latitude);
            Assert.NotEqual(0, cities[0].Longitude);
        }
    }
}
