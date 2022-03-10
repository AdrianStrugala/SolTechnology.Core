using Microsoft.AspNetCore.Authentication;

namespace SolTechnology.Core.Authentication
{
    public class SolTechnologyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string AuthenticationHeaderName = "X-Auth";
        public const string AuthenticationScheme = "SolTechnologyAuthentication";
        public string AuthenticationKey = "";
    }
}
