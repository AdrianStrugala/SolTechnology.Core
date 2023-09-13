using System;
using DreamTravel.Identity.Domain.Users;
using Xunit;

namespace DreamTravel.Identity.Domain.UnitTests
{
    public class UserTests
    {
        [Fact]
        public void Ctor_InvalidArguments_ExceptionIsThrown()
        {
            //Arrange


            //Act
            var exception = Record.Exception(() => new User(",", "", ""));


            //Assert
            Assert.IsType<ArgumentException>(exception);
        }


        [Fact]
        public void User_IsCopied_CopiedAreValuesButNotReference()
        {
            //Arrange
            User user = new User(
                email: "adrian@buziaczek.pl",
                password: "Diariusz Ehwara",
                name: "Ehwar jest wielki!"
            );


            //Act
            var copyOfUser = new User(user.Name, user.Password, user.Email);

            //change original user password
            user.UpdatePassword("Ace, Uczen i Khajitka");


            //Assert
            //passwords are different
            
            Assert.Equal(user.Email, copyOfUser.Email);
            Assert.Equal(user.Name, copyOfUser.Name);
            Assert.NotEqual(user.Password, copyOfUser.Password);
            Assert.Equal("Ace, Uczen i Khajitka", user.Password);
            Assert.Equal("Diariusz Ehwara", copyOfUser.Password);
        }
    }
}
