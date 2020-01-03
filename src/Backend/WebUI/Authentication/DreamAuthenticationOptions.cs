namespace DreamTravel.WebUI.Authentication
{
    using Microsoft.AspNetCore.Authentication;

    public class DreamAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string AuthenticationHeaderName = "Authorization";
        public const string AuthenticationScheme = "DreamAuthentication";
        public string AuthenticationKey { get; set; }
    }
}
