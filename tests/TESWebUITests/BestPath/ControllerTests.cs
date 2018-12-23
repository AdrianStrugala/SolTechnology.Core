namespace DreamTravelITests.BestPath
{
    using AutoFixture;
    using DreamTravel.BestPath;
    using DreamTravel.BestPath.Interfaces;
    using DreamTravel.SharedModels;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using NSubstitute;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using TestHelpers;
    using Xunit;

    public class ControllerTests : IClassFixture<Query>
    {
        private readonly TestServerSession _session;
        private ICalculateBestPath _calculateBestPath;
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

            _calculateBestPath.Execute(Arg.Any<List<City>>(), Arg.Any<bool>()).Returns(new List<Path>());


            //Act
            var response = await _session.PostCalculateBestPath(query);


            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }


        [Fact(Skip = "Add validation mechanism")]
        public async Task CalculateBestPath_InvalidQuery_StatusCodeIs400()
        {
            //Arrange
            Query query = new Query
            {
                Cities = new List<City>
                {

                },
                OptimizePath = true,
                SessionId = "dupa"
            };


            //Act
            var response = await _session.PostCalculateBestPath(query);


            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        //TODO
        [Fact]
        public async Task CalculateBestPath_CalledMultipleTimes_DuplicatedCitiesAreReadFromCache()
        {
            //Arrange
            Query query = _fixture.Create<Query>();

            _calculateBestPath.Execute(Arg.Any<List<City>>(), Arg.Any<bool>()).Returns(new List<Path>());


            //Act
            await _session.PostCalculateBestPath(query);

            await _session.PostCalculateBestPath(query);

            //Assert
            //TODO Create here a command
            _calculateBestPath.Received();
        }
    }
}
