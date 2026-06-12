using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationDataClients.GoogleApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace DreamTravel.GeolocationDataClients.IntegrationTests.GoogleApi
{
    [TestFixture]
    public class DownloadDurationMatrixByFreeRoadTests
    {
        readonly GoogleHTTPClient _sut = new(Options.Create(new GoogleHTTPOptions()), Substitute.For<HttpClient>(), NullLogger<GoogleHTTPClient>.Instance);


        [Test, Ignore("Paid test")]
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
            result[1].Should().NotBe(0);
            result[1].Should().NotBe(double.MaxValue);
            result[0].Should().Be(double.MaxValue);
        }

        [Test, Ignore("Paid test")]
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
            var act = async () => await _sut.GetDurationMatrixByFreeRoad(list);

            //Assert
            await act.Should().ThrowAsync<InvalidDataException>();
        }
    }
}
