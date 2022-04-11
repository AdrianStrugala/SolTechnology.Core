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
                    throw new ArgumentException($"The [{nameof(ApiClientConfiguration)}] for client: [{httpClientName}] is missing. Provide it by parameter or configuration section");
                }

                config.Add(apiClientConfiguration);
            });

            var options = services.BuildServiceProvider().GetRequiredService<IOptions<List<ApiClientConfiguration>>>().Value;

            apiClientConfiguration = options.First(h => h.Name.Equals(httpClientName, StringComparison.InvariantCultureIgnoreCase));

            services.AddHttpClient<TIClient, TClient>(httpClientName,
                httpClient =>
                {
                    httpClient.BaseAddress = new Uri(apiClientConfiguration.BaseAddress);
                    if (apiClientConfiguration.TimeoutSeconds.HasValue)
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(apiClientConfiguration.TimeoutSeconds.Value);
                    }

                    foreach (var header in apiClientConfiguration.Headers)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Name, header.Value);
                    }
                });


            return services;
        }
    }
}
