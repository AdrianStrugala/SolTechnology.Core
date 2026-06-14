using DreamTravel.Queries.LimitCostOfPaths;
using FluentAssertions;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.Queries.UnitTests.LimitCostOfPaths
{
    [TestFixture]
    public class LimitCostOfPathsTests
    {
        private readonly LimitCostOfPathsService _sut = new();

        [Test]
        public async Task Handle_ValidInput_ListOfCitiesIsSortedByIndex()
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
            var result = (await _sut.Handle(new LimitCostOfPathsQuery
            {
                CostLimit = costLimit,
                Paths = paths
            }, CancellationToken.None)).Data;

            //Assert
            result[0].Index.Should().Be(0);
            result[1].Index.Should().Be(1);
            result[2].Index.Should().Be(2);
            result[3].Index.Should().Be(3);

            double totalCost = result.Sum(p => p.OptimalCost);
            totalCost.Should().BeLessThan(costLimit);
        }

        [Test]
        public async Task Handle_PathWithVinietaCostAndLimitDecreases_FreePathIsReturned()
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
            var result = (await _sut.Handle(new LimitCostOfPathsQuery
            {
                CostLimit = costLimit,
                Paths = paths
            }, CancellationToken.None)).Data;


            //Assert
            result[0].OptimalCost.Should().Be(0);
        }


        [Test]
        public async Task Handle_2PathsUsingTheSameVinieta_ResultContainsOptimalCostForThem()
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
            var result = (await _sut.Handle(new LimitCostOfPathsQuery
            {
                CostLimit = costLimit,
                Paths = paths
            }, CancellationToken.None)).Data;


            //Assert
            result[0].OptimalCost.Should().Be(5);
            result[1].OptimalCost.Should().Be(5);
        }
    }
}
