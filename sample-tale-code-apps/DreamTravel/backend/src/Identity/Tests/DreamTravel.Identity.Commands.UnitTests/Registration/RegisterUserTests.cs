using DreamTravel.Identity.Commands.Register;
using DreamTravel.Identity.DatabaseData.Repositories.Users;
using DreamTravel.Identity.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace DreamTravel.Identity.Commands.UnitTests.Registration
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
        public async Task Register_NewUser_IsRegistered()
        {
            //Arrange
            User user = new User(
            email: "adrian@buziaczek.pl",
            password: "Diariusz Ehwara",
            name: "Ehwar jest wielki!"
            );

            _userRepository.Get(Arg.Any<string>()).Returns((User)null);


            //Act
            var result = await _sut.Handle(new RegisterUserCommand(user.Name, user.Password, user.Email), CancellationToken.None);


            //Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Register_EmailAlreadyExists_RegisterExceptionIsThrown()
        {
            //Arrange
            User user = new User(
                email: "adrian@buziaczek.pl",
                password: "Diariusz Ehwara",
                name: "Ehwar jest wielki!"
            );

            _userRepository.Get(Arg.Any<string>()).Returns(user);


            //Act
            var result = await _sut.Handle(new RegisterUserCommand(user.Name, user.Password, user.Email), CancellationToken.None);


            //Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Message.Should().NotBeNullOrEmpty();
        }
    }
}
