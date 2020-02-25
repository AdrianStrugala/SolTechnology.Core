namespace DreamTravel.Domain.Users
{
    public interface IUserRepository
    {
        void Insert(User user);

        User Get(string userEmail);

        User Get(int id);

        void Update(User user);
    }
}