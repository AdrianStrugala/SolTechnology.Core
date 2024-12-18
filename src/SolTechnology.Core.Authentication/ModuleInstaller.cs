using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Authentication
{
    public static class ModuleInstaller
    {
        public static AuthorizeFilter AddAuthenticationAndBuildFilter(
            this IServiceCollection services,
            AuthenticationConfiguration authenticationConfiguration = null)
        {
            services
            .AddOptions<AuthenticationConfiguration>()
            .Configure<IConfiguration>((config, configuration) =>
            {
                if (authenticationConfiguration == null)
                {
                    authenticationConfiguration = configuration.GetSection("SolTechnology:Authentication").Get<AuthenticationConfiguration>();
                }

                if (authenticationConfiguration == null)
                {
                    throw new ArgumentException($"The [{nameof(AuthenticationConfiguration)}] is missing. Provide it by parameter or configuration section");
                }

                config.Key = authenticationConfiguration.Key;
            });

            var options = services.BuildServiceProvider().GetRequiredService<IOptions<AuthenticationConfiguration>>().Value;

            services.AddAuthentication(SolTechnologyAuthenticationOptions.AuthenticationScheme)
                .AddScheme<SolTechnologyAuthenticationOptions, SolTechnologyAuthentication>(
                    SolTechnologyAuthenticationOptions.AuthenticationScheme, c => c.AuthenticationKey = options.Key);

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            var filter = new AuthorizeFilter(policy);

            return filter;
        }
    }
}
