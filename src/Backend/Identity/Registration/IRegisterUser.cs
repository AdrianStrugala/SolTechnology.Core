using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure;

namespace DreamTravel.Identity.Registration
{
    public interface IRegisterUser
    {
        CommandResult Register(User user);
    }
}
