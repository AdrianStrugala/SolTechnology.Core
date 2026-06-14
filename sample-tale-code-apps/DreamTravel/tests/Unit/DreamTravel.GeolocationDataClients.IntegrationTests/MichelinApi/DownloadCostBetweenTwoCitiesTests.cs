using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationDataClients.MichelinApi;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace DreamTravel.GeolocationDataClients.IntegrationTests.MichelinApi
{
    [TestFixture]
    public class DownloadCostBetweenTwoCitiesTests
    {
        private readonly MichelinHTTPClient _sut = new MichelinHTTPClient(Options.Create(new MichelinHTTPOptions()), Substitute.For<HttpClient>(), NullLogger<MichelinHTTPClient>.Instance);


        [Test, Ignore("Paid test")]
        public async Task DownloadCostBetweenTwoCities_InvokeWithValidCities_ReturnsSomeCost()
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
            var result = await _sut.DownloadCostBetweenTwoCities(firstCity, secondCity);

            //Assert
            result.Should().NotBe((0, 0));
        }


        [Test, Ignore("Paid test")]
        public async Task DownloadCostBetweenTwoCities_InvalidCities_MinusCostIsReturned()
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
            var result = await _sut.DownloadCostBetweenTwoCities(firstCity, secondCity);

            //Assert
            result.Should().Be((-1, -1));
        }
    }
}
