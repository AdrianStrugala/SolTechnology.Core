using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.ApiClient
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddApiClient<TIClient, TClient>(
            this IServiceCollection services,
            string httpClientName,
            ApiClientConfiguration apiClientConfiguration = null,
            HttpPolicyConfiguration httpPolicyConfiguration = null)
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

            services
                .AddOptions<HttpPolicyConfiguration>()
                .Configure<IConfiguration>((config, configuration) =>
                {
                    if (httpPolicyConfiguration == null)
                    {
                        httpPolicyConfiguration = configuration.GetSection("Configuration:HttpPolicy").Get<HttpPolicyConfiguration>();
                    }

                    if (httpPolicyConfiguration == null)
                    {
                        httpPolicyConfiguration = new HttpPolicyConfiguration();
                    }

                    config.CircuitBreakerDelayDuration = httpPolicyConfiguration.CircuitBreakerDelayDuration;
                    config.CircuitBreakerFailureThreshold = httpPolicyConfiguration.CircuitBreakerFailureThreshold;
                    config.CircuitBreakerMinimumThroughput = httpPolicyConfiguration.CircuitBreakerMinimumThroughput;
                    config.CircuitBreakerSamplingDuration = httpPolicyConfiguration.CircuitBreakerSamplingDuration;
                    config.MaxRequestRetries = httpPolicyConfiguration.MaxRequestRetries;
                    config.RequestTimeout = httpPolicyConfiguration.RequestTimeout;
                    config.RetryInitialDelay = httpPolicyConfiguration.RetryInitialDelay;
                    config.RetryTimeout = httpPolicyConfiguration.RetryTimeout;
                    config.UsePolly = httpPolicyConfiguration.UsePolly;
                });

            var serviceProvider = services.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<HttpPolicyFactory>>();
            var policyFactory = new HttpPolicyFactory(logger);

            var apiClientConfigurations = serviceProvider.GetRequiredService<IOptions<List<ApiClientConfiguration>>>().Value;
            apiClientConfiguration = apiClientConfigurations.First(h => h.Name.Equals(httpClientName, StringComparison.InvariantCultureIgnoreCase));

            httpPolicyConfiguration = serviceProvider.GetRequiredService<IOptions<HttpPolicyConfiguration>>().Value;

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
                })
                .AddPolicyHandler(policyFactory.Create(httpPolicyConfiguration));


            return services;
        }
    }
}
