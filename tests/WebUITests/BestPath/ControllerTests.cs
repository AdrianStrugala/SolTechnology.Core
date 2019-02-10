namespace WebUITests.BestPath
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using NSubstitute;
    using NSubstitute.ExceptionExtensions;
    using TestHelpers;
    using WebUI.BestPath;
    using WebUI.BestPath.Interfaces;
    using WebUI.SharedModels;
    using Xunit;

    public class ControllerTests : IClassFixture<Query>
    {
        private readonly TestServerSession _session;
        private readonly ICalculateBestPath _calculateBestPath;
        private readonly Fixture _fixture;

        public ControllerTests()
        {
            _calculateBestPath = Substitute.For<ICalculateBestPath>();

            void RegisterServices(IServiceCollection services)

            {
                services.RemoveAll<ICalculateBestPath>();
                services.AddSingleton(_calculateBestPath);
            }

            _session = new TestServerSession(RegisterServices);
            _fixture = new Fixture();
        }

        [Fact]
        public async Task CalculateBestPath_ValidQuery_StatusCodeIs200()
        {
            //Arrange
            Query query = _fixture.Create<Query>();

            _calculateBestPath.Execute(Arg.Any<List<City>>(), Arg.Any<bool>()).Returns(new Result());


            //Act
            var response = await _session.PostCalculateBestPath(query);


            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }


        [Fact]
        public async Task CalculateBestPath_ExceptionIsThrown_StatusCodeIs400()
        {
            //Arrange
            Query query = _fixture.Create<Query>();

            _calculateBestPath.Execute(Arg.Any<List<City>>(), Arg.Any<bool>()).Throws(new Exception());

            //Act
            var response = await _session.PostCalculateBestPath(query);


            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}