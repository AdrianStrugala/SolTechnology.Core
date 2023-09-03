using System.Threading.Tasks;
using DreamTravel.Identity.Cryptography;
using DreamTravel.Identity.Domain.Users;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Identity.Commands.ChangePassword
{
    public class ChangePasswordHandler : ICommandHandler<ChangePasswordCommand>
    {
        private readonly IUserRepository _userRepository;

        public ChangePasswordHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<CommandResult> Handle(ChangePasswordCommand command)
        {
            var user = _userRepository.Get(command.UserId);

            var newPassword = Encryption.Encrypt(command.NewPassword);
            var currentPassword = Encryption.Encrypt(command.CurrentPassword);

            if (currentPassword != user.Password)
            {
                return CommandResult.FailedTask("Given password does not match user password");
            }

            user.UpdatePassword(newPassword);

            _userRepository.Update(user);

            return CommandResult.SucceededTask();
        }
    }
}