using DreamTravel.Domain.Users;

namespace DreamTravel.Identity.Logging
{
    public interface ILoginUser
    {
        LoginResult Login(User loggingInUser);
    }
}
