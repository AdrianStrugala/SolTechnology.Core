using System.Collections.Generic;
using DreamTravel.CostLimit;
using DreamTravel.SharedModels;
using Xunit;

namespace DreamTravelITests.CostLimit
{
    public class CostLimitBreakerTests
    {
        private readonly BreakCostLimit _sut;
        public CostLimitBreakerTests()
        {
            _sut = new BreakCostLimit();
        }

        [Fact]
        void Handle_ValidInput_ListOfCitiesIsSortedByIndex()
        {
            //Arrange
            int costLimit = 40;
            List<Path> paths = new List<Path>();
            paths.Add(new Path()
            {
                Cost = 10,
                Index = 0,
                Goal = 3
            });

            paths.Add(new Path()
            {
                Cost = 15,
                Index = 1,
                Goal = 5
            });

            paths.Add(new Path()
            {
                Cost = 0,
                Index = 2,
                Goal = 7,
            });

            paths.Add(new Path()
            {
                Cost = 83,
                Index = 3,
                Goal = 1
            });


            //Act
            var result = _sut.Execute(costLimit, paths);

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
    }
}
