using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Logging
{
    public interface ILogging
    {
        int LogIn(User loggingInUser);
    }
}
