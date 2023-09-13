using System;
using DreamTravel.Identity.Domain.Users;

namespace DreamTravel.Identity.DatabaseData.Repositories.Users
{
    public interface IUserRepository
    {
        void Insert(User user);

        User Get(string userEmail);

        User Get(Guid id);

        void Update(User user);
    }
}