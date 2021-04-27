using DreamTravel.Cryptography;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure;

namespace DreamTravel.Identity.Registration
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
            var alreadyExistingUser = _userRepository.Get(command.User.Email);

            if (alreadyExistingUser != null)
            {
                return CommandResult.Failed("User with provided email already exists");
            }

            command.User.UpdatePassword(Encryption.Encrypt(command.User.Password));
            _userRepository.Insert(command.User);

            return CommandResult.Succeeded();
        }
    }
}