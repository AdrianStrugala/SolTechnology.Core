using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection;
using DreamTravel.Models;
using Xunit;

namespace TESWebUITests.ExternalConnection
{
    public class ProcessInputDataTests
    {
        private readonly ProcessInputData _sut; 

        public ProcessInputDataTests()
        {
            ICallAPI apiCaller = new CallAPI();

            _sut = new ProcessInputData(apiCaller);
        }
        [Fact]
        public async Task GetDurationBetweenTwoCitiesByTollRoad_InvokeWithValidCities_ReturnsSomeDuration()
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
            var result = await _sut.GetDurationBetweenTwoCitiesByTollRoad(firstCity, secondCity);

            //Assert
            Assert.NotEqual(0,result);
        }

        [Fact]
        public async Task GetDurationBetweenTwoCitiesByFreeRoad_InvokeWithValidCities_ReturnsSomeDuration()
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
            var result = await _sut.GetDurationBetweenTwoCitiesByFreeRoad(firstCity, secondCity);

            //Assert
            Assert.NotEqual(0, result);
        }

        [Fact]
        public async Task GetCostBetweenTwoCities_InvokeWithValidCities_ReturnsSomeCost()
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
            var result = await _sut.GetCostBetweenTwoCities(firstCity, secondCity);

            //Assert
            Assert.NotEqual(0, result);
        }


        [Fact]
        public async Task GetCityByName_InvokeWithRealName_ReturnsCityObject()
        {
            //Arrange
            List<string> cityNames = new List<string>();
            cityNames.Add("Wroclaw");
            List<City> cities = new List<City>();

            //Act
            cities = await _sut.GetCitiesFromGoogleApi(cityNames);

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

        [Fact]
        public void DownloadExternalData_ValidConditions_MatrixIsPopulated()
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

            List<City> cities = new List<City> {firstCity, secondCity};

            EvaluationMatrix matrix = new EvaluationMatrix(2);


            //Act
            _sut.DownloadExternalData(cities, matrix);


            //Assert
            Assert.Equal(4, matrix.Costs.Length);
            Assert.Equal(4, matrix.FreeDistances.Length);
            Assert.Equal(4, matrix.TollDistances.Length);

            //valid values
            Assert.Equal(double.MaxValue, matrix.FreeDistances[0]);
            Assert.Equal(double.MaxValue, matrix.FreeDistances[3]);
            Assert.NotEqual(double.MaxValue, matrix.FreeDistances[1]);
            Assert.NotEqual(double.MaxValue, matrix.FreeDistances[2]);
        }
    }
}
