using DreamTravel.Cryptography;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure;

namespace DreamTravel.Identity.Registration
{
    public class RegisterUser : IRegisterUser
    {
        private readonly IUserRepository _userRepository;

        public RegisterUser(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public CommandResult Register(User user)
        {
            CommandResult result = new CommandResult();
            var alreadyExistingUser = _userRepository.Get(user.Email);

            if (alreadyExistingUser != null)
            {
                result.Success = false;
                result.Message = "User with provided email already exists";
                return result;
            }

            user.Password = Encryption.Encrypt(user.Password);
            _userRepository.Insert(user);

            result.Success = true;
            return result;
        }
    }
}