using System.Net.Http;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationDataClients.MichelinApi;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace DreamTravel.GeolocationDataClients.IntegrationTests.MichelinApi
{
    public class DownloadCostBetweenTwoCitiesTests
    {
        private readonly MichelinApiClient _sut = new MichelinApiClient(Options.Create(new MichelinApiOptions()), Substitute.For<HttpClient>(), NullLogger<MichelinApiClient>.Instance);


        [Fact(Skip = "Paid test")]
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
            Assert.NotEqual((0, 0), result);
        }


        [Fact(Skip = "Paid test")]
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
            Assert.Equal((-1, -1), result);
        }
    }
}
