using System;
using FluentAssertions;
using Xunit;

namespace SolTechnology.Core.Guards.Tests
{
    public class LongTests
    {
        [Fact]
        public void NotZero_Zero_Throws()
        {
            //Arrange
            long underTest = 0;


            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .NotZero());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(-1)]
        public void NotNegative_Negative_Throws(long underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .NotNegative());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }


        [Theory]
        [InlineData(long.MaxValue)]
        [InlineData(1)]
        public void NotPositive_Positive_Throws(long underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .NotPositive());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        [InlineData(-6)]
        [InlineData(11)]
        public void InRange_NotInRange_Throws(long underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .InRange(-5, 10));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void InRange_InRange_NoException(long underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .InRange(-5, 10));


            //Assert
            exception.Should().BeNull();
        }


        [Theory]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        [InlineData(-6)]
        [InlineData(11)]
        public void NotInRange_NotInRange_NoException(long underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .NotInRange(-5, 10));


            //Assert
            exception.Should().BeNull();
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void NotInRange_InRange_Throws(long underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .NotInRange(-5, 10));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }


        [Fact]
        public void Equal_NotEqual_Throws()
        {
            //Arrange
            long underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .Equal(50));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void NotEqual_Equal_Throws()
        {
            //Arrange
            long underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .NotEqual(underTest));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        public void GreaterThan_NotGreater_Throws(long toCompare)
        {
            //Arrange
            long underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .GreaterThan(toCompare));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(long.MinValue)]
        public void GreaterEqual_GreaterOrEqual_Success(long toCompare)
        {
            //Arrange
            long underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .GreaterEqual(toCompare));


            //Assert
            exception.Should().BeNull();
        }


        [Theory]
        [InlineData(5)]
        [InlineData(-10)]
        public void LessThan_NotLess_Throws(long toCompare)
        {
            //Arrange
            long underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .LessThan(toCompare));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(long.MaxValue)]
        public void LessEqual_LessOrEqual_Success(long toCompare)
        {
            //Arrange
            long underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Long(underTest, nameof(underTest))
                .LessEqual(toCompare));


            //Assert
            exception.Should().BeNull();
        }
    }
}