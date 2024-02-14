using System.Threading.Tasks;
using DreamTravel.Identity.Cryptography;
using DreamTravel.Identity.DatabaseData.Repositories.Users;
using DreamTravel.Identity.Domain.Users;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Identity.Commands.Login
{
    public class LoginHandler : ICommandHandler<LoginQuery, LoginResult>
    {
        private readonly IUserRepository _userRepository;

        public LoginHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }


        public Task<ResultBase<LoginResult>> Handle(LoginQuery query)
        {
            LoginResult result = new LoginResult();

            User userFromDb = _userRepository.Get(query.Email);

            //User does not exist
            if (userFromDb == null)
            {
                return ResultBase<LoginResult>.FailedTask("Email not registered");
            }

            //Invalid password
            if (!Encryption.Decrypt(userFromDb.Password).Equals(query.Password))
            {
                return ResultBase<LoginResult>.FailedTask("Invalid password");
            }

            result.User = userFromDb;

            return ResultBase<LoginResult>.SucceededTask(result);
        }
    }
}