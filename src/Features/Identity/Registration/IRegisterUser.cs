using DreamTravel.Domain.Users;

namespace DreamTravel.Features.Identity.Registration
{
    public interface IRegisterUser
    {
        bool Register(User user);
    }
}
