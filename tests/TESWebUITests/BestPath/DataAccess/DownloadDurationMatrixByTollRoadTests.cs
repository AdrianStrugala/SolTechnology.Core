using System.Collections.Generic;
using System.IO;
using DreamTravel.BestPath.DataAccess;
using DreamTravel.SharedModels;
using Xunit;

namespace DreamTravelITests.BestPath.DataAccess
{
    public class DownloadDurationMatrixByTollRoadTests
    {
        readonly DownloadDurationMatrixByTollRoad _sut = new DownloadDurationMatrixByTollRoad();

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

            var list = new List<City> { firstCity, secondCity };

            //Act
            var result = _sut.Execute(list);

            //Assert
            Assert.NotEqual(0, result[1]);
            Assert.Equal(0, result[0]);
        }

        [Fact]
        public void DowloadDurationMatrixByTollRoad_InvalidCities_ExceptionIsThrown()
        {
            //Arrange
            City firstCity = new City
            {
                Name = "first",
                Latitude = 0,
                Longitude = 0
            };

            City secondCity = new City
            {
                Name = "second",
                Latitude = -50,
                Longitude = 19
            };

            var list = new List<City> { firstCity, secondCity };

            //Act
            var exception = Record.Exception(() => _sut.Execute(list));

            //Assert
            Assert.IsType<InvalidDataException>(exception);
        }
    }
}
