using DreamTravel.Domain.Users;

namespace DreamTravel.Identity.Logging
{
    public class LoginResult
    {
        public string Message { get; set; }

        public User User { get; set; }

        public LoginResult()
        {
            Message = string.Empty;
        }
    }
}
