using DreamTravel.Domain.Users;
using DreamTravel.Identity.Cryptography;
using DreamTravel.Infrastructure;

namespace DreamTravel.Identity.Commands.Register
{
    public class RegisterUserHandler : ICommandHandler<RegisterUserCommand>
    {
        private readonly IUserRepository _userRepository;

        public RegisterUserHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public CommandResult Handle(RegisterUserCommand command)
        {
            var alreadyExistingUser = _userRepository.Get(command.Email);

            if (alreadyExistingUser != null)
            {
                return CommandResult.Failed("User with provided email already exists");
            }

            var user = new User(command.Name, command.Password, command.Email);

            user.UpdatePassword(Encryption.Encrypt(user.Password));
            _userRepository.Insert(user);

            return CommandResult.Succeeded();
        }
    }
}