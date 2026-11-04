using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Auth
{
    /// <summary>
    /// Authenticates a request by comparing its <c>X-API-KEY</c> header with the configured key in
    /// constant time. Endpoints marked <see cref="IAllowAnonymous"/> are skipped; a blank configured
    /// key rejects every request.
    /// </summary>
    /// <param name="optionsMonitor">Scheme options; <see cref="ApiKeyAuthenticationSchemeOptions.ApiKey"/> holds the expected key.</param>
    /// <param name="loggerFactory">Creates the handler's <c>Logger</c>.</param>
    /// <param name="encoder">URL encoder required by <see cref="AuthenticationHandler{TOptions}"/>.</param>
    public class ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> optionsMonitor,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder)
        : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>(optionsMonitor, loggerFactory, encoder)
    {
        /// <summary>
        /// Returns <c>NoResult</c> for anonymous endpoints, <c>Fail</c> for a missing header, a wrong key or
        /// a blank configured key, and <c>Success</c> with a principal named after the scheme otherwise.
        /// </summary>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                return AuthenticateResult.NoResult();
            }

            if (string.IsNullOrWhiteSpace(Options.ApiKey))
            {
                Logger.LogWarning("No API key is configured for scheme [{Scheme}]; request [{RequestPath}] is rejected", Scheme.Name, Context.Request.Path);
                return AuthenticateResult.Fail("API key is not configured");
            }

            if (!Request.Headers.TryGetValue(ApiKeyAuthenticationSchemeOptions.AuthenticationHeaderName, out var apiKey))
            {
                Logger.LogWarning("Missing authentication header for request [{RequestPath}]", Context.Request.Path);
                return AuthenticateResult.Fail("Missing authentication header");
            }

            if (!IsConfiguredKey(apiKey.ToString()))
            {
                Logger.LogWarning("Invalid API key for request [{RequestPath}]", Context.Request.Path);
                return AuthenticateResult.Fail("Invalid API key");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, ApiKeyAuthenticationSchemeOptions.AuthenticationScheme)
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return await Task.FromResult(AuthenticateResult.Success(ticket));
        }

        // Equal-length hashes keep the comparison time independent of the key's length and content.
        private bool IsConfiguredKey(string candidate) => CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(candidate)),
            SHA256.HashData(Encoding.UTF8.GetBytes(Options.ApiKey)));
    }
}