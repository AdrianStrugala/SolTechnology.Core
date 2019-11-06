using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Identity.Logging
{
    public interface ILogging
    {
        User Login(User loggingInUser);
    }
}
