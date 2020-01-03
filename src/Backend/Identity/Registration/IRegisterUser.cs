using DreamTravel.Domain.Users;

namespace DreamTravel.Identity.Registration
{
    public interface IRegisterUser
    {
        void Register(User user);
    }
}
