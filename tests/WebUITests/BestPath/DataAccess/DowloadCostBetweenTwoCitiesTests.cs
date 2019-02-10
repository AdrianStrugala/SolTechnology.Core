namespace WebUITests.BestPath.DataAccess
{
    using System.IO;
    using Microsoft.Extensions.Logging.Abstractions;
    using WebUI.BestPath.DataAccess;
    using WebUI.SharedModels;
    using Xunit;

    public class DowloadCostBetweenTwoCitiesTests
    {
        private readonly DownloadCostBetweenTwoCities _sut = new DownloadCostBetweenTwoCities(NullLogger<DownloadCostBetweenTwoCities>.Instance);



        [Fact]
        public void DowloadCostBetweenTwoCities_InvokeWithValidCities_ReturnsSomeCost()
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
            var result = _sut.Execute(firstCity, secondCity);

            //Assert
            Assert.NotEqual((0, 0), result);
        }


        [Fact]
        public void DowloadCostBetweenTwoCities_InvalidCities_ExceptionIsThrown()
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
            var exception = Record.Exception(() => _sut.Execute(firstCity, secondCity));

            //Assert
            Assert.IsType<InvalidDataException>(exception);
        }
    }
}
