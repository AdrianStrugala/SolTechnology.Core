using DreamTravel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DreamTravelITests
{
    public class HomeControllerTests
    {
        private readonly HomeController _sut;

        public HomeControllerTests()
        {
            ILogger<HomeController> logger = Substitute.For<ILogger<HomeController>>();
            _sut = new HomeController(logger);
        }

        [Fact]
        public void Index_OnCall_ReturnsAViewResult()
        {
            // Arrange

            // Act
            var result = _sut.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }
    }
}
