using DreamTravel.Queries.LimitCostOfPaths;
using FluentAssertions;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.Queries.UnitTests.LimitCostOfPaths
{
    [TestFixture]
    public class LimitCostOfPathsHandlerTests
    {
        private readonly LimitCostOfPathsHandler _sut = new();

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

        [Test]
        public async Task Handle_PathsOverTheLimit_DoesNotMutateTheInputListOrItsElements()
        {
            // Arrange
            var paths = new List<Path>
            {
                new() { Index = 1, Goal = 2, Cost = 50, TollDistance = 200, FreeDistance = 260, OptimalCost = 7, OptimalDistance = 8 },
                new() { Index = 0, Goal = 1, Cost = 10, TollDistance = 100, FreeDistance = 150, OptimalCost = 7, OptimalDistance = 8 }
            };
            var input = paths.ToList();

            // Act
            var result = (await _sut.Handle(new LimitCostOfPathsQuery { CostLimit = 20, Paths = paths }, CancellationToken.None)).Data!;

            // Assert
            result.Select(p => (p.Index, p.OptimalCost, p.OptimalDistance)).Should().Equal((0, 10d, 100d), (1, 0d, 260d));
            paths.Should().Equal(input);
            paths.Select(p => (p.OptimalCost, p.OptimalDistance)).Should().AllBeEquivalentTo((7d, 8d));
            result.Should().NotContain(r => paths.Contains(r));
        }
    }
}
