using System;
using FluentAssertions;
using Xunit;

namespace SolTechnology.Core.Guards.Tests
{
    public class Int
    {
        [Fact]
        public void NotZero_Zero_Throws()
        {
            //Arrange
            int underTest = 0;


            //Act
            var exception = Record.Exception(() => Guards.Int(underTest, nameof(underTest))
                .NotZero());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        public void NotNegative_Negative_Throws(int underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Int(underTest, nameof(underTest))
                .NotNegative());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }


        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(1)]
        public void NotPositive_Positive_Throws(int underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Int(underTest, nameof(underTest))
                .NotPositive());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(-6)]
        [InlineData(11)]
        public void InRange_NotInRange_Throws(int underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Int(underTest, nameof(underTest))
                .InRange(-5, 10));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void InRange_InRange_NoException(int underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Int(underTest, nameof(underTest))
                .InRange(-5, 10));


            //Assert
            exception.Should().BeNull();
        }


        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(-6)]
        [InlineData(11)]
        public void NotInRange_NotInRange_NoException(int underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Int(underTest, nameof(underTest))
                .NotInRange(-5, 10));


            //Assert
            exception.Should().BeNull();
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void NotInRange_InRange_Throws(int underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Int(underTest, nameof(underTest))
                .NotInRange(-5, 10));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }


        [Fact]
        public void Equal_NotEqual_Throws()
        {
            //Arrange
            int underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Int(underTest, nameof(underTest))
                .Equal(50));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void NotEqual_Equal_Throws()
        {
            //Arrange
            int underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Int(underTest, nameof(underTest))
                .NotEqual(underTest));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }
    }
}