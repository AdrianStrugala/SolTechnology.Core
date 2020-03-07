using DreamTravel.Domain.Users;
using DreamTravel.Identity.Registration;
using NSubstitute;
using Xunit;

namespace DreamTravel.FeaturesTests.Registration
{
    public class RegisterUserTests
    {
        private readonly IUserRepository _userRepository;
        private readonly RegisterUser _sut;

        public RegisterUserTests()
        {
            _userRepository = Substitute.For<IUserRepository>();

            _sut = new RegisterUser(_userRepository);
        }

        [Fact]
        public void Register_NewUser_IsRegistered()
        {
            //Arrange
            User user = new User();
            user.Email = "adrian@buziaczek.pl";
            user.Password = "Diariusz Ehwara";

            _userRepository.Get(Arg.Any<string>()).Returns((User)null);


            //Act
            _sut.Register(user);


            //Assert
            //no exception :)
        }

        [Fact]
        public void Register_EmailAlreadyExists_RegisterExceptionIsThrown()
        {
            //Arrange
            User user = new User();
            user.Email = "adrian@buziaczek.pl";

            _userRepository.Get(Arg.Any<string>()).Returns(user);


            //Act
            var result = _sut.Register(user);


            //Assert
            Assert.NotEmpty(result.Message);
        }
    }
}
