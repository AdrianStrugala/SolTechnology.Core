using DreamTravel.Cryptography;
using DreamTravel.DatabaseData;
using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Registration
{
    public class Registration : IRegistration
    {
        private readonly IUserRepository _userRepository;

        public Registration(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public void Register(User user)
        {
            user.Password = Encryption.Encrypt(user.Password);

            _userRepository.Insert(user);
        }
    }
}