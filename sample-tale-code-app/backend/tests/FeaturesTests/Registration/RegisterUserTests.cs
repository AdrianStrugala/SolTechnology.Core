using DreamTravel.Domain.Users;
using DreamTravel.Identity.Register;
using NSubstitute;
using Xunit;

namespace DreamTravel.FeaturesTests.Registration
{
    public class RegisterUserTests
    {
        private readonly IUserRepository _userRepository;
        private readonly RegisterUserHandler _sut;

        public RegisterUserTests()
        {
            _userRepository = Substitute.For<IUserRepository>();

            _sut = new RegisterUserHandler(_userRepository);
        }

        [Fact]
        public void Register_NewUser_IsRegistered()
        {
            //Arrange
            User user = new User(
            email: "adrian@buziaczek.pl",
            password: "Diariusz Ehwara",
            name: "Ehwar jest wielki!"
            );

            _userRepository.Get(Arg.Any<string>()).Returns((User)null);


            //Act
            _sut.Handle(new RegisterUserCommand(user.Name, user.Password, user.Email));


            //Assert
            //no exception :)
        }

        [Fact]
        public void Register_EmailAlreadyExists_RegisterExceptionIsThrown()
        {
            //Arrange
            User user = new User(
                email: "adrian@buziaczek.pl",
                password: "Diariusz Ehwara",
                name: "Ehwar jest wielki!"
            );

            _userRepository.Get(Arg.Any<string>()).Returns(user);


            //Act
            var result = _sut.Handle(new RegisterUserCommand(user.Name, user.Password, user.Email));


            //Assert
            Assert.NotEmpty(result.Message);
        }
    }
}
