using DreamTravel.Cryptography;
using DreamTravel.DatabaseData;
using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Logging
{
    public class Logging : ILogging
    {
        private readonly IUserRepository _userRepository;

        public Logging(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // RESPONSES:
        // 1-9999999 - UserId
        // -2 - User does not exist
        // -1 - Invalid password


        public int LogIn(User loggingInUser)
        {
            User userFromDb = _userRepository.Get(loggingInUser.Email);

            //User does not exist
            if (userFromDb == null)
            {
                return -2;
            }

            //Valid logging
            if (Encryption.Decrypt(userFromDb.Password).Equals(loggingInUser.Password))
            {
                return userFromDb.Id;
            }

            //Invalid password
            return -1;
        }
    }
}