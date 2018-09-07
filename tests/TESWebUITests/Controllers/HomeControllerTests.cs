using DreamTravel.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DreamTravelITests.Controllers
{
    public class HomeControllerTests
    {
        private readonly HomeController _sut;

        public HomeControllerTests()
        {
            _sut = new HomeController();
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
