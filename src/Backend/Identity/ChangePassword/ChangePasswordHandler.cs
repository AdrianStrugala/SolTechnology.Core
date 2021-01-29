using DreamTravel.Cryptography;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure;

namespace DreamTravel.Identity.ChangePassword
{
    public class ChangePasswordHandler : IChangePassword
    {
        private readonly IUserRepository _userRepository;

        public ChangePasswordHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public CommandResult Handle(ChangePasswordCommand command)
        {
            var result = new CommandResult();
            var user = _userRepository.Get(command.UserId);

            var newPassword = Encryption.Encrypt(command.NewPassword);
            var currentPassword = Encryption.Encrypt(command.CurrentPassword);

            if (currentPassword != user.Password)
            {
                result.Message = "Given password does not match user password";
                return result;
            }

            user.Password = newPassword;

            _userRepository.Update(user);

            result.Success = true;
            return result;
        }
    }
}