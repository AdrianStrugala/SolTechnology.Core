using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Registration
{
    public interface IRegistration
    {
        bool Register(User user);
    }
}
