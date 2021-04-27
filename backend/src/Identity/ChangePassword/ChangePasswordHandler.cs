using DreamTravel.Cryptography;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure;

namespace DreamTravel.Identity.ChangePassword
{
    public class ChangePasswordHandler : ICommandHandler<ChangePasswordCommand>
    {
        private readonly IUserRepository _userRepository;

        public ChangePasswordHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public CommandResult Handle(ChangePasswordCommand command)
        {
            var user = _userRepository.Get(command.UserId);

            var newPassword = Encryption.Encrypt(command.NewPassword);
            var currentPassword = Encryption.Encrypt(command.CurrentPassword);

            if (currentPassword != user.Password)
            {
                return CommandResult.Failed("Given password does not match user password");
            }

            user.UpdatePassword(newPassword);

            _userRepository.Update(user);

            return CommandResult.Succeeded();
        }
    }
}