using DreamTravel.Cryptography;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure;

namespace DreamTravel.Identity.Registration
{
    public class RegisterUserHandler : IRegisterUser
    {
        private readonly IUserRepository _userRepository;

        public RegisterUserHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public CommandResult Handle(User user)
        {
            CommandResult result = new CommandResult();
            var alreadyExistingUser = _userRepository.Get(user.Email);

            if (alreadyExistingUser != null)
            {
                result.Success = false;
                result.Message = "User with provided email already exists";
                return result;
            }

            user.UpdatePassword(Encryption.Encrypt(user.Password));
            _userRepository.Insert(user);

            result.Success = true;
            return result;
        }
    }
}