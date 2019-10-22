using System.Collections.Generic;
using DreamTravel.Domain.Users;

namespace DreamTravel.DatabaseData
{
    public interface IUserRepository
    {
        List<User> GetAll();

        void Insert(User user);

        User Get(string userEmail);
    }
}