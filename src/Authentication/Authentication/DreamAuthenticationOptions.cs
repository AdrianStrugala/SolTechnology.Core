using Microsoft.AspNetCore.Authentication;

namespace Authentication.Authentication
{
    public class DreamAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string Key { get; set; }
    }
}