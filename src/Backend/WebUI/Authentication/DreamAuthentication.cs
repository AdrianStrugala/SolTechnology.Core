namespace DreamTravel.WebUI.Authentication
{
    using System;
    using System.Net.Http.Headers;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class DreamAuthentication : AuthenticationHandler<DreamAuthenticationOptions>
    {
        private static ILogger<DreamAuthentication> _logger;

        public DreamAuthentication(
            IOptionsMonitor<DreamAuthenticationOptions> optionsMonitor,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            ISystemClock clock,
            ILogger<DreamAuthentication> logger)
            : base(optionsMonitor, loggerFactory, encoder, clock) => _logger = logger;


        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            _logger.LogInformation($"Authentication for request [{Context.Request.Path} started");

            if (!Request.Headers.ContainsKey(DreamAuthenticationOptions.AuthenticationHeaderName))
            {
                _logger.LogWarning($"Missing authentication header");
                return AuthenticateResult.Fail("Missing authentication header");
            }

            if (!AuthenticationHeaderValue.TryParse(Request.Headers[DreamAuthenticationOptions.AuthenticationHeaderName],
                out AuthenticationHeaderValue headerValue))
            {
                _logger.LogWarning($"Invalid authentication header");
                return AuthenticateResult.Fail("Invalid authentication header");
            }

            if (!DreamAuthenticationOptions.AuthenticationScheme.Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Not DreamAuthentication schema");
                return AuthenticateResult.Fail("Not DreamAuthentication schema");
            }

            _logger.LogInformation($"Decoding incoming authentication key");
            string headerParameterDecoded = Base64Decode(headerValue.Parameter);

            if (!Options.AuthenticationKey.Equals(headerParameterDecoded,
                StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Invalid authentication key");
                return AuthenticateResult.Fail("Invalid authentication key");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, DreamAuthenticationOptions.AuthenticationScheme)
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            _logger.LogInformation($"Authenticated user: [{principal}]");
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