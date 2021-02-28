using System;

namespace DreamTravel.Domain.Users
{
    public interface IUserRepository
    {
        void Insert(User user);

        User Get(string userEmail);

        User Get(Guid id);

        void Update(User user);
    }
}