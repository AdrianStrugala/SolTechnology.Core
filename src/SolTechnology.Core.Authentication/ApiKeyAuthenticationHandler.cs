using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
    {
        private static ILogger<ApiKeyAuthenticationHandler> _logger = null!;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> optionsMonitor,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            ILogger<ApiKeyAuthenticationHandler> logger)
            : base(optionsMonitor, loggerFactory, encoder) => _logger = logger;


        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                return AuthenticateResult.NoResult();
            }
            
            if (!Request.Headers.TryGetValue(ApiKeyAuthenticationSchemeOptions.AuthenticationHeaderName, out var apiKey))
            {
                _logger.LogWarning("Missing authentication header for request [{RequestPath}]", Context.Request.Path);
                return AuthenticateResult.Fail("Missing authentication header");
            }

            if (!Options.ApiKey.Equals(apiKey.ToString()))
            {
                _logger.LogWarning("Invalid API key for request [{RequestPath}]", Context.Request.Path);
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
    }
}