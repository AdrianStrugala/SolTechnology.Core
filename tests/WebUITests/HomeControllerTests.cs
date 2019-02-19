namespace DreamTravel.WebUITests
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using WebUI;
    using Xunit;

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
