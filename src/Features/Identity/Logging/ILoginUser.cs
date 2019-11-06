using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Identity.Logging
{
    public interface ILoginUser
    {
        User Login(User loggingInUser);
    }
}
