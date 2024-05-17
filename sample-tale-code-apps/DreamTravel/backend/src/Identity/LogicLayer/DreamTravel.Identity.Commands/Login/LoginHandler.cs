using System.Threading;
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


        public Task<Result<LoginResult>> Handle(LoginQuery query, CancellationToken cancellationToken)
        {
            LoginResult result = new LoginResult();

            User userFromDb = _userRepository.Get(query.Email);

            //User does not exist
            if (userFromDb == null)
            {
                return Result<LoginResult>.FailAsTask("Email not registered");
            }

            //Invalid password
            if (!Encryption.Decrypt(userFromDb.Password).Equals(query.Password))
            {
                return Result<LoginResult>.FailAsTask("Invalid password");
            }

            result.User = userFromDb;

            return Result<LoginResult>.SuccessAsTask(result);
        }
    }
}