using DreamTravel.DatabaseData;
using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Identity.Logging
{
    public class LoginUser : ILoginUser
    {
        private readonly IUserRepository _userRepository;

        public LoginUser(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }


        public User Login(User loggingInUser)
        {
            User userFromDb = _userRepository.Get(loggingInUser.Email);

            //User does not exist
            if (userFromDb == null)
            {
                throw new LoginException("Email not registered");
            }

            //TODO REMOVE THE HACK WHEN REAL USERS WILL ARRIVE
            if (userFromDb.Password.Equals(loggingInUser.Password))
            {
                return userFromDb;
            }

            else
            {
                throw new LoginException("Invalid password");
            }

            //TODO BRING IT BACK WHEN REAL USERS ARRIVE
//            if (!Encryption.Decrypt(userFromDb.Password).Equals(loggingInUser.Password))
//            {
//                throw new LoginException("Invalid password");
//            }

//            return userFromDb.Id;

        }
    }
}