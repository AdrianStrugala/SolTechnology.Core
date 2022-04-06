using System;
using FluentAssertions;
using Xunit;

namespace SolTechnology.Core.Guards.Tests
{
    public class DecimalTests
    {
        [Fact]
        public void NotZero_Zero_Throws()
        {
            //Arrange
            decimal underTest = 0;


            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .NotZero());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(-79228162514264335)]
        [InlineData(-1)]
        public void NotNegative_Negative_Throws(decimal underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .NotNegative());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }


        [Theory]
        [InlineData(7922816251423375335)]
        [InlineData(1)]
        public void NotPositive_Positive_Throws(decimal underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .NotPositive());


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(7922816259343950335)]
        [InlineData(-7922816251543950335)]
        [InlineData(-6)]
        [InlineData(11)]
        public void InRange_NotInRange_Throws(decimal underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .InRange(-5, 10));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void InRange_InRange_NoException(decimal underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .InRange(-5, 10));


            //Assert
            exception.Should().BeNull();
        }


        [Theory]
        [InlineData(7922816251426430335)]
        [InlineData(-7922816251543950335)]
        [InlineData(-6)]
        [InlineData(11)]
        public void NotInRange_NotInRange_NoException(decimal underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .NotInRange(-5, 10));


            //Assert
            exception.Should().BeNull();
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void NotInRange_InRange_Throws(decimal underTest)
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .NotInRange(-5, 10));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }


        [Fact]
        public void Equal_NotEqual_Throws()
        {
            //Arrange
            decimal underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .Equal(50));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void NotEqual_Equal_Throws()
        {
            //Arrange
            decimal underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .NotEqual(underTest));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        public void GreaterThan_NotGreater_Throws(decimal toCompare)
        {
            //Arrange
            decimal underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .GreaterThan(toCompare));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(-7922816)]
        public void GreaterEqual_GreaterOrEqual_Success(decimal toCompare)
        {
            //Arrange
            Decimal underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .GreaterEqual(toCompare));


            //Assert
            exception.Should().BeNull();
        }


        [Theory]
        [InlineData(5)]
        [InlineData(-10)]
        public void LessThan_NotLess_Throws(decimal toCompare)
        {
            //Arrange
            decimal underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .LessThan(toCompare));


            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(7922816251426433)]
        public void LessEqual_LessOrEqual_Success(decimal toCompare)
        {
            //Arrange
            Decimal underTest = 5;

            //Act
            var exception = Record.Exception(() => Guards.Decimal(underTest, nameof(underTest))
                .LessEqual(toCompare));


            //Assert
            exception.Should().BeNull();
        }
    }
}