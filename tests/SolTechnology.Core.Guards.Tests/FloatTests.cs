using System;
using FluentAssertions;
using Xunit;

namespace SolTechnology.Core.Guards.Tests
{
    public class FloatTests
    {
        [Fact]
        public void NotZero_Zero_Throws()
        {
            //Arrange
            float underTest = 0;


            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .NotZero());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(float.MinValue)]
        [InlineData(-1)]
        public void NotNegative_Negative_Throws(float underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .NotNegative());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }


        [Theory]
        [InlineData(float.MaxValue)]
        [InlineData(1)]
        public void NotPositive_Positive_Throws(float underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .NotPositive());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(float.MaxValue)]
        [InlineData(float.MinValue)]
        [InlineData(-6)]
        [InlineData(11)]
        public void InRange_NotInRange_Throws(float underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .InRange(-5, 10));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void InRange_InRange_NoException(float underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .InRange(-5, 10));


            //Assert
            exception.Should().BeNull();
        }


        [Theory]
        [InlineData(float.MaxValue)]
        [InlineData(float.MinValue)]
        [InlineData(-6)]
        [InlineData(11)]
        public void NotInRange_NotInRange_NoException(float underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .NotInRange(-5, 10));


            //Assert
            exception.Should().BeNull();
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void NotInRange_InRange_Throws(float underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .NotInRange(-5, 10));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }


        [Fact]
        public void Equal_NotEqual_Throws()
        {
            //Arrange
            float underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .Equal(50));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void NotEqual_Equal_Throws()
        {
            //Arrange
            float underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .NotEqual(underTest));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        public void GreaterThan_NotGreater_Throws(float toCompare)
        {
            //Arrange
            float underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .GreaterThan(toCompare));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(float.MinValue)]
        public void GreaterEqual_GreaterOrEqual_Success(float toCompare)
        {
            //Arrange
            float underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .GreaterEqual(toCompare));


            //Assert
            exception.Should().BeNull();
        }


        [Theory]
        [InlineData(5)]
        [InlineData(-10)]
        public void LessThan_NotLess_Throws(float toCompare)
        {
            //Arrange
            float underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .LessThan(toCompare));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(float.MaxValue)]
        public void LessEqual_LessOrEqual_Success(float toCompare)
        {
            //Arrange
            float underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Float(underTest, nameof(underTest))
                .LessEqual(toCompare));


            //Assert
            exception.Should().BeNull();
        }
    }
}