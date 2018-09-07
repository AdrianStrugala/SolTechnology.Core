using System;
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

        public TSPControllerTests()
        {
            ICalculateBestPath calcuateBestPath = Substitute.For<ICalculateBestPath>();
            _downloadLocationOfCity = Substitute.For<IDownloadLocationOfCity>();
            IDownloadCityNameByLocation downloadCityNameByLocation = Substitute.For<IDownloadCityNameByLocation>();
            IBreakCostLimit breakCostLimit = Substitute.For<IBreakCostLimit>();

            _sut = new TSPController(calcuateBestPath, _downloadLocationOfCity, downloadCityNameByLocation, breakCostLimit);
        }

        [Fact]
        public async Task Index_SuccesfullCall_ReturnsOK()
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
        public async Task Index_ExceptionHappend_ReturnsBadRequest()
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
    }
}
