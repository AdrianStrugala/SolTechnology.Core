using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Authentication.Authentication
{
    public class DreamAuthentication : AuthenticationHandler<DreamAuthenticationOptions>
    {
        public DreamAuthentication(IOptionsMonitor<DreamAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
            // store custom services here...
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var sth = Context.Request.Path;
            // build the claims and put them in "Context"; you need to import the Microsoft.AspNetCore.Authentication package
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), OriginalPath));
        }
    }  
}
