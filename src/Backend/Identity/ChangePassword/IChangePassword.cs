using DreamTravel.Infrastructure;

namespace DreamTravel.Identity.ChangePassword
{
    public interface IChangePassword
    {
        CommandResult Handle(ChangePasswordCommand command);
    }
}
