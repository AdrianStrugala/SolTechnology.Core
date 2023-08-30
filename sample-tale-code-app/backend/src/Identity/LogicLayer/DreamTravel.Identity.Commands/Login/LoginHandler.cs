using System.Threading.Tasks;
using DreamTravel.Identity.Cryptography;
using DreamTravel.Identity.Domain.Users;
using DreamTravel.Infrastructure;

namespace DreamTravel.Identity.Commands.Login
{
    public class LoginHandler : IQueryHandler<LoginQuery, LoginResult>
    {
        private readonly IUserRepository _userRepository;

        public LoginHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }


        public Task<LoginResult> Handle(LoginQuery query)
        {
            LoginResult result = new LoginResult();

            User userFromDb = _userRepository.Get(query.Email);

            //User does not exist
            if (userFromDb == null)
            {
                result.Message = "Email not registered";
                return Task.FromResult(result);
            }

            //Invalid password
            if (!Encryption.Decrypt(userFromDb.Password).Equals(query.Password))
            {
                result.Message = "Invalid password";
                return Task.FromResult(result);
            }

            result.User = userFromDb;

            return Task.FromResult(result);
        }
    }
}