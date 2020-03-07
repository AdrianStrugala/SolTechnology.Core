using DreamTravel.Cryptography;
using DreamTravel.Domain.Users;

namespace DreamTravel.Identity.Registration
{
    public class RegisterUser : IRegisterUser
    {
        private readonly IUserRepository _userRepository;

        public RegisterUser(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public RegisterResult Register(User user)
        {
            RegisterResult result = new RegisterResult();
            var alreadyExistingUser = _userRepository.Get(user.Email);

            if (alreadyExistingUser != null)
            {
                result.Message = "User with provided email already exists";
                return result;
            }

            user.Password = Encryption.Encrypt(user.Password);
            _userRepository.Insert(user);

            return result;
        }
    }
}