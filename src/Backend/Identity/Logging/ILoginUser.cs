using DreamTravel.Domain.Users;

namespace DreamTravel.Identity.Logging
{
    public interface ILoginUser
    {
        User Login(User loggingInUser);
    }
}
