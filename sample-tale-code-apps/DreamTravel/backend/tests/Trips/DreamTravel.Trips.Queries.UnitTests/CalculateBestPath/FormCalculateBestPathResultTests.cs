using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Queries.CalculateBestPath;
using DreamTravel.Trips.Queries.CalculateBestPath.Executors;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.UnitTests.CalculateBestPath
{
    public class FormCalculateBestPathResultTests
    {
        private readonly FormCalculateBestPathResult _sut = new FormCalculateBestPathResult();

        [Fact]
        public async Task GetDurationBetweenTwoCitiesByTollRoad_InvokeWithValidCities_ReturnsSomeDuration()
        {
            //Arrange
            int noOfCities = 3;

            City firstCity = new City
            {
                Name = "first",
                Latitude = 51,
                Longitude = 17
            };

            City secondCity = new City
            {
                Name = "second",
                Latitude = 53,
                Longitude = 19
            };

            City thirdCity = new City
            {
                Name = "third",
                Latitude = 55,
                Longitude = 21
            };
            List<City> listOfCities = new List<City> { firstCity, secondCity, thirdCity };

            List<int> orderOfCities = new List<int>(noOfCities) { 0, 2, 1 };

            var calculateBestPathContext = new CalculateBestPathContext
            {
                Cities = listOfCities,
                Costs = new double[noOfCities*noOfCities],
                VinietaCosts = new double[noOfCities*noOfCities],
                OrderOfCities = orderOfCities
            };

            //Act
            Result result = await _sut.Execute(calculateBestPathContext);

            //Assert
            Assert.True(result.IsSuccess);
            var bestPaths = calculateBestPathContext.Output.BestPaths;
            Assert.Equal(noOfCities - 1, bestPaths.Count);
            Assert.Equal("first", bestPaths[0].StartingCity.Name);
            Assert.Equal("third", bestPaths[1].StartingCity.Name);
            Assert.Equal("second", bestPaths[1].EndingCity.Name);
        }
    }
}
