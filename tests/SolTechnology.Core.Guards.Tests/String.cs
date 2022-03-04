using System;
using FluentAssertions;
using Xunit;

namespace SolTechnology.Core.Guards.Tests
{
    public class String
    {
        [Fact]
        public void NotNull_Null_Throws()
        {
            //Arrange
            string underTest = null;


            //Act
            var exception = Record.Exception(() => Guards.String(underTest, nameof(underTest))
                .NotNull());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void NotEmpty_Empty_Throws()
        {
            //Arrange
            string underTest = "";


            //Act
            var exception = Record.Exception(() => Guards.String(underTest, nameof(underTest))
                .NotEmpty());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void NotEquals_Equals_Throws()
        {
            //Arrange
            string underTest = "EqualString";


            //Act
            var exception = Record.Exception(() => Guards.String(underTest, nameof(underTest))
                .NotEqual(underTest));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void Equals_NotEquals_Throws()
        {
            //Arrange
            string underTest = "NotEqualString";


            //Act
            var exception = Record.Exception(() => Guards.String(underTest, nameof(underTest))
                .Equal("AnotherString"));


            //Assert

            exception.Should().BeOfType<ArgumentException>();
        }
    }
}