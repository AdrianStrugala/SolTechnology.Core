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
            CommandResult result = new CommandResult();
            var alreadyExistingUser = _userRepository.Get(command.User.Email);

            if (alreadyExistingUser != null)
            {
                result.Success = false;
                result.Message = "User with provided email already exists";
                return result;
            }

            command.User.UpdatePassword(Encryption.Encrypt(command.User.Password));
            _userRepository.Insert(command.User);

            result.Success = true;
            return result;
        }
    }
}