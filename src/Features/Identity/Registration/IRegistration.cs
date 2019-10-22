using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Identity.Registration
{
    public interface IRegistration
    {
        bool Register(User user);
    }
}
