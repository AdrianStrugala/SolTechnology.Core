using System.IO;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.Features.FindLocationOfCity;
using DreamTravel.WebUI;
using DreamTravel.WebUI.Routes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DreamTravel.WebUITests
{
    public class FindLocationOfCityControllerTests
    {
        private readonly FindLocationOfCityController _sut;

        private readonly IFindLocationOfCity _findLocationOfCity;

        public FindLocationOfCityControllerTests()
        {
            ILogger<FindLocationOfCityController> logger = Substitute.For<ILogger<FindLocationOfCityController>>();
            _findLocationOfCity = Substitute.For<IFindLocationOfCity>();

            _sut = new FindLocationOfCityController(_findLocationOfCity, logger);
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
        public async Task FindCity_ExceptionHappened_ReturnsBadRequest()
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
    }
}
