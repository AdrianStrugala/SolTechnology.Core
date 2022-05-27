using System;
using FluentAssertions;
using Xunit;

namespace SolTechnology.Core.Guards.Tests
{
    public class FloatTests
    {
        private readonly Guards _guards;

        public FloatTests()
        {
            _guards = new Guards();
        }

        [Fact]
        public void NotZero_Zero_Throws()
        {
            //Arrange
            float underTest = 0;


            //Act
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .NotZero());


            //Assert
            result.Errors.Should().HaveCount(1);
        }

        [Theory]
        [InlineData(float.MinValue)]
        [InlineData(-1)]
        public void NotNegative_Negative_Throws(float underTest)
        {
            //Arrange


            //Act
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .NotNegative());


            //Assert
            result.Errors.Should().HaveCount(1);
        }


        [Theory]
        [InlineData(float.MaxValue)]
        [InlineData(1)]
        public void NotPositive_Positive_Throws(float underTest)
        {
            //Arrange


            //Act
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .NotPositive());


            //Assert
            result.Errors.Should().HaveCount(1);
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
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .InRange(-5, 10));


            //Assert
            result.Errors.Should().HaveCount(1);
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void InRange_InRange_NoException(float underTest)
        {
            //Arrange


            //Act
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .InRange(-5, 10));


            //Assert
            result.Errors.Should().HaveCount(0);
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
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .NotInRange(-5, 10));


            //Assert
            result.Errors.Should().HaveCount(0);
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(10)]
        [InlineData(0)]
        public void NotInRange_InRange_Throws(float underTest)
        {
            //Arrange


            //Act
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .NotInRange(-5, 10));


            //Assert
            result.Errors.Should().HaveCount(1);
        }


        [Fact]
        public void Equal_NotEqual_Throws()
        {
            //Arrange
            float underTest = 5;

            //Act
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .Equal(50));


            //Assert
            result.Errors.Should().HaveCount(1);
        }

        [Fact]
        public void NotEqual_Equal_Throws()
        {
            //Arrange
            float underTest = 5;

            //Act
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .NotEqual(underTest));


            //Assert
            result.Errors.Should().HaveCount(1);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        public void GreaterThan_NotGreater_Throws(float toCompare)
        {
            //Arrange
            float underTest = 5;

            //Act
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .GreaterThan(toCompare));


            //Assert
            result.Errors.Should().HaveCount(1);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(float.MinValue)]
        public void GreaterEqual_GreaterOrEqual_Success(float toCompare)
        {
            //Arrange
            float underTest = 5;

            //Act
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .GreaterEqual(toCompare));


            //Assert
            result.Errors.Should().HaveCount(0);
        }


        [Theory]
        [InlineData(5)]
        [InlineData(-10)]
        public void LessThan_NotLess_Throws(float toCompare)
        {
            //Arrange
            float underTest = 5;

            //Act
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .LessThan(toCompare));


            //Assert
            result.Errors.Should().HaveCount(1);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(float.MaxValue)]
        public void LessEqual_LessOrEqual_Success(float toCompare)
        {
            //Arrange
            float underTest = 5;

            //Act
            var result = _guards.Float(underTest, nameof(underTest), x => x
                .LessEqual(toCompare));


            //Assert
            result.Errors.Should().HaveCount(0);
        }
    }
}