using System;

namespace DreamTravel.Identity.Logging
{
    public class LoginException : Exception
    {
        public LoginException(string message) : base(message)
        {
        }
    }
}

