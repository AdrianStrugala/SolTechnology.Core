using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData.Query.DownloadRoadData.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DreamTravel.GeolocationDataTests.Query.DownloadRoadData.Clients
{
    public class DownloadCostBetweenTwoCitiesTests
    {
        private readonly DownloadCostBetweenTwoCities _sut = new DownloadCostBetweenTwoCities(NullLogger<DownloadCostBetweenTwoCities>.Instance);



        [Fact]
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
            var result = await _sut.Execute(firstCity, secondCity);

            //Assert
            Assert.NotEqual((0, 0), result);
        }


        [Fact]
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
            var result = await _sut.Execute(firstCity, secondCity);

            //Assert
            Assert.Equal((-1, -1), result);
        }
    }
}
