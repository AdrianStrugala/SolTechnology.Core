using System.Threading.Tasks;
using DreamTravel.Identity.Cryptography;
using DreamTravel.Identity.Domain.Users;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Identity.Commands.Login
{
    public class LoginHandler : ICommandHandler<LoginQuery, Result<LoginResult>>
    {
        private readonly IUserRepository _userRepository;

        public LoginHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }


        public Task<Result<LoginResult>> Handle(LoginQuery query)
        {
            LoginResult result = new LoginResult();

            User userFromDb = _userRepository.Get(query.Email);

            //User does not exist
            if (userFromDb == null)
            {
                return Result<LoginResult>.FailedTask("Email not registered");
            }

            //Invalid password
            if (!Encryption.Decrypt(userFromDb.Password).Equals(query.Password))
            {
                return Result<LoginResult>.FailedTask("Invalid password");
            }

            result.User = userFromDb;

            return Result<LoginResult>.SucceededTask(result);
        }
    }
}