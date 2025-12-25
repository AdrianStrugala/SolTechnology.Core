using DreamTravel.Domain.Cities;
using DreamTravel.Queries.CalculateBestPath;
using DreamTravel.Queries.CalculateBestPath.Chapters;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Queries.UnitTests.CalculateBestPath
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

            var calculateBestPathContext = new CalculateBestPathNarration
            {
                Cities = listOfCities,
                Costs = new double[noOfCities*noOfCities],
                Goals = new double[noOfCities*noOfCities],
                VinietaCosts = new double[noOfCities*noOfCities],
                OptimalCosts = new double[noOfCities*noOfCities],
                OptimalDistances = new double[noOfCities*noOfCities],
                FreeDistances = new double[noOfCities*noOfCities],
                TollDistances = new double[noOfCities*noOfCities],
                OrderOfCities = orderOfCities
            };

            //Act
            Result result = await _sut.Read(calculateBestPathContext);

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
