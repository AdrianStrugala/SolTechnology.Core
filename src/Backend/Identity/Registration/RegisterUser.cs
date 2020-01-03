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

        public void Register(User user)
        {
            var alreadyExistingUser = _userRepository.Get(user.Email);

            if (alreadyExistingUser != null)
            {
                throw new RegisterException("User with provided email already exists");
            }

            user.Password = Encryption.Encrypt(user.Password);
            _userRepository.Insert(user);
        }
    }
}