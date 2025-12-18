using DreamTravel.Domain.Cities;
using DreamTravel.Queries.CalculateBestPath;
using DreamTravel.Queries.CalculateBestPath.Executors;

namespace DreamTravel.Queries.UnitTests.CalculateBestPath
{
    public class FindProfitablePathTests
    {
        private readonly FindProfitablePath _sut = new();

        [Fact]
        public async Task EvaluateCost_ValidData_IntelligentResults()
        {
            //Arrange
            var cities = new List<City>
            {
                new City { Name = "CityA" },
                new City { Name = "CityB" },
                new City { Name = "CityC" }
            };

            int expectedLength = cities.Count * cities.Count; // 3 x 3 = 9
            var matrix = new CalculateBestPathContext
            {
                Cities = cities,
                NoOfCities = cities.Count,
                VinietaCosts = new double[expectedLength],
                OptimalDistances = new double[expectedLength],
                OptimalCosts = new double[expectedLength],
                Goals = new double[expectedLength],
                Costs =
                [
                    Double.MaxValue, 10, 19,
                    10, Double.MaxValue, 30,
                    19, 30,  Double.MaxValue
                ],
                FreeDistances =
                [
                    Double.MaxValue, 100, 200,
                    100, Double.MaxValue, 300,
                    200, 300,  Double.MaxValue
                ],
                TollDistances =
                [
                    Double.MaxValue, 90, 20,
                    90, Double.MaxValue, 350,
                    20, 350,  Double.MaxValue
                ]
            };


            //Act 
            await _sut.Execute(matrix);


            //Assert
            //1) takes profitable road
            Assert.Equal(20, matrix.OptimalDistances[2]);
            Assert.Equal(19, matrix.OptimalCosts[2]);
            Assert.Equal(20, matrix.OptimalDistances[6]);
            Assert.Equal(19, matrix.OptimalCosts[6]);

            //2) Rejects non-profitable road
            Assert.Equal(100, matrix.OptimalDistances[1]);
            Assert.Equal(0, matrix.OptimalCosts[1]);
            Assert.Equal(100, matrix.OptimalDistances[3]);
            Assert.Equal(0, matrix.OptimalCosts[3]);

            //3) Rejects toll road longer than free
            Assert.Equal(300, matrix.OptimalDistances[5]);
            Assert.Equal(0, matrix.OptimalCosts[5]);
            Assert.Equal(300, matrix.OptimalDistances[7]);
            Assert.Equal(0, matrix.OptimalCosts[7]);
        }
    }
}
