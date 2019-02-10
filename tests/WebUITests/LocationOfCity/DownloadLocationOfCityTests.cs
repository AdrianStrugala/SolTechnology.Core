namespace WebUITests.LocationOfCity
{
    using System.IO;
    using System.Threading.Tasks;
    using WebUI.LocationOfCity;
    using Xunit;

    public class DownloadLocationOfCityTests
    {
        private readonly FindLocationOfCity _sut;

        public DownloadLocationOfCityTests()
        {
            _sut = new FindLocationOfCity();
        }

        [Fact]
        public async Task GetCityByName_InvokeWithRealName_ReturnsCityObject()
        {
            //Arrange
            string cityName = "Wroclaw";

            //Act
            var result = await _sut.Execute(cityName);

            //Assert
            Assert.Equal("Wroclaw", result.Name);
            Assert.NotEqual(0, result.Latitude);
            Assert.NotEqual(0, result.Longitude);
        }

        [Fact]
        public async Task GetCityByName_NonExistingCity_ExceptionIsThrown()
        {
            //Arrange
            string cityName = "DUPA";

            //Act
            // ReSharper disable once PossibleNullReferenceException
            var exception = await Record.ExceptionAsync(async () => await _sut.Execute(cityName));

            //Assert
            Assert.IsType<InvalidDataException>(exception);
        }

    }
}
