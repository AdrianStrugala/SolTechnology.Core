using System.IO;
using System.Threading.Tasks;
using DreamTravel.LocationOfCity.Interfaces;
using DreamTravel.NameOfCity.Interfaces;
using DreamTravel.SharedModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DreamTravelITests.NameOfCity
{
    public class ControllerTests
    {
        private readonly DreamTravel.NameOfCity.Controller _sut;

        private readonly IFindNameOfCity _findNameOfCity;

        public ControllerTests()
        {
            ILogger<DreamTravel.NameOfCity.Controller> logger = Substitute.For<ILogger<DreamTravel.NameOfCity.Controller>>();
            _findNameOfCity = Substitute.For<IFindNameOfCity>();

            _sut = new DreamTravel.NameOfCity.Controller(_findNameOfCity, logger);
        }


        [Fact]
        public async Task FindCityByLocation_SuccesfullCall_ReturnsData()
        {
            // Arrange
            City city = new City();
            city.Name = "someName";
            city.Latitude = 21;
            city.Longitude = 37;

            _findNameOfCity.Execute(Arg.Any<City>()).Returns(city);

            // Act
            var result = await _sut.FindNameOfCity(city.Latitude, city.Longitude, "someSessionId");

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
            _findNameOfCity.Execute(Arg.Any<City>()).ThrowsForAnyArgs(new InvalidDataException());

            // Act
            var result = await _sut.FindNameOfCity(21, 34, "someSessionId");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);

            var badRequestResult = result as BadRequestObjectResult;
            Assert.NotNull(badRequestResult);
            Assert.NotNull(badRequestResult.Value);
        }
    }
}
