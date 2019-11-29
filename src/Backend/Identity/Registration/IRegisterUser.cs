using DreamTravel.Domain.Users;

namespace DreamTravel.Identity.Registration
{
    public interface IRegisterUser
    {
        bool Register(User user);
    }
}
