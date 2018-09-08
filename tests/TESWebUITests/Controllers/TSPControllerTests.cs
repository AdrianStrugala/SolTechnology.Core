using System.IO;
using System.Threading.Tasks;
using DreamTravel.Controllers;
using DreamTravel.ExternalConnection.Interfaces;
using DreamTravel.Models;
using DreamTravel.TSPControllerHandlers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DreamTravelITests.Controllers
{
    public class TSPControllerTests
    {
        private readonly TSPController _sut;

        private readonly IDownloadLocationOfCity _downloadLocationOfCity;
        private readonly IDownloadCityNameByLocation _downloadCityNameByLocation;
        private readonly ICalculateBestPath _calcuateBestPath;

        public TSPControllerTests()
        {
            _calcuateBestPath = Substitute.For<ICalculateBestPath>();
            _downloadLocationOfCity = Substitute.For<IDownloadLocationOfCity>();
            _downloadCityNameByLocation = Substitute.For<IDownloadCityNameByLocation>();
            IBreakCostLimit breakCostLimit = Substitute.For<IBreakCostLimit>();

            _sut = new TSPController(_calcuateBestPath, _downloadLocationOfCity, _downloadCityNameByLocation,
                breakCostLimit);
        }

        [Fact]
        public async Task FindCity_SuccesfullCall_ReturnsData()
        {
            // Arrange
            City city = new City();
            city.Name = "SomeCity";
            city.Latitude = 21;
            city.Longitude = 37;

            _downloadLocationOfCity.Execute(Arg.Any<string>()).Returns(city);

            // Act
            var result = await _sut.FindCity(city.Name, "2137");

            // Assert
            Assert.IsType<OkObjectResult>(result);

            var okObjectResult = result as OkObjectResult;
            Assert.NotNull(okObjectResult);

            var resultDeserialized = JsonConvert.DeserializeObject<City>(okObjectResult.Value.ToString());
            Assert.Equal(city.Name, resultDeserialized.Name);
            Assert.Equal(city.Latitude, resultDeserialized.Latitude);
            Assert.Equal(city.Longitude, resultDeserialized.Longitude);
        }

        [Fact]
        public async Task FindCity_ExceptionHappend_ReturnsBadRequest()
        {
            // Arrange
            _downloadLocationOfCity.Execute(Arg.Any<string>()).ThrowsForAnyArgs(new InvalidDataException());

            // Act
            var result = await _sut.FindCity("SomeCity", "2137");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);

            var badRequestResult = result as BadRequestObjectResult;
            Assert.NotNull(badRequestResult);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task FindCityByLocation_SuccesfullCall_ReturnsData()
        {
            // Arrange
            City city = new City();
            city.Name = "someName";
            city.Latitude = 21;
            city.Longitude = 37;

            _downloadCityNameByLocation.Execute(Arg.Any<City>()).Returns(city);

            // Act
            var result = await _sut.FindCityByLocation(city.Latitude, city.Longitude, "someSessionId");

            // Assert
            Assert.IsType<OkObjectResult>(result);

            var okObjectResult = result as OkObjectResult;
            Assert.NotNull(okObjectResult);

            var resultDeserialized = JsonConvert.DeserializeObject<City>(okObjectResult.Value.ToString());
            Assert.Equal(city.Name, resultDeserialized.Name);
            Assert.Equal(city.Latitude, resultDeserialized.Latitude);
            Assert.Equal(city.Longitude, resultDeserialized.Longitude);
        }

        [Fact]
        public async Task FindCityByLocation_ExceptionHappend_ReturnsBadRequest()
        {
            // Arrange
            _downloadCityNameByLocation.Execute(Arg.Any<City>()).ThrowsForAnyArgs(new InvalidDataException());

            // Act
            var result = await _sut.FindCityByLocation(21, 34, "someSessionId");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);

            var badRequestResult = result as BadRequestObjectResult;
            Assert.NotNull(badRequestResult);
            Assert.NotNull(badRequestResult.Value);
        }
    }
}
