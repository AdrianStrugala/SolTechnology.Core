using DreamTravel.Infrastructure;

namespace DreamTravel.Identity.ChangePassword
{
    public interface IChangePassword
    {
        CommandResult Execute(ChangePasswordCommand command);
    }
}
