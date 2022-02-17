using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.ApiClient
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddApiClient<TIClient, TClient>(this IServiceCollection services, string httpClientName, ApiClientConfiguration apiClientConfigurations = null) where TIClient : class where TClient : class, TIClient
        {
            //it is run only, if the options are not build (once per multiple registrations)
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

            var options = services.BuildServiceProvider().GetRequiredService<IOptions<ApiClientConfiguration>>().Value;

            var httpClientConfiguration = options.HttpClients.FirstOrDefault(h => h.Name == httpClientName);
            if (httpClientConfiguration == null)
            {
                throw new ArgumentException($"The Http Client configuration for client: [{httpClientName}] is missing. Provide it by parameter or configuration section");
            }

            services.AddHttpClient<TIClient, TClient>(httpClientName,
                httpClient =>
                {
                    httpClient.BaseAddress = new Uri(httpClientConfiguration.BaseAddress);
                    if (httpClientConfiguration.TimeoutSeconds.HasValue)
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(httpClientConfiguration.TimeoutSeconds.Value);
                    }

                    foreach (var header in httpClientConfiguration.Headers)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Name, header.Value);
                    }
                });


            return services;
        }
    }
}
