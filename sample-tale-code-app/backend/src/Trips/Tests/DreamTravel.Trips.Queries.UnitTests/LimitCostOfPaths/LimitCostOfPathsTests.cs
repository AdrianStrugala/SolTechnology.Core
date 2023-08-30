using DreamTravel.Trips.Queries.LimitCostOfPaths;
using Path = DreamTravel.Trips.Domain.Paths.Path;

namespace DreamTravel.Trips.Queries.UnitTests.LimitCostOfPaths
{
    public class LimitCostOfPathsTests
    {
        private readonly LimitCostOfPathsService _sut;
        public LimitCostOfPathsTests()
        {
            _sut = new LimitCostOfPathsService();
        }

        [Fact]
        async Task Handle_ValidInput_ListOfCitiesIsSortedByIndex()
        {
            //Arrange
            int costLimit = 40;
            List<Path> paths = new List<Path>();
            paths.Add(new Path
            {
                Cost = 10,
                Index = 0,
                Goal = 3
            });

            paths.Add(new Path
            {
                Cost = 15,
                Index = 1,
                Goal = 5
            });

            paths.Add(new Path
            {
                Cost = 0,
                Index = 2,
                Goal = 7,
            });

            paths.Add(new Path
            {
                Cost = 83,
                Index = 3,
                Goal = 1
            });


            //Act
            var result = await _sut.Execute(new LimitCostOfPathsInput
            {
                CostLimit = costLimit,
                Paths = paths
            });

            //Assert
            Assert.Equal(0, result[0].Index);
            Assert.Equal(1, result[1].Index);
            Assert.Equal(2, result[2].Index);
            Assert.Equal(3, result[3].Index);

            double totalCost = 0;
            foreach (var path in result)
            {
                totalCost += path.OptimalCost;
            }

            Assert.True(totalCost < costLimit);
        }

        [Fact]
        async Task Handle_PathWithVinietaCostAndLimitDecreases_FreePathIsReturned()
        {
            //Arrange
            int costLimit = 0;
            List<Path> paths = new List<Path>();
            paths.Add(new Path
            {
                Cost = 10,
                VinietaCost = 10,
                OptimalCost = 10,
                Index = 0,
                Goal = 3
            });


            //Act
            var result = await _sut.Execute(new LimitCostOfPathsInput
            {
                CostLimit = costLimit,
                Paths = paths
            });


            //Assert
            Assert.Equal(0, result[0].OptimalCost);
        }


        [Fact]
        async Task Handle_2PathsUsingTheSameVinieta_ResultContainsOptimalCostForThem()
        {
            //Arrange
            int costLimit = 11;
            List<Path> paths = new List<Path>();
            paths.Add(new Path
            {
                Cost = 5,
                VinietaCost = 10,
                OptimalCost = 0,
                Index = 0,
                Goal = 3
            });

            paths.Add(new Path
            {
                Cost = 5,
                VinietaCost = 10,
                OptimalCost = 0,
                Index = 1,
                Goal = 3
            });


            //Act
            var result = await _sut.Execute(new LimitCostOfPathsInput
            {
                CostLimit = costLimit,
                Paths = paths
            });


            //Assert
            Assert.Equal(5, result[0].OptimalCost);
            Assert.Equal(5, result[1].OptimalCost);
        }
    }
}
