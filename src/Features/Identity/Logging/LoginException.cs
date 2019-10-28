using System;

namespace DreamTravel.Features.Identity.Logging
{
    public class LoginException : Exception
    {
        public LoginException(string message) : base(message)
        {
        }
    }
}

