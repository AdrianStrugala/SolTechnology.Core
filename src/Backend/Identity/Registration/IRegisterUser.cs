using DreamTravel.Domain.Users;

namespace DreamTravel.Identity.Registration
{
    public interface IRegisterUser
    {
        RegisterResult Register(User user);
    }
}
