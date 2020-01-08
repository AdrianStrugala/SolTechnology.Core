namespace DreamTravel.Identity.ChangePassword
{
    public interface IChangePassword
    {
        void Execute(ChangePasswordCommand command);
    }
}
