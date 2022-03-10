using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Authentication
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddAuthentication(
            this IServiceCollection services,
            AuthenticationConfiguration authenticationConfiguration = null)
        {


            //it is run only, if the options are not build (once per multiple registrations)
            services
            .AddOptions<AuthenticationConfiguration>()
            .Configure<IConfiguration>((config, configuration) =>
            {
                if (authenticationConfiguration == null)
                {
                    authenticationConfiguration = configuration.GetSection("Configuration:Authentication").Get<AuthenticationConfiguration>();
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

            return services;
        }
    }
}
