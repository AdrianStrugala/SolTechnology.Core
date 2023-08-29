using System;

namespace DreamTravel.Identity.Domain.Users
{
    public interface IUserRepository
    {
        void Insert(User user);

        User Get(string userEmail);

        User Get(Guid id);

        void Update(User user);
    }
}