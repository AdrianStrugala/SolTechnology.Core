using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.Trips.Domain.Cities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace DreamTravel.Trips.GeolocationDataClients.IntegrationTests.GoogleApi
{
    public class DownloadDurationMatrixByFreeRoadTests
    {
        readonly GoogleApiClient _sut = new(Options.Create(new GoogleApiOptions()), Substitute.For<HttpClient>(), NullLogger<GoogleApiClient>.Instance);


        [Fact(Skip = "Paid test")]
        public async Task DownloadDurationMatrixByFreeRoad_InvokeWithValidCities_ReturnsSomeDuration()
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
            var result = await _sut.GetDurationMatrixByFreeRoad(list);

            //Assert
            Assert.NotEqual(0, result[1]);
            Assert.NotEqual(double.MaxValue, result[1]);
            Assert.Equal(double.MaxValue, result[0]);
        }

        [Fact(Skip = "Paid test")]
        public async Task DownloadDurationMatrixByFreeRoad_InvalidCities_ExceptionIsThrown()
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
            var exception = await Record.ExceptionAsync(async () => await _sut.GetDurationMatrixByFreeRoad(list));

            //Assert
            Assert.IsType<InvalidDataException>(exception);
        }
    }
}