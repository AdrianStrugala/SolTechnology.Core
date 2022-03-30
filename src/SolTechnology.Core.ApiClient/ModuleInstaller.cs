using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.ApiClient
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddApiClient<TIClient, TClient>(
            this IServiceCollection services,
            string httpClientName,
            ApiClientConfiguration apiClientConfiguration = null) 
            where TIClient : class where TClient : class, TIClient
        {
            services
            .AddOptions<List<ApiClientConfiguration>>()
            .Configure<IConfiguration>((config, configuration) =>
            {
                if (apiClientConfiguration == null)
                {
                    apiClientConfiguration = configuration.GetSection("Configuration:ApiClients").Get<List<ApiClientConfiguration>>().FirstOrDefault(a => a.Name.Equals(httpClientName, StringComparison.InvariantCultureIgnoreCase));
                }

                if (apiClientConfiguration == null)
                {
                    throw new ArgumentException($"The [{nameof(ApiClientConfiguration)}] is missing. Provide it by parameter or configuration section");
                }

                config.Add(apiClientConfiguration);
            });

            var options = services.BuildServiceProvider().GetRequiredService<IOptions<List<ApiClientConfiguration>>>().Value;

            var httpClientConfiguration = options.FirstOrDefault(h => h.Name.Equals(httpClientName, StringComparison.InvariantCultureIgnoreCase));
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
