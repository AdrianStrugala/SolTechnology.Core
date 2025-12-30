using Microsoft.AspNetCore.Authentication;

namespace SolTechnology.Core.Authentication
{
    public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public const string AuthenticationHeaderName = "X-API-KEY";
        public const string AuthenticationScheme = "ApiKey";
        public string ApiKey = "";
    }
}
