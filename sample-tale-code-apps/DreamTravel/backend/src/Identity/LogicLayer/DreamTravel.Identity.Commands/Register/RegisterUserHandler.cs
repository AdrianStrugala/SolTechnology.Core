using System.Threading.Tasks;
using DreamTravel.Identity.Cryptography;
using DreamTravel.Identity.DatabaseData.Repositories.Users;
using DreamTravel.Identity.Domain.Users;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Identity.Commands.Register
{
    public class RegisterUserHandler : ICommandHandler<RegisterUserCommand>
    {
        private readonly IUserRepository _userRepository;

        public RegisterUserHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<OperationResult> Handle(RegisterUserCommand command)
        {
            var alreadyExistingUser = _userRepository.Get(command.Email);

            if (alreadyExistingUser != null)
            {
                return OperationResult.FailedTask("User with provided email already exists");
            }

            var user = new User(command.Name, command.Password, command.Email);

            user.UpdatePassword(Encryption.Encrypt(user.Password));
            _userRepository.Insert(user);

            return OperationResult.SucceededTask();
        }
    }
}