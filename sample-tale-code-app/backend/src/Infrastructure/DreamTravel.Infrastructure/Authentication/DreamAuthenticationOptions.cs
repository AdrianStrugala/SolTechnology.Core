using Microsoft.AspNetCore.Authentication;

namespace DreamTravel.Infrastructure.Authentication
{
    public class DreamAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string AuthenticationHeaderName = "Authorization";
        public const string AuthenticationScheme = "DreamAuthentication";
        public string AuthenticationKey = "SolUberAlles";
    }
}
