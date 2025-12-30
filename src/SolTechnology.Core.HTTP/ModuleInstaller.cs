using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.HTTP
{
    public static class ModuleInstaller
    {
        public static IServiceCollection AddHTTPClient<TIClient, TClient>(
            this IServiceCollection services,
            string httpClientName,
            HTTPClientConfiguration? httpClientConfiguration = null,
            HttpPolicyConfiguration? httpPolicyConfiguration = null)
            where TIClient : class where TClient : class, TIClient
        {
            AddHTTPClientInternal<TIClient, TClient>(services, httpClientName, httpClientConfiguration, httpPolicyConfiguration);
            return services;
        }

        public static IServiceCollection AddHTTPClient<TIClient, TClient, TOptions>(
            this IServiceCollection services,
            string httpClientName,
            HTTPClientConfiguration? httpClientConfiguration = null,
            HttpPolicyConfiguration? httpPolicyConfiguration = null)
            where TIClient : class where TClient : class, TIClient where TOptions : class
        {
            AddHTTPClientInternal<TIClient, TClient>(services, httpClientName, httpClientConfiguration, httpPolicyConfiguration);
            
            services
                .AddOptions<TOptions>()
                .Configure<IConfiguration>((options, configuration) =>
            {
                configuration = configuration.GetSection($"HTTPClients:{httpClientName}:Options");
                configuration.Bind(options);
            });

            return services;
        }

        private static void AddHTTPClientInternal<TIClient, TClient>(
            IServiceCollection services,
            string httpClientName,
            HTTPClientConfiguration? httpClientConfiguration,
            HttpPolicyConfiguration? httpPolicyConfiguration)
            where TIClient : class where TClient : class, TIClient
        {
            IConfigurationSection? configurationSection = null;

            services
                .AddOptions<HttpPolicyConfiguration>()
                .Configure<IConfiguration>((config, configuration) =>
                {
                    configurationSection = configuration.GetSection($"HTTPClients:{httpClientName}");
                    if (httpClientConfiguration == null)
                    {
                        httpClientConfiguration = configurationSection.Get<HTTPClientConfiguration>();
                    }

                    if (httpClientConfiguration == null)
                    {
                        throw new ArgumentException(
                            $"The [{nameof(HTTPClientConfiguration)}] for client: [{httpClientName}] is missing. Provide it by parameter or configuration section");
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
                        httpClient.BaseAddress = new Uri(httpClientConfiguration!.BaseAddress);
                        if (httpClientConfiguration.TimeoutSeconds.HasValue)
                        {
                            httpClient.Timeout = TimeSpan.FromSeconds(httpClientConfiguration.TimeoutSeconds.Value);
                        }

                        foreach (var header in httpClientConfiguration.Headers)
                        {
                            httpClient.DefaultRequestHeaders.Add(header.Name, header.Value);
                        }
                    })
                .AddPolicyHandler(policyFactory.Create(httpPolicyConfiguration));
        }
    }
}
