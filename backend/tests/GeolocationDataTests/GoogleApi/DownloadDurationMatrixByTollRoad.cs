using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData.GoogleApi;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DreamTravel.GeolocationDataTests.GoogleApi
{
    public class DownloadDurationMatrixByTollRoadTests
    {
        readonly GoogleApiClient _sut = new GoogleApiClient(NullLogger<GoogleApiClient>.Instance);

        [Fact(Skip = "Paid test")]
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
            var result = await _sut.GetDurationMatrixByTollRoad(list);

            //Assert
            Assert.NotEqual(0, result[1]);
            Assert.NotEqual(double.MaxValue, result[1]);
            Assert.Equal(double.MaxValue, result[0]);
        }

        [Fact(Skip = "Paid test")]
        public async Task DownloadDurationMatrixByTollRoad_InvalidCities_ExceptionIsThrown()
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
            var exception = await Record.ExceptionAsync(async () => await _sut.GetDurationMatrixByTollRoad(list));

            //Assert
            Assert.IsType<InvalidDataException>(exception);
        }
    }
}