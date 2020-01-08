namespace DreamTravel.Identity.ChangePassword
{
   public class ChangePasswordCommand
    {
        public int UserId { get; set; }

        public string OldPassword { get; set; }

        public string NewPassword { get; set; }
    }
}
