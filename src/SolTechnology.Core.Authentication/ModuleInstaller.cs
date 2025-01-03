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
            AuthenticationConfiguration? authenticationConfiguration = null)
        {
            services
            .AddOptions<AuthenticationConfiguration>()
            .Configure<IConfiguration>((config, configuration) =>
            {
                if (string.IsNullOrEmpty(authenticationConfiguration?.ApiKey))
                {
                    authenticationConfiguration = configuration.GetSection("Authentication").Get<AuthenticationConfiguration>();
                }

                if (string.IsNullOrEmpty(authenticationConfiguration?.ApiKey))
                {
                    throw new ArgumentException($"The [{nameof(AuthenticationConfiguration)}{nameof(authenticationConfiguration.ApiKey)}] is missing. Provide it by parameter or configuration section");
                }

                config.ApiKey = authenticationConfiguration.ApiKey;
            });

            var options = services.BuildServiceProvider().GetRequiredService<IOptions<AuthenticationConfiguration>>().Value;

            services.AddAuthentication(ApiKeyAuthenticationSchemeOptions.AuthenticationScheme)
                .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyAuthenticationSchemeOptions.AuthenticationScheme, c => c.ApiKey = options.ApiKey);

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            var filter = new AuthorizeFilter(policy);

            return filter;
        }
    }
}
