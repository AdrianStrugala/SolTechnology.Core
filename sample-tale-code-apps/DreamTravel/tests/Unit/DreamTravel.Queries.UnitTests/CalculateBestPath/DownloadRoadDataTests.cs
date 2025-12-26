using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationDataClients.GoogleApi;
using DreamTravel.GeolocationDataClients.MichelinApi;
using DreamTravel.Queries.CalculateBestPath;
using DreamTravel.Queries.CalculateBestPath.Chapters;
using FluentAssertions;
using NSubstitute;

namespace DreamTravel.Queries.UnitTests.CalculateBestPath
{
    public class DownloadRoadDataTests
    {
        private readonly DownloadRoadData _sut;
        private readonly IGoogleApiClient _googleApiClient;
        private readonly IMichelinApiClient _michelinApiClient;
        
        public DownloadRoadDataTests()
        {
            var fixture = new Fixture().Customize(
                new AutoNSubstituteCustomization { ConfigureMembers = true });

            _googleApiClient = fixture.Freeze<IGoogleApiClient>();
            _michelinApiClient = fixture.Freeze<IMichelinApiClient>();
            
            _sut = fixture.Create<DownloadRoadData>();
        } 
        [Fact]
        public async Task Execute_ShouldPopulateContextWithRoadData()
        {
            // Arrange: Create a list of 3 cities.
            var cities = new List<City>
            {
                new City { Name = "CityA" },
                new City { Name = "CityB" },
                new City { Name = "CityC" }
            };

            int expectedLength = cities.Count * cities.Count; // 3 x 3 = 9
            var context = new CalculateBestPathContext
            {
                Cities = cities,
                Costs = new double[expectedLength],
                VinietaCosts = new double[expectedLength]
            };

            // Setup Google API substitutes to return dummy double arrays.
            var tollMatrix = new double[] { 1.1, 2.2, 3.3 };
            var freeMatrix = new double[] { 4.4, 5.5, 6.6 };
            _googleApiClient.GetDurationMatrixByTollRoad(cities).Returns(Task.FromResult(tollMatrix));
            _googleApiClient.GetDurationMatrixByFreeRoad(cities).Returns(Task.FromResult(freeMatrix));

            // Setup Michelin API substitute: for any city pair, return (10.0, 5.0).
            _michelinApiClient.DownloadCostBetweenTwoCities(Arg.Any<City>(), Arg.Any<City>())
                .Returns(Task.FromResult((10.0, 5.0)));

            // Act
            var result = await _sut.Read(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            context.TollDistances.Should().Equal(tollMatrix);
            context.FreeDistances.Should().Equal(freeMatrix);
            
            context.Costs.Length.Should().Be(expectedLength);
            context.VinietaCosts.Length.Should().Be(expectedLength);
            for (int i = 0; i < expectedLength; i++)
            {
                context.Costs[i].Should().Be(10.0);
                context.VinietaCosts[i].Should().Be(5.0);
            }

            // Verify that each Google API method was called exactly once.
            await _googleApiClient.Received(1).GetDurationMatrixByTollRoad(cities);
            await _googleApiClient.Received(1).GetDurationMatrixByFreeRoad(cities);
            // Verify that the Michelin API was called for every city pair (9 calls).
            await _michelinApiClient.Received(expectedLength).DownloadCostBetweenTwoCities(Arg.Any<City>(), Arg.Any<City>());}
    }
}
