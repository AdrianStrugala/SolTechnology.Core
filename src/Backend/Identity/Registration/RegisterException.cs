using System;

namespace DreamTravel.Identity.Registration
{
    public class RegisterException : Exception
    {
        public RegisterException(string message) : base(message)
        {
        }
    }
}

