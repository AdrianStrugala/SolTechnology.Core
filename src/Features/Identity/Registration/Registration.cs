using System;
using DreamTravel.Cryptography;
using DreamTravel.DatabaseData;
using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Identity.Registration
{
    public class Registration : IRegistration
    {
        private readonly IUserRepository _userRepository;

        public Registration(IUserRepository userRepository)
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