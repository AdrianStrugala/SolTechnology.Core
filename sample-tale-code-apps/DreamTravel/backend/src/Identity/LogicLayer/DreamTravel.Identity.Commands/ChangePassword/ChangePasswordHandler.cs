﻿using System.Threading;
using System.Threading.Tasks;
using DreamTravel.Identity.Cryptography;
using DreamTravel.Identity.DatabaseData.Repositories.Users;
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

        public Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            var user = _userRepository.Get(command.UserId);

            var newPassword = Encryption.Encrypt(command.NewPassword);
            var currentPassword = Encryption.Encrypt(command.CurrentPassword);

            if (currentPassword != user.Password)
            {
                return Result.FailAsTask("Given password does not match user password");
            }

            user.UpdatePassword(newPassword);

            _userRepository.Update(user);

            return Result.SuccessAsTask();
        }
    }
}