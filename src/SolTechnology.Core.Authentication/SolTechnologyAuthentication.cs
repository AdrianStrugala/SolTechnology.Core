using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Authentication
{
    public class SolTechnologyAuthentication : AuthenticationHandler<SolTechnologyAuthenticationOptions>
    {
        private static ILogger<SolTechnologyAuthentication> _logger;

        public SolTechnologyAuthentication(
            IOptionsMonitor<SolTechnologyAuthenticationOptions> optionsMonitor,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            ISystemClock clock,
            ILogger<SolTechnologyAuthentication> logger)
            : base(optionsMonitor, loggerFactory, encoder, clock) => _logger = logger;


        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            _logger.LogDebug($"Authentication for request [{Context.Request.Path} started");

            if (!Request.Headers.ContainsKey(SolTechnologyAuthenticationOptions.AuthenticationHeaderName))
            {
                _logger.LogWarning($"Missing authentication header");
                return AuthenticateResult.Fail("Missing authentication header");
            }

            if (!AuthenticationHeaderValue.TryParse(Request.Headers[SolTechnologyAuthenticationOptions.AuthenticationHeaderName],
                out AuthenticationHeaderValue headerValue))
            {
                _logger.LogWarning($"Invalid authentication header");
                return AuthenticateResult.Fail("Invalid authentication header");
            }

            if (!SolTechnologyAuthenticationOptions.AuthenticationScheme.Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Not SolTechnologyAuthentication schema");
                return AuthenticateResult.Fail("Not SolTechnologyAuthentication schema");
            }

            _logger.LogDebug($"Decoding incoming authentication key");
            string headerParameterDecoded = Base64Decode(headerValue.Parameter);

            if (!Options.AuthenticationKey.Equals(headerParameterDecoded,
                StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Invalid authentication key");
                return AuthenticateResult.Fail("Invalid authentication key");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, SolTechnologyAuthenticationOptions.AuthenticationScheme)
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            _logger.LogDebug($"Authenticated user: [{principal}]");
            return await Task.FromResult(AuthenticateResult.Success(ticket));
        }

        public static string Base64Decode(string base64EncodedData)
        {
            byte[] base64EncodedBytes = { };
            try
            {
                base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            }
            catch (FormatException e)
            {
                _logger.LogWarning($"Incoming key [{base64EncodedData}] was in incorrect format, {e}");
            }

            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}