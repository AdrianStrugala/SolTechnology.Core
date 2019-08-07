namespace DreamTravel.WebUITests.BestPath.DataAccess
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging.Abstractions;
    using WebUI.BestPath.DataAccess;
    using WebUI.Contract;
    using Xunit;

    public class DownloadDurationMatrixByFreeRoadTests
    {
        readonly DownloadDurationMatrixByFreeRoad _sut = new DownloadDurationMatrixByFreeRoad(NullLogger<DownloadDurationMatrixByFreeRoad>.Instance);


        [Fact]
        public async Task DowloadDurationMatrixByFreeRoad_InvokeWithValidCities_ReturnsSomeDuration()
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
            var result = await _sut.Execute(list);

            //Assert
            Assert.NotEqual(0, result[1]);
            Assert.NotEqual(double.MaxValue, result[1]);
            Assert.Equal(double.MaxValue, result[0]);
        }

        [Fact]
        public async Task DowloadDurationMatrixByFreeRoad_InvalidCities_ExceptionIsThrown()
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
            var exception = await Record.ExceptionAsync( async () => await _sut.Execute(list));

            //Assert
            Assert.IsType<InvalidDataException>(exception);
        }
    }
}
