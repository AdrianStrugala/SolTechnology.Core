using DreamTravel.Cryptography;
using DreamTravel.Domain.Users;
using DreamTravel.Identity.ChangePassword;

namespace DreamTravel.Identity.Logging
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
            throw new System.NotImplementedException();
        }
    }
}