using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Identity.Logging
{
    public interface ILogging
    {
        int LogIn(User loggingInUser);
    }
}
