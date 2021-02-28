using DreamTravel.Domain.Users;

namespace DreamTravel.Identity.Logging
{
    public interface ILoginUser
    {
        LoginResult Handle(User loggingInUser);
    }
}
