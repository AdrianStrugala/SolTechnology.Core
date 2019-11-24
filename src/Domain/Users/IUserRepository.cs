using System.Collections.Generic;

namespace DreamTravel.Domain.Users
{
    public interface IUserRepository
    {
        List<User> GetPreviewUsers();

        void Insert(User user);

        User Get(string userEmail);
    }
}