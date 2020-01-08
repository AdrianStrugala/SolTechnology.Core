namespace DreamTravel.Identity.ChangePassword
{
   public class ChangePasswordCommand
    {
        public int UserId { get; set; }

        public string CurrentPassword { get; set; }

        public string NewPassword { get; set; }
    }
}
