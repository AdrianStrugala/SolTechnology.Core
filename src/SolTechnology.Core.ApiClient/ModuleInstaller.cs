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
            ApiClientConfiguration? apiClientConfiguration = null,
            HttpPolicyConfiguration? httpPolicyConfiguration = null)
            where TIClient : class where TClient : class, TIClient
        {
            AddApiClientInternal<TIClient, TClient>(services, httpClientName, apiClientConfiguration, httpPolicyConfiguration);
            return services;
        }

        public static IServiceCollection AddApiClient<TIClient, TClient, TOptions>(
            this IServiceCollection services,
            string httpClientName,
            ApiClientConfiguration? apiClientConfiguration = null,
            HttpPolicyConfiguration? httpPolicyConfiguration = null)
            where TIClient : class where TClient : class, TIClient where TOptions : class
        {
            AddApiClientInternal<TIClient, TClient>(services, httpClientName, apiClientConfiguration, httpPolicyConfiguration);
            
            services
                .AddOptions<TOptions>()
                .Configure<IConfiguration>((options, configuration) =>
            {
                configuration = configuration.GetSection($"ApiClients:{httpClientName}:Options");
                configuration.Bind(options);
            });

            return services;
        }

        private static IConfigurationSection AddApiClientInternal<TIClient, TClient>(
            IServiceCollection services,
            string httpClientName,
            ApiClientConfiguration? apiClientConfiguration,
            HttpPolicyConfiguration? httpPolicyConfiguration) 
            where TIClient : class where TClient : class, TIClient
        {
            IConfigurationSection? configurationSection = null;

            services
                .AddOptions<HttpPolicyConfiguration>()
                .Configure<IConfiguration>((config, configuration) =>
                {
                    configurationSection = configuration.GetSection($"ApiClients:{httpClientName}");
                    if (apiClientConfiguration == null)
                    {
                        apiClientConfiguration = configurationSection.Get<ApiClientConfiguration>();
                    }

                    if (apiClientConfiguration == null)
                    {
                        throw new ArgumentException(
                            $"The [{nameof(ApiClientConfiguration)}] for client: [{httpClientName}] is missing. Provide it by parameter or configuration section");
                    }

                    if (httpPolicyConfiguration == null)
                    {
                        httpPolicyConfiguration =
                            configuration.GetSection("HttpPolicy").Get<HttpPolicyConfiguration>();
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
            httpPolicyConfiguration = serviceProvider.GetRequiredService<IOptions<HttpPolicyConfiguration>>().Value;

            services.AddHttpClient<TIClient, TClient>(httpClientName,
                    httpClient =>
                    {
                        httpClient.BaseAddress = new Uri(apiClientConfiguration?.BaseAddress);
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


            return configurationSection;
        }
    }
}
