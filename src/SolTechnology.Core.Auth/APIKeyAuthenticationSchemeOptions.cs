using Microsoft.AspNetCore.Authentication;

namespace SolTechnology.Core.Auth
{
    /// <summary>
    /// Options of the API-key authentication scheme registered by <see cref="ModuleInstaller.AddSolAuth"/>.
    /// </summary>
    public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        /// <summary>Request header that carries the API key.</summary>
        public const string AuthenticationHeaderName = "X-API-KEY";

        /// <summary>Name of the authentication scheme, also the default scheme set by <see cref="ModuleInstaller.AddSolAuth"/>.</summary>
        public const string AuthenticationScheme = "ApiKey";

        /// <summary>The expected API key. A blank value never authenticates.</summary>
        public string ApiKey { get; set; } = "";
    }
}
