using System.IO;
using System.Threading.Tasks;
using DreamTravel.LocationOfCity.Interfaces;
using DreamTravel.SharedModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DreamTravelITests.LocationOfCity
{
    public class ControllerTests
    {
        private readonly DreamTravel.LocationOfCity.Controller _sut;

        private readonly IFindLocationOfCity _findLocationOfCity;

        public ControllerTests()
        {
            ILogger<DreamTravel.LocationOfCity.Controller> logger = Substitute.For<ILogger<DreamTravel.LocationOfCity.Controller>>();
            _findLocationOfCity = Substitute.For<IFindLocationOfCity>();

            _sut = new DreamTravel.LocationOfCity.Controller(_findLocationOfCity, logger);
        }

        [Fact]
        public async Task FindCity_SuccesfullCall_ReturnsData()
        {
            // Arrange
            City city = new City();
            city.Name = "SomeCity";
            city.Latitude = 21;
            city.Longitude = 37;

            _findLocationOfCity.Execute(Arg.Any<string>()).Returns(city);

            // Act
            var result = await _sut.FindLocationOfCity(city.Name, "2137");

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
            _findLocationOfCity.Execute(Arg.Any<string>()).ThrowsForAnyArgs(new InvalidDataException());

            // Act
            var result = await _sut.FindLocationOfCity("SomeCity", "2137");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);

            var badRequestResult = result as BadRequestObjectResult;
            Assert.NotNull(badRequestResult);
            Assert.NotNull(badRequestResult.Value);
        }

        //        [Fact]
        //        public async Task FindCityByLocation_SuccesfullCall_ReturnsData()
        //        {
        //            // Arrange
        //            City city = new City();
        //            city.Name = "someName";
        //            city.Latitude = 21;
        //            city.Longitude = 37;
        //
        //            _findNameOfCity.Execute(Arg.Any<City>()).Returns(city);
        //
        //            // Act
        //            var result = await _sut.FindCityByLocation(city.Latitude, city.Longitude, "someSessionId");
        //
        //            // Assert
        //            Assert.IsType<OkObjectResult>(result);
        //
        //            var okObjectResult = result as OkObjectResult;
        //            Assert.NotNull(okObjectResult);
        //
        //            var resultDeserialized = JsonConvert.DeserializeObject<City>(okObjectResult.Value.ToString());
        //            Assert.Equal(city.Name, resultDeserialized.Name);
        //            Assert.Equal(city.Latitude, resultDeserialized.Latitude);
        //            Assert.Equal(city.Longitude, resultDeserialized.Longitude);
        //        }
        //
        //        [Fact]
        //        public async Task FindCityByLocation_ExceptionHappend_ReturnsBadRequest()
        //        {
        //            // Arrange
        //            _findNameOfCity.Execute(Arg.Any<City>()).ThrowsForAnyArgs(new InvalidDataException());
        //
        //            // Act
        //            var result = await _sut.FindCityByLocation(21, 34, "someSessionId");
        //
        //            // Assert
        //            Assert.IsType<BadRequestObjectResult>(result);
        //
        //            var badRequestResult = result as BadRequestObjectResult;
        //            Assert.NotNull(badRequestResult);
        //            Assert.NotNull(badRequestResult.Value);
        //        }
    }
}
