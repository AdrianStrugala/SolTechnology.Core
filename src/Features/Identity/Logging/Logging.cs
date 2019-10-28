using DreamTravel.Cryptography;
using DreamTravel.DatabaseData;
using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Identity.Logging
{
    public class Logging : ILogging
    {
        private readonly IUserRepository _userRepository;

        public Logging(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }


        public int LogIn(User loggingInUser)
        {
            User userFromDb = _userRepository.Get(loggingInUser.Email);

            //User does not exist
            if (userFromDb == null)
            {
                throw new LoginException("Email not registered");
            }

            //TODO REMOVE THE HACK WHEN REAL USERS WILL ARRIVE
            if (loggingInUser.Password.Equals("Password") && userFromDb.Password.Equals(loggingInUser.Password))
            {
                return userFromDb.Id;
            }

            if (!Encryption.Decrypt(userFromDb.Password).Equals(loggingInUser.Password))
            {
                throw new LoginException("Invalid password");
            }

            return userFromDb.Id;

        }
    }
}