using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.ApiClient.Connection;

namespace SolTechnology.Core.ApiClient
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddApiClients(this IServiceCollection services, ApiClientConfiguration? apiClientConfigurations = null)
        {

            services
                .AddOptions<ApiClientConfiguration>()
                .Configure<IConfiguration>((config, configuration) =>
           {

               if (apiClientConfigurations == null)
               {
                   apiClientConfigurations = configuration.GetSection("Configuration:ApiClients").Get<ApiClientConfiguration>();
               }

               if (apiClientConfigurations == null)
               {
                   throw new ArgumentException($"The [{nameof(ApiClientConfiguration)}] is missing. Provide it by parameter or configuration section");
               }

               config.HttpClients = apiClientConfigurations.HttpClients;
           });


            services.AddSingleton<IApiClientFactory, ApiClientFactory>();

            return services;
        }
    }
}
