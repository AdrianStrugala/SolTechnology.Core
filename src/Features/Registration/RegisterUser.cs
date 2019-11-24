using System;
using DreamTravel.Cryptography;
using DreamTravel.Domain.Users;

namespace DreamTravel.Identity.Registration
{
    public class RegisterUser : IRegisterUser
    {
        private readonly IUserRepository _userRepository;

        public RegisterUser(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public bool Register(User user)
        {
            try
            {
                user.Password = Encryption.Encrypt(user.Password);

                _userRepository.Insert(user);

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}