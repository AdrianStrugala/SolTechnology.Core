using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Registration
{
    public interface IRegistration
    {
        void Register(User user);
    }
}
