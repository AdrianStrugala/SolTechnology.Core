using System;
using FluentAssertions;
using Xunit;

namespace SolTechnology.Core.Guards.Tests
{
    public class StringTests
    {
        private readonly Guards _guards;

        public StringTests()
        {
            _guards = new Guards();
        }

        [Fact]
        public void Null_String_Produces_Error()
        {
            //Arrange
            string underTest = null;


            //Act
            var result = _guards.String(underTest, nameof(underTest), x => x.NotNull());


            //Assert
            result.Errors.Should().HaveCount(1);
        }

        [Fact]
        public void Empty_String_Produces_Error()
        {
            //Arrange
            string underTest = "";


            //Act
            var result = _guards.String(underTest, nameof(underTest), x => x.NotEmpty());


            //Assert
            result.Errors.Should().HaveCount(1);
        }

        [Fact]
        public void String_Equal_To_Given_Produces_Error()
        {
            //Arrange
            string underTest = "EqualString";


            //Act
            var result = _guards.String(underTest, nameof(underTest), x => x.NotEqual(underTest));


            //Assert
            result.Errors.Should().HaveCount(1);
        }

        [Fact]
        public void String_Not_Equal_To_Given_Produces_Error()
        {
            //Arrange
            string underTest = "NotEqualString";


            //Act
            var result = _guards.String(underTest, nameof(underTest), x => x.Equal("AnotherString"));


            //Assert
            result.Errors.Should().HaveCount(1);
        }
    }
}