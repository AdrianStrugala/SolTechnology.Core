using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData.Matrices;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DreamTravel.GeolocationDataTests.Matrices
{
    public class MatrixRepositoryTests
    {
        readonly MatrixRepository _sut = new MatrixRepository(NullLogger<MatrixRepository>.Instance);

        [Fact(Skip = "Paid test :(")]
        public async Task DownloadDurationMatrixByTollRoad_InvokeWithValidCities_ReturnsSomeDuration()
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
            var result = await _sut.GetTollRoadDuration(list);

            //Assert
            Assert.NotEqual(0, result[1]);
            Assert.NotEqual(double.MaxValue, result[1]);
            Assert.Equal(double.MaxValue, result[0]);
        }

        [Fact(Skip = "Paid test :(")]
        public async Task DowloadDurationMatrixByTollRoad_InvalidCities_ExceptionIsThrown()
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
            var exception = await Record.ExceptionAsync(async () => await _sut.GetTollRoadDuration(list));

            //Assert
            Assert.IsType<InvalidDataException>(exception);
        }

        [Fact(Skip = "Paid test :(")]
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
            var result = await _sut.GetFreeRoadDuration(list);

            //Assert
            Assert.NotEqual(0, result[1]);
            Assert.NotEqual(double.MaxValue, result[1]);
            Assert.Equal(double.MaxValue, result[0]);
        }


        [Fact(Skip = "Paid test :(")]
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
            var exception = await Record.ExceptionAsync(async () => await _sut.GetFreeRoadDuration(list));

            //Assert
            Assert.IsType<InvalidDataException>(exception);
        }

        [Fact]
        public async Task GetCosts_ValidCities_ReturnsSomeCost()
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
            var result = await _sut.GetCosts(new List<City> { firstCity, secondCity });

            //Assert
            Assert.NotEqual((0), (result.Item1[1]));
            Assert.NotEqual((0), (result.Item1[2]));
            Assert.NotEqual((-1), (result.Item1[1]));
            Assert.NotEqual((-1), (result.Item1[2]));
        }


        [Fact]
        public async Task GetCosts_InvalidCities_MinusCostIsReturned()
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

            //Act
            var result = await _sut.GetCosts(new List<City> { firstCity, secondCity });

            //Assert
            Assert.Equal((-1, -1), (result.Item1[1], result.Item2[1]));
        }
    }
}
