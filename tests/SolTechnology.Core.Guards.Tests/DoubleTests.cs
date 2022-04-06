using System;
using FluentAssertions;
using Xunit;

namespace SolTechnology.Core.Guards.Tests
{
    public class DoubleTests
    {
        [Fact]
        public void NotZero_Zero_Throws()
        {
            //Arrange
            double underTest = 0;


            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .NotZero());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(double.MinValue)]
        [InlineData(-1)]
        public void NotNegative_Negative_Throws(double underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .NotNegative());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }


        [Theory]
        [InlineData(double.MaxValue)]
        [InlineData(1)]
        public void NotPositive_Positive_Throws(double underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .NotPositive());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        [InlineData(-6)]
        [InlineData(11)]
        public void InRange_NotInRange_Throws(double underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .InRange(-5, 10));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void InRange_InRange_NoException(double underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .InRange(-5, 10));


            //Assert
            exception.Should().BeNull();
        }


        [Theory]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        [InlineData(-6)]
        [InlineData(11)]
        public void NotInRange_NotInRange_NoException(double underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .NotInRange(-5, 10));


            //Assert
            exception.Should().BeNull();
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void NotInRange_InRange_Throws(double underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .NotInRange(-5, 10));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }


        [Fact]
        public void Equal_NotEqual_Throws()
        {
            //Arrange
            double underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .Equal(50));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void NotEqual_Equal_Throws()
        {
            //Arrange
            double underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .NotEqual(underTest));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        public void GreaterThan_NotGreater_Throws(double toCompare)
        {
            //Arrange
            double underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .GreaterThan(toCompare));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(double.MinValue)]
        public void GreaterEqual_GreaterOrEqual_Success(double toCompare)
        {
            //Arrange
            double underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .GreaterEqual(toCompare));


            //Assert
            exception.Should().BeNull();
        }


        [Theory]
        [InlineData(5)]
        [InlineData(-10)]
        public void LessThan_NotLess_Throws(double toCompare)
        {
            //Arrange
            double underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .LessThan(toCompare));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(double.MaxValue)]
        public void LessEqual_LessOrEqual_Success(double toCompare)
        {
            //Arrange
            double underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Double(underTest, nameof(underTest))
                .LessEqual(toCompare));


            //Assert
            exception.Should().BeNull();
        }
    }
}