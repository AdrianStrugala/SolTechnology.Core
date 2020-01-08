using DreamTravel.Cryptography;
using DreamTravel.Domain.Users;

namespace DreamTravel.Identity.ChangePassword
{
    public class ChangePassword : IChangePassword
    {
        private readonly IUserRepository _userRepository;

        public ChangePassword(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public void Execute(ChangePasswordCommand command)
        {
            var user = _userRepository.Get(command.UserId);

            var newPassword = Encryption.Encrypt(command.NewPassword);
            var currentPassword = Encryption.Encrypt(command.CurrentPassword);

            if (currentPassword != user.Password)
            {
                throw new ChangePasswordException("Current password is invalid");
            }

            user.Password = newPassword;

            _userRepository.Update(user);
        }
    }
}