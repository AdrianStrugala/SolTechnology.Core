using System;
using FluentAssertions;
using Xunit;

namespace SolTechnology.Core.Guards.Tests
{
    public class ComponentTests
    {
        private readonly Guards _guards;

        public ComponentTests()
        {
            _guards = new Guards();
        }

        [Fact]
        public void Three_Invalid_Properties_Produces_Three_Errors()
        {
            //Arrange
            string winner = null;
            int apiId = 0;
            int playerApiId = 0;


            //Act
            var result =
                _guards.String(winner, nameof(winner), x => x.NotNull().NotEmpty())
                    .Int(apiId, nameof(apiId), x => x.NotZero())
                    .Int(playerApiId, nameof(playerApiId), x => x.NotZero());


            //Assert
            result.Errors.Should().HaveCount(3);
        }
    }
}