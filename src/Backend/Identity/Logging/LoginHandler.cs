using DreamTravel.Cryptography;
using DreamTravel.Domain.Users;

namespace DreamTravel.Identity.Logging
{
    public class LoginHandler : ILoginUser
    {
        private readonly IUserRepository _userRepository;

        public LoginHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }


        public LoginResult Handle(User loggingInUser)
        {
            LoginResult result = new LoginResult();

            User userFromDb = _userRepository.Get(loggingInUser.Email);

            //User does not exist
            if (userFromDb == null)
            {
                result.Message = "Email not registered";
                return result;
            }

            //Invalid password
            if (!Encryption.Decrypt(userFromDb.Password).Equals(loggingInUser.Password))
            {
                result.Message = "Invalid password";
                return result;
            }

            result.User = userFromDb;

            return result;
        }
    }
}